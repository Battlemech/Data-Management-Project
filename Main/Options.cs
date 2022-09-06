using System;
using System.Collections.Generic;
using Main.Databases;

namespace Main
{
    public static class Options
    {
        public const int MaxMessageSize = int.MaxValue;
        public const int DefaultPort = 11000;
        
        /// <summary>
        /// True if clients shall save data persistently if host of data is persistent
        /// </summary>
        public const bool DefaultClientPersistence = true;
        
        //default timeout in ms
        public const int DefaultTimeout = 3000;

        /// <summary>
        /// Array of types which will be ignored during serialization of objects
        /// </summary>
        public static readonly Type[] IgnoredTypes = new[] { typeof(Database) };
    }
}