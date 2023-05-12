using System;
using System.Threading;
using System.Threading.Tasks;
using DMP.Threading;
using DMP.Utility;

namespace DMP.Networking.Messaging.Client
{
    public partial class MessageClient
    {
        public delegate void OnReply<T>(T reply) where T : ReplyMessage;

        public bool SendRequest<TRequest, TReply>(TRequest requestMessage, OnReply<TReply> onReply, int timeout = Options.DefaultTimeout)
            where TReply : ReplyMessage
            where TRequest : RequestMessage<TReply>
        {
            string callbackId = $"NETWORKING-{requestMessage.Id}";
            ManualResetEvent receivedMessage = new ManualResetEvent(false);
            
            AddCallback<TReply>((replyMessage =>
            {
                //received message with another id
                if(replyMessage.Id != requestMessage.Id) return;

                //stop waiting task from invoking the callback
                receivedMessage.Set();
                
                //remove callback
                RemoveCallbacks<TReply>(callbackId);
                
                //invoke delegate: request has arrived
                onReply.Invoke(replyMessage);
            }), callbackId);

            //start background thread instead of task: Starting too many waiting task in TaskPool will cause task
            //later tasks to not start because Task Pool is still waiting for first tasks to complete (which are waiting)
            Delegation.EnqueueAction((() =>
            {
                if (receivedMessage.WaitOne(timeout)) return;

                //remove the callback
                RemoveCallbacks<TReply>(callbackId);

                //invoke it with null as value
                onReply.Invoke(null);
            }));

            //send the request
            return SendMessage(requestMessage);
        }
        
        public bool SendRequest<TRequest, TReply>(TRequest requestMessage, out TReply replyMessage,
            int timeout = Options.DefaultTimeout)
            where TReply : ReplyMessage
            where TRequest : RequestMessage<TReply>
        {
            //init out parameter
            replyMessage = null;
            
            //init var to save message. Cant use out parameter in lambda expression :/
            TReply message = null;
            
            //reset event to wait for reply
            ManualResetEvent receivedReply = new ManualResetEvent(false);
            
            bool success = SendRequest<TRequest, TReply>(requestMessage, (reply) =>
            {
                //save received message
                message = reply;
                
                //signal waiting thread that message was received
                receivedReply.Set();

            }, timeout);

            //failed to send request
            if (!success)
            {
                return false;
            }
            
            //try waiting for a reply
            success = receivedReply.WaitOne(timeout);

            //save reply in out parameter if request was received
            if (success) replyMessage = message;
            
            return success;
        }

        public async Task<TReply> SendRequest<TRequest, TReply>(TRequest requestMessage, int timeout = Options.DefaultTimeout)
            where TReply : ReplyMessage
            where TRequest : RequestMessage<TReply>
        {
            TReply reply = default;

            //wait for reply
            await Delegation.DelegateAction((() =>
            {
                //allow new task to wait for reply
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                
                bool success = SendRequest<TRequest, TReply>(requestMessage, (r =>
                {
                    //update reply
                    reply = r;
                
                    //signal waiting task that reply was received
                    resetEvent.Set();
                }), timeout);

                if (!success)
                {
                    LogWriter.LogError($"Failed to send {requestMessage}");
                    return;
                }
                
                if(!resetEvent.WaitOne(timeout))
                    LogWriter.LogError($"Received no reply for {requestMessage} within {timeout} ms!");
            }));

            return reply;
        }
    }
}