using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Main;
using Main.Utility;
using NUnit.Framework;

namespace Tests
{
    public static class TestUtility
    {
        public delegate object GetValueDelegate();
        public delegate T GetValueDelegate<T>();

        //port tracking
        private static readonly Dictionary<string, int> _ports = new Dictionary<string, int>();
        private static int _currentPort = Options.DefaultPort;
        
        public static void AreEqual(object expected, GetValueDelegate getValueDelegate, string testName = "Test", int timeInMs = 3000, int waitTimeInMs = 1)
        {
            int tryCount = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            //while test time didn't elapse
            while (stopwatch.ElapsedMilliseconds < timeInMs)
            {
                //continue trying
                if (ObjectComparer.ObjectsAreEqual(expected, getValueDelegate.Invoke()))
                {
                    //log assertion success
                    Console.WriteLine(testName + " succeeded after " + GetElapsedTime(tryCount, stopwatch));
                    return;    
                }

                tryCount++;
                Thread.Sleep(waitTimeInMs);
            }
            
            //try assertion one last time
            Assert.AreEqual(expected, getValueDelegate.Invoke(), testName + " failed after " + GetElapsedTime(tryCount, stopwatch));
        }

        public static void IsChanging(int expected, GetValueDelegate<int> valueDelegate, string testName = "Test",
            int waitTime = 1000, int maxFailCount = 10)
        {
            int tryCount = 0;
            int failCount = 0;
            
            //track value
            int startValue = valueDelegate.Invoke();
            List<int> averageChange = new List<int>();
            
            int current = startValue;
            
            //start tracking time
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (current != expected)
            {
                //wait for value to change
                Thread.Sleep(waitTime + tryCount);
                
                //invoke test again
                int newValue = valueDelegate.Invoke();
                
                //Make sure value changed while waiting
                if (newValue == current)
                {
                    failCount++;
                    if (failCount >= maxFailCount)
                    {
                        //try assertion one last time
                        Assert.AreEqual(expected, valueDelegate.Invoke(),
                            testName + " failed after " + GetElapsedTime(tryCount, stopwatch) + 
                            ". Average change per try: " + averageChange.Average());
                    }
                }
                
                averageChange.Add(newValue - current);
                
                current = newValue;
                
                //increment try count
                tryCount++;
            }
            
            Console.WriteLine($"{testName} succeeded after {GetElapsedTime(tryCount, stopwatch)}");
        }

        private static string GetElapsedTime(int tryCount, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            return $"[ms: {stopwatch.ElapsedMilliseconds}, assertionTries: {tryCount}, ticks: {stopwatch.ElapsedTicks}]";
        }

        [Test]
        public static void TestUtilityFunctions()
        {
            AreEqual(true, (() => true));
        }

        public static int GetPort(string className, string testName)
        {
            string id = className + testName;
            
            //try get port
            if (_ports.TryGetValue(id, out int port)) return port;

            //add new port
            port = _currentPort;
            _ports.Add(id, port);

            //increase currently used port
            _currentPort++;
            
            return port;
        }
    }
}