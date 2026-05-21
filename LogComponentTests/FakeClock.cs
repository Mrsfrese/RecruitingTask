using System;
using LogTest;

namespace LogComponentTests
{
    public class FakeClock : IClock
    {
        public DateTime Now { get; set; }
    }
}