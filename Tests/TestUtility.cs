using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Main.Utility;
using NUnit.Framework;

namespace Tests
{
    public static class TestUtility
    {
        public delegate object GetValueDelegate();

        private static readonly Stopwatch Stopwatch = new Stopwatch();
        
        public static void AreEqual(object expected, GetValueDelegate getValueDelegate, string testName = "Test", int timeInMs = 3000, int waitTimeInMs = 1)
        {
            int tryCount = 0;
            Stopwatch.Restart();

            //while test time didn't elapse
            while (Stopwatch.ElapsedMilliseconds < timeInMs)
            {
                //continue trying
                if (ObjectComparer.ObjectsAreEqual(expected, getValueDelegate.Invoke()))
                {
                    //log assertion success
                    Console.WriteLine(testName + " succeeded after " + GetElapsedTime(tryCount));
                    return;    
                }

                tryCount++;
                Thread.Sleep(waitTimeInMs);
            }
            
            //try assertion one last time
            Assert.AreEqual(expected, getValueDelegate.Invoke(), testName + " failed after " + GetElapsedTime(tryCount));
        }

        private static string GetElapsedTime(int tryCount)
        {
            Stopwatch.Stop();
            return $"[ms: {Stopwatch.ElapsedMilliseconds}, assertionTries: {tryCount}, ticks: {Stopwatch.ElapsedTicks}]";
        }

        [Test]
        public static void TestUtilityFunctions()
        {
            AreEqual(true, (() => true));
        }
    }
}