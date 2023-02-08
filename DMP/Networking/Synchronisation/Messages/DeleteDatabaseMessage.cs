using System.Collections.Generic;
using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class DeleteDatabaseMessage : Message
    {
        public string DatabaseId { get; set; }
    }
}