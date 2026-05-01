using System;

namespace Ayla
{
    public readonly struct YieldAction
    {
        public readonly double? TimeSlicing;
        public readonly Action Work;

        public YieldAction(Action work, double? timeSlicing)
        {
            Work = work;
            TimeSlicing = timeSlicing;
        }
    }
}
