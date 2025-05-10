using System;

namespace Ayla.Core
{
    public static class MathEx
    {
        public static double InvSqrt(double value)
        {
            unsafe
            {
                const ulong MNumber = 0x5FE6EB50C7B537A9UL;

                double y = value;
                double x2 = y * 0.5;
                ulong i = (ulong)BitConverter.DoubleToInt64Bits(y);
                i = MNumber - (i >> 1);
                y = BitConverter.DoubleToInt64Bits(i);
                y *= (1.5 - (x2 * y * y));
#if INVSQRT_TWO_ITERATIONS
                y *= (1.5 - (x2 * y * y));
#endif
#if INVSQRT_THREE_ITERATIONS
                y *= (1.5 - (x2 * y * y));
#endif
                return y;
            }
        }
    }
}