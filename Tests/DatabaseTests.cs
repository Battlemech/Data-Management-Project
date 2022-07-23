using Main.Databases;
using NUnit.Framework;

namespace Tests
{
    public static class BaseTests
    {
        [Test]
        public static void TestGetNull()
        {
            Database database = new Database(nameof(TestGetNull));
            Assert.AreEqual(0, database.Get<int>(""));
            Assert.AreEqual(0, database.Get<int>(""));
        }
    }
}