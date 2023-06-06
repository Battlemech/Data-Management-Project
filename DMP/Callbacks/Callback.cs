using System;
using DMP.Utility;

namespace DMP.Callbacks
{
    public abstract class Callback
    {
        public readonly string Name;
        public readonly bool RemoveOnError;

        protected Callback(string name, bool removeOnError)
        {
            Name = name;
            RemoveOnError = removeOnError;
        }

        public abstract bool Invoke(object o);

        public abstract bool Invoke(object one, object other);
    }

    public class Callback<T> : Callback
    {
        private readonly Action<T> _callback;

        public Callback(string name, Action<T> callback, bool removeOnError) : base(name, removeOnError)
        {
            _callback = callback;
        }

        public bool Invoke(T value)
        {
            try
            {
                _callback.Invoke(value);
            }
            catch (Exception e)
            {
                //varying exception format depending on RemoveOnError flag
                if (RemoveOnError)
                {
                    LogWriter.Log($"Removing callback {Name} because it caused an exception.\nException: " + e);
                    return false;
                }
                
                LogWriter.LogException(e);
                return true;
            }

            return true;
        }

        public override bool Invoke(object o)
        {
            if (o is T data) return Invoke(data);
            return true;
        }

        public override bool Invoke(object one, object other)
        {
            //single callback can't be invoked with two parameters
            return true;
        }
    }

    public class Callback<T1, T2> : Callback
    {
        private readonly Action<T1, T2> _callback;

        public Callback(string name, Action<T1, T2> callback, bool removeOnError) : base(name, removeOnError)
        {
            _callback = callback;
        }

        public bool Invoke(T1 one, T2 two)
        {
            try
            {
                _callback.Invoke(one, two);
            }
            catch (Exception e)
            {
                //varying exception format depending on RemoveOnError flag
                if (RemoveOnError)
                {
                    LogWriter.Log($"Removing callback {Name} because it caused an exception.\nException: " + e);
                    return false;
                }
                
                LogWriter.LogException(e);
                return true;
            }

            return true;
        }

        public override bool Invoke(object one, object two)
        {
            if (one is T1 t1 && two is T2 t2) return Invoke(t1, t2);
            return true;
        }
        
        public override bool Invoke(object o)
        {
            if (o is Tuple<T1, T2> tuple) return Invoke(tuple.Item1, tuple.Item2);
            return true;
        }
    }
}