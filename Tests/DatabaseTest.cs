using System;
using System.Collections.Generic;
using System.Diagnostics;
using DMP.Databases;
using DMP.Databases.VS;
using DMP.Persistence;
using NUnit.Framework;

namespace Tests
{
    public static class DatabaseTest
    {
        [Test]
        public static void Persistence()
        {
            //delete old persistent data
            PersistentData.DeleteDatabase("Test");
            
            for (int i = 0; i < 10; i++)
            {
                Database database = new Database("Test", true);
                
                //get previous value
                Assert.AreEqual(i, database.Get<int>("Int").Get());
                
                //increment value by 1
                database.Get<int>("Int").Set(i + 1);

                //save values persistently
                database.Save();
                
                //make sure they were saved persistently
                TestUtility.AreEqual(true, 
                    (() => PersistentData.TryLoad("Test", "Int", out int loaded) && loaded == i + 1));
            }
            
        }
    }
}