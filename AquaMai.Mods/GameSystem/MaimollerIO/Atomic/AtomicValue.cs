// ***********************************************************************
// 文件名：         AtomicValue.cs
// 创建日期：       2026/04/05
// 作者：           nekopunch
// ***********************************************************************

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WaveKits
{
    /// <summary>
    /// 线程安全的 struct 泛型原子容器。
    /// <para>
    /// 适用于任意值类型（struct），支持原子读（Load）、写（Store）、
    /// 交换（Exchange）以及比较交换（CAS）操作，且所有操作均不产生额外 GC 分配。
    /// </para>
    /// <para>
    /// <b>实现原理</b>：内部使用 <see cref="SpinLock"/>（本身为 struct，存储于字段中无堆分配）
    /// 保护对 <typeparamref name="T"/> 字段的访问。与基于 Box 包装的无锁方案相比，
    /// 每次写入不产生额外堆对象，适合高频率更新的场景。
    /// </para>
    /// <para>
    /// <b>等值比较</b>：CAS 系方法使用 <see cref="EqualityComparer{T}.Default"/> 进行值比较；
    /// 建议 <typeparamref name="T"/> 实现 <see cref="IEquatable{T}"/> 以获得最优比较性能。
    /// </para>
    /// <para>
    /// <b>约束</b>：<typeparamref name="T"/> 必须是值类型（struct）。
    /// </para>
    /// </summary>
    /// <typeparam name="T">任意值类型（struct）。建议同时实现 <see cref="IEquatable{T}"/>。</typeparam>
    public sealed class AtomicValue<T> where T : struct
    {
        // SpinLock 本身是 struct：存在字段中，不产生堆分配。
        // enableThreadOwnerTracking = false：禁用调试追踪，避免额外开销。
        private SpinLock _lock = new(false);
        private T _value;

        // EqualityComparer<T>.Default 是静态缓存单例，取用时不产生 GC。
        private static readonly EqualityComparer<T> s_comparer = EqualityComparer<T>.Default;

        /// <summary>初始化原子容器，初始值为 default(<typeparamref name="T"/>)。</summary>
        public AtomicValue()
        {
        }

        /// <summary>初始化原子容器，并设置初始值。</summary>
        public AtomicValue(T value) => _value = value;

        // ── Load ──────────────────────────────────────────────────────────

        /// <summary>
        /// 原子读取当前值（struct 拷贝）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Load()
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                return _value;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
        }

        // ── Store ─────────────────────────────────────────────────────────

        /// <summary>
        /// 原子写入新值。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(T newValue)
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                _value = newValue;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
        }

        public T Value
        {
            get => Load();
            set => Store(value);
        }

        // ── Exchange ──────────────────────────────────────────────────────

        /// <summary>
        /// 原子地将当前值替换为 <paramref name="newValue"/>，并返回替换前的旧值。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Exchange(T newValue)
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                var old = _value;
                _value = newValue;
                return old;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
        }

        // ── CompareAndSet / CompareExchange ───────────────────────────────

        /// <summary>
        /// 原子 CAS：若当前值与 <paramref name="expected"/> 值相等，
        /// 则将其替换为 <paramref name="newValue"/> 并返回 <see langword="true"/>；
        /// 否则返回 <see langword="false"/>。
        /// <para>相等性通过 <see cref="EqualityComparer{T}.Default"/> 判断。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CompareAndSet(in T expected, T newValue)
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                if (!s_comparer.Equals(_value, expected))
                {
                    return false;
                }

                _value = newValue;
                return true;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
        }

        /// <summary>
        /// 原子 CAS：返回替换前的旧值（无论是否成功替换）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CompareExchange(in T expected, T newValue)
        {
            var lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                var old = _value;
                if (s_comparer.Equals(_value, expected))
                {
                    _value = newValue;
                }

                return old;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
        }

        // ── 隐式转换 ──────────────────────────────────────────────────────

        /// <summary>隐式转换：直接从 <see cref="AtomicValue{T}"/> 读取当前值。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(AtomicValue<T> atomic) => atomic.Load();

        public override string ToString() => Load().ToString();
    }
}