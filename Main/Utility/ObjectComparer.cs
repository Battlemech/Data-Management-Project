using System.Collections;

namespace Main.Utility
{
    public class ObjectComparer
    {
        public static bool ObjectsAreEqual(object object1, object object2)
        {
            if (object1 == null) return object2 == null;

            if (object1.Equals(object2)) return true;

            if (object1 is ICollection collection1 && object2 is ICollection collection2)
            {
                return CollectionsAreEqual(collection1, collection2);
            }

            return false;
        }
        
        public static bool CollectionsAreEqual(ICollection collection1, ICollection collection2)
        {
            //make sure both connections contain the same amount of elements
            if (collection1.Count != collection2.Count) return false;
            
            //return true if both collections are empty and of the same type
            if(collection1.Count == 0 && collection1.GetType() == collection2.GetType()) return true;
                
            //if both collections are dictionaries: compare keys and values separately
            if (collection1 is IDictionary dictionary1 && collection2 is IDictionary dictionary2)
            {
                return CollectionsAreEqual(dictionary1.Keys, dictionary2.Keys) &&
                       CollectionsAreEqual(dictionary1.Values, dictionary2.Values);
            }
            
            //compare each element
            var enumerator1 = collection1.GetEnumerator();
            var enumerator2 = collection2.GetEnumerator();

            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                object object1 = enumerator1.Current;
                object object2 = enumerator2.Current;
                
                //check for null exceptions
                if(object1 == null && object2 == null) continue;
                if (object1 == null || object2 == null)
                {
                    return false;
                }
                
                //if objects are equal: continue
                if (object1.Equals(object2)) continue;
                
                //check if the two objects are collections
                if (object1 is ICollection c1 && object2 is ICollection c2)
                {
                    if(CollectionsAreEqual(c1, c2)) continue;
                }

                //objects must be different
                return false;
            }

            return true;
        }
    }
}