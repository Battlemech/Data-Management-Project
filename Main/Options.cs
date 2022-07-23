using System;

namespace Main
{
    public static class Options
    {
        public static readonly int MaxMessageSize = int.MaxValue;
        public static readonly int DefaultPort = 11000;
        public static readonly TimeSpan DefaultTimeout = new TimeSpan(0, 0, 3);
    }
}