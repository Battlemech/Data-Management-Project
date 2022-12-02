using System;
using System.Collections.Generic;
using DMP.Databases;

namespace DMP
{
    public static class Options
    {
        public const int MaxMessageSize = int.MaxValue;
        public const int DefaultPort = 11000;

        //default timeout in ms
        public const int DefaultTimeout = 3000;
    }
}