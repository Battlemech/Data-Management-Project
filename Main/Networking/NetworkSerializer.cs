using System;
using System.Collections.Generic;
using Main.Utility;
using Buffer = Main.Submodules.NetCoreServer.Buffer;

namespace Main.Networking
{
    public class NetworkSerializer
    {
        private static readonly byte[] MessageStart = BitConverter.GetBytes('['); //2 bytes
        private static readonly byte[] MessageSeparator = BitConverter.GetBytes('|'); //2 bytes
        private static readonly byte[] MessageEnd = BitConverter.GetBytes(']'); //2 bytes
        
        private readonly Buffer _receiveBuffer = new Buffer(256);
        private bool _invalidDataInBuffer = false;

        public List<byte[]> Deserialize(byte[] buffer)
        {
            return Deserialize(buffer, 0, buffer.Length);
        }
        
        public List<byte[]> Deserialize(byte[] buffer, long offset, long size)
        {
            //append received data
            _receiveBuffer.Append(buffer, offset, size);

            List<byte[]> messages = new List<byte[]>(1);
            
            //while there is a header to process
            while (_receiveBuffer.Size >= 8)
            {
                //check message start
                byte[] received = _receiveBuffer.Data;
                if (!StartsWith(received, MessageStart))
                {
                    Console.WriteLine(received[0]+received[1]+ " is not equal to "+MessageStart[0]+MessageStart[1]);
                    if(!_invalidDataInBuffer) LogWriter.LogWarning("Received invalid message start");
                    FindNextMessageStart();
                    continue;
                }

                //check message length
                int messageLength = BitConverter.ToInt32(PartialCopy(received, 2, 4), 0);
                
                if (messageLength > Options.MaxMessageSize)
                {
                    LogWriter.LogWarning($"Received message with length of {messageLength}, which is bigger than allowed max: {Options.MaxMessageSize}");
                    FindNextMessageStart();
                    continue;
                }
                
                //check message separator
                if (!StartsWith(received, MessageSeparator, 6))
                {
                    LogWriter.LogWarning("Received invalid message separator");
                    FindNextMessageStart();
                    continue;
                }
                
                //check if data has been received
                //MessageStart + MessageLength + MessageSeparator + <Message> + MessageEnd
                if (2 + 4 + 2 + messageLength + 2 > _receiveBuffer.Size)
                {
                    //if data wasn't received, return later
                    break;
                }

                //check if message end was received
                if (!StartsWith(received, MessageEnd, 8 + messageLength))
                {
                    LogWriter.Log("Received invalid message end");
                    FindNextMessageStart();
                    continue;
                }
                
                //add received message by copying array
                byte[] message = new byte[messageLength];
                Array.Copy(received, 8, message, 0, messageLength);
                messages.Add(message);

                //remove bytes from buffer
                //MessageStart + MessageLength + MessageSeparator + <Message> + MessageEnd
                _receiveBuffer.Remove(0, 2 + 4 + 2 + messageLength + 2);
                
                //a message has been successfully deserialized.
                _invalidDataInBuffer = false;
            }
            
            return messages;
        }

        /// <summary>
        /// Remove the first byte of the receiveBuffer while it doesn't start with message start
        /// </summary>
        private void FindNextMessageStart()
        {
            _invalidDataInBuffer = true;
            while (_receiveBuffer.Size > 1)
            {
                if (StartsWith(_receiveBuffer.Data, MessageStart))
                {
                    LogWriter.Log("Removed invalid data from buffer");
                    _invalidDataInBuffer = false;
                    return;
                }
                
                //remove first bit
                _receiveBuffer.Remove(0, 1);
            }
            
            LogWriter.LogWarning("Failed to find next valid message start");
        }
        
        public static byte[] Serialize(byte[] message)
        {
            if (message.Length > Options.MaxMessageSize) throw new Exception($"Max message size was exceeded! ({message.Length}/{Options.MaxMessageSize})");
            
            byte[] buffer = new byte[2 + 4 + 2 + message.Length + 2];
            Array.Copy(MessageStart, 0, buffer, 0, 2);
            Array.Copy(BitConverter.GetBytes(message.Length), 0, buffer, 2, 4);
            Array.Copy(MessageSeparator, 0, buffer, 6, 2);
            Array.Copy(message, 0, buffer, 8, message.Length);
            Array.Copy(MessageEnd, 0, buffer, 8 + message.Length, 2);
            return buffer;
        }

        /// <summary>
        /// Returns true if buffer1 starts with buffer2
        /// </summary>
        public static bool StartsWith(byte[] buffer1, byte[] buffer2, int offset = 0)
        {
            if (buffer1.Length < buffer2.Length) {return false;}
            
            for (int i = 0; i < buffer2.Length; i++)
            {
                if (buffer1[i+offset] != buffer2[i]) return false;
            }
            return true;
        }

        public static byte[] PartialCopy(byte[] buffer, int offset, int length)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = buffer[i + offset];
            }

            return bytes;
        }
    }
}