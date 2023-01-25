using System;
using System.Collections.Generic;
using DMP.Databases.VS;
using DMP.Utility;

namespace DMP.Databases
{
    public partial class Database
    {
        private readonly Dictionary<string, Queue<FailedRequest>> _failedRequests =
            new Dictionary<string, Queue<FailedRequest>>();

        private void TrackFailedRequest<T>(string valueId, byte[] value, uint modCount, Action<T> onConfirm)
        {
            EnqueueFailedRequest(valueId,new FailedRequest<T>(value, modCount, onConfirm));
        }
        
        private void TrackFailedRequest<T>(string valueId, SetValueDelegate<T> modifyValueDelegate, uint modCount, Action<T> onConfirm)
        {
            EnqueueFailedRequest(valueId,new FailedRequest<T>(modifyValueDelegate, modCount, onConfirm));
        }

        private void EnqueueFailedRequest<T>(string valueId, FailedRequest<T> failedRequest)
        {
            Queue<FailedRequest> failedRequests;
            lock (_failedRequests)
            {
                //initialise failedRequests if necessary
                if (!_failedRequests.TryGetValue(valueId, out failedRequests))
                {
                    failedRequests = new Queue<FailedRequest>();
                    _failedRequests.Add(valueId, failedRequests);
                }
            }

            lock (failedRequests)
            {
                failedRequests.Enqueue(failedRequest);
            }
        }

        private bool TryDequeueFailedRequest(string valueId, uint modCount, out FailedRequest failedRequest)
        {
            Queue<FailedRequest> failedRequests;
            lock (_failedRequests)
            {
                //no failed request known for this value
                if (!_failedRequests.TryGetValue(valueId, out failedRequests))
                {
                    failedRequest = null;
                    return false;
                }
            }

            //mod count of queued request is too high
            if (failedRequests.Peek().ModCount > modCount)
            {
                failedRequest = null;
                return false;
            }

            failedRequest = failedRequests.Dequeue();
            return true;
        }
    }

    public abstract class FailedRequest
    {
        public readonly uint ModCount;

        public FailedRequest(uint modCount)
        {
            ModCount = modCount;
        }

        public abstract byte[] RepeatModification(byte[] value);
    }
    
    public class FailedRequest<T> : FailedRequest
    {
        public readonly byte[] Value;
        public readonly SetValueDelegate<T> SetValue;

        private readonly Action<T> _onConfirm;

        public FailedRequest(byte[] value, uint modCount, Action<T> onConfirm) : base(modCount)
        {
            Value = value;
            SetValue = null;
            _onConfirm = onConfirm;
        }
        
        public FailedRequest(SetValueDelegate<T> setValue, uint modCount, Action<T> onConfirm) : base(modCount)
        {
            Value = null;
            SetValue = setValue;
            _onConfirm = onConfirm;
        }

        public override byte[] RepeatModification(byte[] value)
        {
            if (Value != null)
            {
                _onConfirm?.Invoke(Serialization.Deserialize<T>(value));
                return value;
            }
            
            if (SetValue != null)
            {
                T obj = SetValue.Invoke(Serialization.Deserialize<T>(value));
                _onConfirm?.Invoke(obj);
                return Serialization.Serialize(obj);
            }

            throw new ArgumentException("FailedRequest lacked Value and SetValueDelegate!");
        }
    }
    
}