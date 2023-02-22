using System;
using System.Runtime.Serialization;
using DMP.Databases;
using DMP.Databases.ValueStorage;
using DMP.Networking.Synchronisation.Client;
using DMP.Utility;

namespace DMP.Objects
{
    public abstract class SynchronisedObject
    {
        protected readonly string Id;
        
        public bool IsHost => GetDatabase().IsHost;
        public Guid HostId => GetDatabase().HostId;

        public bool IsPersistent
        {
            get => GetDatabase().IsPersistent;
            set => GetDatabase().IsPersistent = value;
        }
        public bool IsSynchronised => GetDatabase().IsSynchronised;

        public ValueStorage<bool> ClientPersistence => GetDatabase().ClientPersistence;
        public ValueStorage<bool> HostPersistence => GetDatabase().HostPersistence;

        [PreventSerialization]
        private Database _database = null;
        
        protected SynchronisedObject(string id, bool isPersistent = false)
        {
            if (id.Contains("/")) 
                throw new ArgumentException("Id may not contain '/', it is used as an internal separator!");
            
            Id = id;
            _database = new Database(id, isPersistent, true);
            
            //if the synchronised object was referenced the first time on this client: Invoke its constructor
            if(_database.TryTrackObject(this)) Constructor();
        }

        /// <summary>
        /// Creates a child object for "synchronisedObject", copying its id as prefix, followed by the new object as suffix.
        /// Uses a "/" as separator.
        /// </summary>
        protected SynchronisedObject(SynchronisedObject synchronisedObject, string id, bool isPersistent = false)
        {
            if (id.Contains("/")) 
                throw new ArgumentException("Id may not contain '/', it is used as an internal separator!");
            
            Id = $"{synchronisedObject.Id}/{id}";
            _database = new Database(Id, isPersistent, true);
            
            //if the synchronised object was referenced the first time on this client: Invoke its constructor
            if(_database.TryTrackObject(this)) Constructor();
        }
        
        public Database GetDatabase()
        {
            //retrieve local version of database if necessary
            if (_database == null)
            {
                if (Id == null)
                {
                    /* 
                     * If users create setter functions directly assigning values to ValueStorages,
                     * set operation will be repeated each time a SynchronisedObject is deserialized.
                     * Since values of super classes are deserialized and set first, the Id will be null,
                     * causing this exception. Thus, this exception prevents additional sets.
                     *
                     */ //todo: create documentation entry for this error
                    throw new InvalidOperationException("No id! Avoid writing setters functions for ValueStorage attributes of synchronised objects!");
                }

                //try retrieving the client
                SynchronisedClient client = SynchronisedClient.Instance;
                if (client == null) 
                    throw new InvalidOperationException("Can't retrieve SynchronisedObject if no local SynchronisedClient exists!");

                //retrieve the database
                _database = client.Get(Id);
                
                //if the synchronised object was referenced the first time on this client: Invoke its constructor
                if(_database.TryTrackObject(this)) Constructor();
            }
            
            return _database;
        }

        /// <summary>
        /// <list type="number">
        ///     <listheader> <description>Constructor() is called in two cases:</description> </listheader>
        ///     <item> <description>By the constructor when the SynchronisedObject is created</description> </item>
        ///     <item> <description>The first time any of its values is accessed after it was created on a remote client or loaded from persistence</description> </item>
        /// </list>
        /// </summary>
        /// <remarks>If its invoked in the constructor of the SynchronisedObject, it will be executed before super constructors of SynchronisedObject are called!</remarks>
        protected virtual void Constructor()
        {
            
        }
        
        public void Delete()
        {
            GetDatabase().Delete();
        }
        
        protected internal virtual void OnDelete()
        {
            _database = null;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is SynchronisedObject so) return so.Id == Id;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}