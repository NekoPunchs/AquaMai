// ***********************************************************************
// 文件名：         Atomic.cs
// 创建日期：       2026/04/05
// 作者：           nekopunch
// ***********************************************************************

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WaveKits
{
    /// <summary>
    /// 线程安全的泛型原子容器。
    /// <para>
    /// 所有读写操作均通过 <see cref="Volatile"/> 保证线程间可见性（happens-before）；
    /// CAS（Compare-And-Swap）系操作通过 <see cref="Interlocked"/> 实现无锁原子语义。
    /// </para>
    /// <para>
    /// 约束说明：<typeparamref name="T"/> 必须是引用类型（class）。
    /// </para>
    /// </summary>
    public class Atomic<T> where T : class?
    {
        private T? _value;

        /// <summary>初始化原子容器，初始值为 <see langword="null"/>。</summary>
        public Atomic()
        {
        }

        /// <summary>初始化原子容器，并设置初始值。</summary>
        public Atomic(T? value) => _value = value;

        // ── Read ──────────────────────────────────────────────────────────

        /// <summary>
        /// 原子读取当前值。
        /// <para>等价于带 <see cref="Volatile.Read{T}"/> 语义的读取，保证不被编译器/CPU 重排。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Load() => Volatile.Read(ref _value);

        // ── Write ─────────────────────────────────────────────────────────

        /// <summary>
        /// 原子写入新值。
        /// <para>等价于带 <see cref="Volatile.Write{T}"/> 语义的写入，所有之前的操作对其他线程立即可见。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(T? newValue) => Volatile.Write(ref _value, newValue);

        public T? Value
        {
            get => Load();
            set => Store(value);
        }

        // ── Exchange ──────────────────────────────────────────────────────

        /// <summary>
        /// 原子地将当前值替换为 <paramref name="newValue"/>，并返回替换前的旧值。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Exchange(T? newValue) => Interlocked.Exchange(ref _value, newValue);

        // ── Compare-And-Swap ──────────────────────────────────────────────

        /// <summary>
        /// 原子 CAS：若当前值 == <paramref name="expected"/>（引用相等），
        /// 则将其替换为 <paramref name="newValue"/> 并返回 <see langword="true"/>；否则返回 <see langword="false"/>。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CompareAndSet(T? expected, T? newValue) => ReferenceEquals(Interlocked.CompareExchange(ref _value, newValue, expected), expected);

        /// <summary>
        /// 原子 CAS：返回替换前的旧值（无论是否成功替换）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? CompareExchange(T? expected, T? newValue) => Interlocked.CompareExchange(ref _value, newValue, expected);

        // ── Update（自旋重试）────────────────────────────────────────────

        /// <summary>
        /// 基于乐观 CAS 自旋，对当前值执行 <paramref name="updateFunc"/> 并原子地写入结果。
        /// <para>适合用于无副作用的纯函数更新（例如：累加、位运算）。</para>
        /// </summary>
        /// <param name="updateFunc">接收当前值，返回期望写入的新值。</param>
        /// <returns>本次成功写入的新值。</returns>
        public T? Update(Func<T?, T?> updateFunc)
        {
            T? current;
            T? next;
            do
            {
                current = Load();
                next = updateFunc(current);
            } while (!CompareAndSet(current, next));

            return next;
        }

        /// <summary>
        /// 基于乐观 CAS 自旋，对当前值执行 <paramref name="updateFunc"/> 并原子地写入结果。
        /// </summary>
        /// <param name="updateFunc">接收当前值，返回期望写入的新值。</param>
        /// <returns>更新前的旧值。</returns>
        public T? GetAndUpdate(Func<T?, T?> updateFunc)
        {
            T? current;
            T? next;
            do
            {
                current = Load();
                next = updateFunc(current);
            } while (!CompareAndSet(current, next));

            return current;
        }

        // ── 隐式转换 ──────────────────────────────────────────────────────

        /// <summary>隐式转换：直接从 <see cref="Atomic{T}"/> 读取当前值。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T?(Atomic<T> atomic) => atomic.Load();

        public override string? ToString() => Load()?.ToString();
    }
}