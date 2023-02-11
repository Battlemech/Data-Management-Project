using System;

namespace DMP.Utility
{
    /// <summary>
    /// Prevents serialization and synchronisation of the value
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PreventSerialization : Attribute
    {
        
    }
}