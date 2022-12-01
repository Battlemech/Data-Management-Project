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

        /// <summary>
        /// Array of types which will be ignored during serialization of objects.
        /// Configure this before you access Utility/Serialization.cs for the first time.
        /// Changes to IgnoredTypes after the initialization will not have any effect
        /// </summary>
        public static readonly List<Type> IgnoredTypes = new List<Type>() { typeof(Database) };
    }
}