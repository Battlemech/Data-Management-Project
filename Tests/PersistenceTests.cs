using System;
using System.Collections.Generic;
using System.Threading;
using Data_Management_Project.Databases.Base;
using Main.Databases;
using NUnit.Framework;

namespace Tests
{
    public class PersistenceTests
    {
        [Test]
        public static void TestPersistence()
        {
            string id = nameof(TestPersistence);
            
            //make sure the test is unaffected by old data
            PersistentData.DeleteDatabase(id);

            for (int i = 0; i < 10; i++)
            {
                Database database = new Database(id, true, true);
                
                //load expected old value
                Assert.AreEqual(i, database.Get<int>(id));
                
                database.Set(id, i + 1);

                //make sure the value has been updated correctly in database
                Assert.AreEqual(i + 1, database.Get<int>(id));
                
                //make sure the value was saved correctly in persistent data
                TestUtility.AreEqual(true, () => PersistentData.TryLoad(id, id, out int value) && value == i + 1, "PersistentSave");
            }
        }
    }
}