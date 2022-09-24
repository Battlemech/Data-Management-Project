using System;
using System.Collections;

namespace DMP.Utility
{
    public static class LogWriter
    {
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }
        
        public static void LogWarning(string message)
        {
            Console.WriteLine("Warning: "+message);
        }
        
        public static void LogError(string message)
        {
            Console.WriteLine("Error: "+message);
        }
        
        public static void LogException(Exception exception)
        {
            Console.WriteLine("Exception: "+exception);
        }

        public static string PrintCollection(object o)
        {
            if (o is ICollection collection) return StringifyCollection(collection);
            return o.ToString();
        }
        
        public static string StringifyCollection(ICollection collection)
        {
            if (collection == null) return "null";
            if (collection.Count == 0) return "0()";
                
            string s = $"{collection.Count}(";
            foreach (var o in collection)
            {
                s += o + ",";
            }
            return s.Substring(0, s.Length - 1) + ")";
        }
    }
}