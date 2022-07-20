using System;
using System.Collections.Generic;
using System.Diagnostics;
using Data_Management_Project.Databases.Base;
using Main;
using NUnit.Framework;

namespace Tests
{
    public class SQLiteTests
    {
        [Test]
        public static void TableCreation()
        {
            //delete old data
            PersistentData.DeleteDatabase(nameof(TableCreation));

            int test1 = 1;
            string test2 = "my name isn't real";
            
            //save data
            PersistentData.CreateDatabase(nameof(TableCreation));
            Save(nameof(TableCreation), "Number one is saved", test1);
            Save(nameof(TableCreation), "Message", test2);
            
            //load it from database
            TestUtility.AreEqual(test1, () =>
            {
                PersistentData.TryLoad(nameof(TableCreation), "Number one is saved", out int result);
                return result;
            });
            
            
            TestUtility.AreEqual(true,
                () =>
                {
                    //wait until set value message was saved
                    bool success = PersistentData.TryLoad(nameof(TableCreation), 
                        "Message", out string result);
                    
                    //make sure values match
                    return success && result == test2;
                });
        }

        [Test]
        public static void OverwriteData()
        {
            //delete old data
            PersistentData.DeleteDatabase(nameof(OverwriteData));
            
            //create table
            PersistentData.CreateDatabase(nameof(OverwriteData));
            
            //save data multiple times, overwriting old values
            for (int i = 0; i < 10; i++)
            {
                Save(nameof(OverwriteData), nameof(OverwriteData), i);    
            }
            
            //make sure the most up to date value was saved
            TestUtility.AreEqual(9, () =>
            {
                PersistentData.TryLoad(nameof(OverwriteData), nameof(OverwriteData), out int result);
                return result;
            });
        }

        [Test]
        public static void CreateAndDeleteMultipleTimes()
        {
            //delete old data
            PersistentData.DeleteDatabase(nameof(CreateAndDeleteMultipleTimes));
            
            //create table
            PersistentData.CreateDatabase(nameof(CreateAndDeleteMultipleTimes));
            PersistentData.CreateDatabase(nameof(CreateAndDeleteMultipleTimes));
            
            //delete table
            PersistentData.DeleteDatabase(nameof(CreateAndDeleteMultipleTimes));
            PersistentData.DeleteDatabase(nameof(CreateAndDeleteMultipleTimes));
            
        }

        [Test]
        public static void DoesDatabaseExist()
        {
            //delete old data
            PersistentData.DeleteDatabase(nameof(DoesDatabaseExist));
            
            Assert.IsFalse(PersistentData.DoesDatabaseExist(nameof(DoesDatabaseExist)));
            
            PersistentData.CreateDatabase(nameof(DoesDatabaseExist));
            
            Assert.IsTrue(PersistentData.DoesDatabaseExist(nameof(DoesDatabaseExist)));
        }

        [Test]
        public static void TryLoadData()
        {
            //delete old database
            PersistentData.DeleteDatabase(nameof(TryLoadData));
            
            //no database: load fails
            Assert.IsFalse(PersistentData.TryLoad(nameof(TryLoadData), nameof(TryLoadData), out int test));
            
            PersistentData.CreateDatabase(nameof(TryLoadData));
            
            //no value saved: load fails
            Assert.IsFalse(PersistentData.TryLoad(nameof(TryLoadData), nameof(TryLoadData), out test));
            
            Save(nameof(TryLoadData), nameof(TryLoadData), 5);
            
            //data saved. Load succeeds
            TestUtility.AreEqual(true, () =>
            {
                bool success = PersistentData.TryLoad(nameof(TryLoadData), nameof(TryLoadData), out test);
                return success && test == 5;
            });
            
            PersistentData.DeleteDatabase(nameof(TryLoadData));
            
            //database deleted. load fails
            Assert.IsFalse(PersistentData.TryLoad(nameof(TryLoadData), nameof(TryLoadData), out test));
        }

        [Test]
        public static void LoadDatabase()
        {
            int saveCount = 10000;
            Stopwatch saveTime = new Stopwatch();
            Stopwatch loadTime = new Stopwatch();
            
            PersistentData.DeleteDatabase(nameof(LoadDatabase));
            
            //database doesnt exist
            Assert.IsFalse(PersistentData.TryLoadDatabase(nameof(LoadDatabase), out List<SavedObject> savedObjects));
            
            PersistentData.CreateDatabase(nameof(LoadDatabase));
            
            //database was loaded, but values are empty
            Assert.IsTrue(PersistentData.TryLoadDatabase(nameof(LoadDatabase), out savedObjects));
            Assert.AreEqual(0, savedObjects.Count);
            
            saveTime.Start();
            //make sure objects were saved correctly
            for (int i = 0; i < saveCount; i++)
            {
                Save(nameof(LoadDatabase), i.ToString(), i);
            }
            saveTime.Stop();
            
            Stopwatch internalSaveTime = Stopwatch.StartNew();
            
            //wait for data to be saved
            TestUtility.AreEqual(true, (() =>
            {
                loadTime.Restart();
                bool success = PersistentData.TryLoadDatabase(nameof(LoadDatabase), out savedObjects);
                loadTime.Stop();

                //keep testing if data wasn't saved internally yet
                if (!success || saveCount != savedObjects.Count) return false;
                
                internalSaveTime.Stop();
                return true;

            }), "Load saved objects");
            
            Console.WriteLine($"Saved {saveCount} items in {saveTime.ElapsedMilliseconds} ms. {(float) saveTime.ElapsedMilliseconds / saveCount} ms/item");
            Console.WriteLine($"Saved {saveCount} items internally in {internalSaveTime.ElapsedMilliseconds} ms. {(float) internalSaveTime.ElapsedMilliseconds / saveCount} ms/item");
            Console.WriteLine($"Loaded {saveCount} items in {loadTime.ElapsedMilliseconds} ms. {(float) loadTime.ElapsedMilliseconds / saveCount} ms/item");
        }

        private static void Save<T>(string databaseId, string valueStorageId, T value)
        {
            PersistentData.Save(databaseId, valueStorageId, Serialization.Serialize(value));
        }
    }
}