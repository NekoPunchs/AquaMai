// ***********************************************************************
// 文件名：         ConvertUtil.Cast.cs
// 创建日期：       2025/05/23
// 功能说明：        指针级直接转换
// 作者：           NekoPunch!
// ***********************************************************************

#nullable enable

namespace WaveKits
{
    public static unsafe class CastUtil
    {
        public static int ToInt32(uint value)
        {
            uint* ptr = &value;
            return *(int*)ptr;
        }

        public static uint ToUInt32(int value)
        {
            int* ptr = &value;
            return *(uint*)ptr;
        }
        
        public static long ToInt64(ulong value)
        {
            ulong* ptr = &value;
            return *(long*)ptr;
        }

        public static ulong ToUInt64(long value)
        {
            long* ptr = &value;
            return *(ulong*)ptr;
        }
    }
}