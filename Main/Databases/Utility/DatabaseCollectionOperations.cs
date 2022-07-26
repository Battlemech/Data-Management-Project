using System.Collections.Generic;

namespace Main.Databases.Utility
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
    }
}