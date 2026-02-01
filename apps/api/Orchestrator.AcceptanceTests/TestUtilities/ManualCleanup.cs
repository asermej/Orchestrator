using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;

namespace Orchestrator.AcceptanceTests.TestUtilities;

/// <summary>
/// Manual test data cleanup utility.
/// Run this test manually when you need to clean up the database.
/// 
/// USAGE:
/// 1. Open Test Explorer in Visual Studio or your IDE
/// 2. Find this test class
/// 3. Run the CleanupDatabase_RemoveAllTestData test
/// 4. Check the output for cleanup results
/// </summary>
[TestClass]
[TestCategory("ManualCleanup")]
public class ManualCleanup
{
    /// <summary>
    /// Manually clean up all test data from the database.
    /// This is useful when:
    /// - Tests have been failing and leaving data behind
    /// - You want to start with a clean slate
    /// - You're seeing leftover test data like "TestPersona638981699624283760"
    /// </summary>
    [TestMethod]
    public void CleanupDatabase_RemoveAllTestData()
    {
        // Arrange
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        var connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        
        Console.WriteLine("========================================");
        Console.WriteLine("MANUAL DATABASE CLEANUP");
        Console.WriteLine("========================================");
        Console.WriteLine($"Connection String: {connectionString}");
        Console.WriteLine();
        
        // Act
        Console.WriteLine("Starting comprehensive cleanup...");
        TestDataCleanup.CleanupAllTestData(connectionString);
        
        Console.WriteLine();
        Console.WriteLine("Starting training file cleanup...");
        TestDataCleanup.CleanupTestTrainingFiles();
        
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("CLEANUP COMPLETE");
        Console.WriteLine("========================================");
        Console.WriteLine("Check the output above for details on what was cleaned up.");
        Console.WriteLine();
        
        // Assert
        Assert.IsTrue(true, "Cleanup completed successfully");
    }
    
    /// <summary>
    /// AGGRESSIVE cleanup - removes ANY data that looks like test data.
    /// Use this with CAUTION - it uses very permissive patterns.
    /// </summary>
    [TestMethod]
    [TestCategory("AggressiveCleanup")]
    public void CleanupDatabase_AggressiveRemoveAllTestData()
    {
        // Arrange
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        var connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        
        Console.WriteLine("========================================");
        Console.WriteLine("⚠️  AGGRESSIVE DATABASE CLEANUP ⚠️");
        Console.WriteLine("========================================");
        Console.WriteLine("WARNING: This will remove ANY data that looks like test data!");
        Console.WriteLine($"Connection String: {connectionString}");
        Console.WriteLine();
        
        // Act
        Console.WriteLine("Starting AGGRESSIVE cleanup...");
        TestDataCleanup.AggressiveCleanup(connectionString);
        
        Console.WriteLine();
        Console.WriteLine("Starting training file cleanup...");
        TestDataCleanup.CleanupTestTrainingFiles();
        
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("AGGRESSIVE CLEANUP COMPLETE");
        Console.WriteLine("========================================");
        
        // Assert
        Assert.IsTrue(true, "Aggressive cleanup completed successfully");
    }
    
    /// <summary>
    /// Verify the database is clean after cleanup.
    /// Run this after cleanup to confirm no test data remains.
    /// </summary>
    [TestMethod]
    [TestCategory("ManualCleanup")]
    public void VerifyDatabase_NoTestDataRemains()
    {
        // Arrange
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        var domainFacade = new Orchestrator.Domain.DomainFacade(serviceLocator);
        
        Console.WriteLine("========================================");
        Console.WriteLine("VERIFYING DATABASE IS CLEAN");
        Console.WriteLine("========================================");
        Console.WriteLine();
        
        try
        {
            // Check for test personas
            Console.WriteLine("Checking for test personas...");
            var personas = domainFacade.SearchPersonas(null, null, "Test", null, null, null, 1, 100).Result;
            if (personas.TotalCount > 0)
            {
                Console.WriteLine($"❌ Found {personas.TotalCount} test personas:");
                foreach (var p in personas.Items)
                {
                    Console.WriteLine($"  - {p.DisplayName} (ID: {p.Id})");
                }
            }
            else
            {
                Console.WriteLine("✅ No test personas found");
            }
            Console.WriteLine();
            
            // Check for test chats
            Console.WriteLine("Checking for test chats...");
            var chats = domainFacade.SearchChats(null, null, "Test", 1, 100).Result;
            if (chats.TotalCount > 0)
            {
                Console.WriteLine($"❌ Found {chats.TotalCount} test chats:");
                foreach (var c in chats.Items)
                {
                    Console.WriteLine($"  - {c.Title} (ID: {c.Id})");
                }
            }
            else
            {
                Console.WriteLine("✅ No test chats found");
            }
            Console.WriteLine();
            
            // Check for test topics
            Console.WriteLine("Checking for test topics...");
            var topics = domainFacade.SearchTopics("Test", null, 1, 100).Result;
            if (topics.TotalCount > 0)
            {
                Console.WriteLine($"❌ Found {topics.TotalCount} test topics:");
                foreach (var t in topics.Items)
                {
                    Console.WriteLine($"  - {t.Name} (ID: {t.Id})");
                }
            }
            else
            {
                Console.WriteLine("✅ No test topics found");
            }
            Console.WriteLine();
            
            // Check for test users
            Console.WriteLine("Checking for test users...");
            var users = domainFacade.SearchUsers("Test", null, null, 1, 100).Result;
            if (users.TotalCount > 0)
            {
                Console.WriteLine($"❌ Found {users.TotalCount} test users:");
                foreach (var u in users.Items)
                {
                    Console.WriteLine($"  - {u.FirstName} {u.LastName} ({u.Email}) (ID: {u.Id})");
                }
            }
            else
            {
                Console.WriteLine("✅ No test users found");
            }
            Console.WriteLine();
            
            Console.WriteLine("========================================");
            Console.WriteLine("VERIFICATION COMPLETE");
            Console.WriteLine("========================================");
            
            // Assert
            var totalTestData = personas.TotalCount + chats.TotalCount + topics.TotalCount + users.TotalCount;
            Assert.AreEqual(0, totalTestData, 
                $"Database should be clean but found {totalTestData} test records. See output above for details.");
        }
        finally
        {
            domainFacade?.Dispose();
        }
    }
}

