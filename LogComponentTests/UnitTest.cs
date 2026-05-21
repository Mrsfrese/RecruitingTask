using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using LogTest;

namespace LogComponentTests
{
    [TestClass]
    public class AsyncLogInterfaceTests
    {
        private string _testDirectory = null!;


        // creates a unique temporary test directory befor each test
        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(_testDirectory);
        }

        // Cleans up the test directory after each test
        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        // Verifies that WriteLog actually writes to a file
        [TestMethod]
        public void WriteLog_WithFlush_WritesToFile()
        {
            // Arrange
            FakeClock clock = new FakeClock
            {
                Now = new DateTime(2026, 5, 19, 12, 0, 0)
            };

            AsyncLogInterface logger =
                new AsyncLogInterface(_testDirectory, clock);

            // Act
            logger.WriteLog("Test log message");
            logger.Stop_With_Flush();

            // Assert
            string file = Directory.GetFiles(_testDirectory).Single();
            string content = File.ReadAllText(file);

            StringAssert.Contains(content, "Test log message");
        }

        // Verifies that crossing midnight creates a new log file for the new date
        [TestMethod]
        public void CrossingMidnight_CreatesNewFile()
        {
            // Arrange
            FakeClock clock = new FakeClock
            {
                Now = new DateTime(2026, 5, 19, 23, 59, 59)
            };

            AsyncLogInterface logger =
                new AsyncLogInterface(_testDirectory, clock);

            // Act
            logger.WriteLog("Before midnight");

            clock.Now = new DateTime(2026, 5, 20, 0, 0, 1);

            logger.WriteLog("After midnight");

            logger.Stop_With_Flush();

            // Assert
            string[] files = Directory.GetFiles(_testDirectory);

            Assert.AreEqual(2, files.Length);
            CollectionAssert.Contains(files, Path.Combine(_testDirectory, "Log20260519.log"));
            CollectionAssert.Contains(files, Path.Combine(_testDirectory, "Log20260520.log"));
        }

        // Verifies that Stop_With_Flush waits for all outstanding logs to be written to the file
        [TestMethod]
        public void StopWithFlush_WritesOutstandingLogs()
        {
            // Arrange
            FakeClock clock = new FakeClock
            {
                Now = new DateTime(2026, 5, 19, 12, 0, 0)
            };

            AsyncLogInterface logger =
                new AsyncLogInterface(_testDirectory, clock);

            // Act
            for (int i = 0; i < 100; i++)
            {
                logger.WriteLog("Message " + i);
            }

            logger.Stop_With_Flush();

            // Assert
            string file = Directory.GetFiles(_testDirectory).Single();
            string content = File.ReadAllText(file);

            StringAssert.Contains(content, "Message 0");
            StringAssert.Contains(content, "Message 99");
        }

        // Verifies that Stop_Without_Flush does not write outstanding logs to the file
        [TestMethod]
        public void StopWithoutFlush_DiscardsOutstandingLogs()
        {
            // Arrange
            FakeClock clock = new FakeClock
            {
                Now = new DateTime(2026, 5, 19, 12, 0, 0)
            };

            AsyncLogInterface logger =
                new AsyncLogInterface(_testDirectory, clock);

            // Act
            for (int i = 0; i < 10000; i++)
            {
                logger.WriteLog("Message " + i);
            }

            logger.Stop_Without_Flush();

            // Assert
            string[] files = Directory.GetFiles(_testDirectory);

            if (files.Length == 0)
                return;

            string content = File.ReadAllText(files.Single());

            Assert.IsFalse(content.Contains("Message 9999"));
        }
    }
}