using System.Collections.Generic;
using DMP.Databases.ValueStorage;

namespace DMP.Databases.Utility
{
    public static class DatabaseCollectionOperations
    {
        public static void Add<TCollection, TValue>(this Database database, string id, TValue toAdd) 
            where TCollection : ICollection<TValue>, new()
        {
            database.Modify<TCollection>(id, value =>
            {
                //initialize collection if necessary
                value ??= new TCollection();
                
                //add value
                value.Add(toAdd);
                return value;
            });
        }

        public static void Add<TCollection, TValue>(this ValueStorage<TCollection> valueStorage, TValue toAdd)
            where TCollection : ICollection<TValue>, new()
        {
            valueStorage.Modify((value =>
            {
                //initialize collection if necessary
                value ??= new TCollection();
                
                //add value
                value.Add(toAdd);
                return value;
            }));
        }

        public static void Add<TCollection, TKey, TValue>(this ValueStorage<TCollection> valueStorage, TKey key,
            TValue value) where TCollection : IDictionary<TKey, TValue>
        {
            valueStorage.Modify((dictionary =>
            {
                dictionary.Add(key, value);
                return dictionary;
            }));
        }

        public static void RemoveKey<TCollection, TKey, TValue>(this ValueStorage<TCollection> valueStorage, TKey key) 
            where TCollection : IDictionary<TKey, TValue>
        {
            valueStorage.Modify((dictionary =>
            {
                dictionary.Remove(key);
                return dictionary;
            }));
        }
        
        public static void Remove<TCollection, TValue>(this Database database, string id, TValue toRemove) 
            where TCollection : ICollection<TValue>, new()
        {
            database.Modify<TCollection>(id, value =>
            {
                //if database doesnt exist: Create empty
                if (value == null) return new TCollection();
                
                //remove value
                value.Remove(toRemove);
                return value;
            });
        }

        public static void Remove<TCollection, TValue>(this ValueStorage<TCollection> valueStorage, TValue toRemove)
            where TCollection : ICollection<TValue>, new()
        {
            valueStorage.Modify((value =>
            {
                //if database doesnt exist: Create empty
                if (value == null) return new TCollection();
                
                //remove value
                value.Remove(toRemove);
                return value;
            }));
        }
    }
}