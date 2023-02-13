using System;

namespace DMP.Utility
{
    /// <summary>
    /// Prevents serialization and synchronisation of the value
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PreventSerialization : Attribute
    {
        
    }
}