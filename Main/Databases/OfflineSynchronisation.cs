using System;
using Main.Utility;

namespace Main.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Invoked when a value was modified while no connection was established.
        /// </summary>
        private void OnOfflineModification(string id, byte[] value)
        {
            Console.WriteLine($"{this}: id={id}. Waiting for HostId to be initialized!");
            
            OnInitialized<Guid>(nameof(HostId), (guid =>
            {
                bool isHost = guid == Client.Id;
                
                Console.WriteLine($"{this}: id={id}. IsHost: {isHost}");
                
                //if host modified data without connection: Synchronise it
                if (isHost)
                {
                    OnSetSynchronised(id, value);
                    return;
                }
                
                //if client modified data and it was already changed: return
                if(GetModCount(id) > 0) return;
                
                SafeModify<object>(id, (o) =>
                {
                    if (GetModCount(id) > 0) return o;

                    //try extracting type of object
                    Type type = null;
                    if (o != null) type = o.GetType();
                    if (type == null) TryGetType(id, out type);

                    //type could not be deserialized: Saved value must be null or it must have been overwritten with null while waiting for SafeModify()
                    //-> overwritten: not a problem because that null value will be synchronised with the next operation -> No data was lost
                    return type != null ? Serialization.Deserialize(value, type) : default;
                });
            }));
        }
    }
}