namespace LogTest
{
    using System;

    public interface IClock
    {
        DateTime Now { get; }
    }
}