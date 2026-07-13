using WaveKits;

namespace AquaMai.Mods.GameSystem
{
    public class InputLatch
    {
        private readonly AtomicLong _accumulated = new();

        public void Update(ulong state)
        {
            var value = CastUtil.ToInt64(state);
            _accumulated.GetAndUpdate(x => x | value);
        }

        public ulong Read()
        {
            var result = _accumulated.GetAndUpdate(x => 0);
            return CastUtil.ToUInt64(result);
        }

        public bool ReadBit(int index)
        {
            var mask = 1L << index;
            return (_accumulated.GetAndUpdate(x => x & ~mask) & mask) != 0;
        }

        public ulong ReadBits(ulong mask)
        {
            var maskValue = CastUtil.ToInt64(mask);
            var result = _accumulated.GetAndUpdate(x => x & ~maskValue) & maskValue;
            return CastUtil.ToUInt64(result);
        }

        public void Clear()
        {
            _accumulated.Store(0);
        }
    }
}