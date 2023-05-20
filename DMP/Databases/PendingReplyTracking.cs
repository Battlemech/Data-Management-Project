using System;
using System.Collections.Generic;

namespace DMP.Databases
{
    public partial class Database
    {

        /// <summary>
        /// Serialized confirmed bytes by remote.
        /// Only saved while replies are pending
        /// </summary>
        private readonly Dictionary<string, byte[]> _confirmedValues = new Dictionary<string, byte[]>();
    }
}