using System;
using Main.Databases;
using NUnit.Framework;

namespace Tests
{
    public static class CallbackTests
    {
        [Test]
        public static void TestCallbacks()
        {
            string id = nameof(TestCallbacks);
            
            Database database = new Database(id);
            
            //save string length in second database variable
            database.AddCallback<string>("string", (data) =>
            {
                database.Set("stringLength", data.Length);
            });

            foreach (var input in new []{"", "test", "my master", "fck yeah boy"})
            {
                database.Set("string", input);
                TestUtility.AreEqual(input.Length, () =>
                {
                    return database.Get<int>("stringLength");
                });
            }
            
        }

        [Test]
        public static void TestGetNull()
        {
            Database database = new Database(nameof(TestGetNull));
            Console.WriteLine(database.Get<int>(""));
            Console.WriteLine(database.Get<int>(""));
        }
    }
}