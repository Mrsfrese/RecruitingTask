namespace LogTest
{
    using System;

    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}