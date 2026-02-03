using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;

namespace Orchestrator.AcceptanceTests.TestUtilities;

/// <summary>
/// Manual test data cleanup utility.
/// Run this test manually when you need to clean up the database.
/// 
/// NOTE: Regular tests should NOT use this. Tests should clean up only 
/// the specific data they create by tracking entity IDs.
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
    /// - You're seeing leftover test data
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
        Console.WriteLine();
        
        // Act
        Console.WriteLine("Starting cleanup of orphaned test data...");
        TestDataCleanup.ManualCleanupAllTestData(connectionString);
        
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("CLEANUP COMPLETE");
        Console.WriteLine("========================================");
        
        // Assert
        Assert.IsTrue(true, "Cleanup completed successfully");
    }
    
    /// <summary>
    /// Verify the database is clean after cleanup.
    /// Run this after cleanup to confirm no test data remains.
    /// </summary>
    [TestMethod]
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
            // Check for test agents (using patterns that tests create)
            Console.WriteLine("Checking for test agents...");
            var agents = domainFacade.SearchAgents(null, "Test", null, null, 1, 100).Result;
            var searchTestAgents = domainFacade.SearchAgents(null, "SearchTest_", null, null, 1, 100).Result;
            var totalAgents = agents.TotalCount + searchTestAgents.TotalCount;
            
            if (totalAgents > 0)
            {
                Console.WriteLine($"❌ Found {totalAgents} test agents");
            }
            else
            {
                Console.WriteLine("✅ No test agents found");
            }
            Console.WriteLine();
            
            // Check for test users (SearchUsers searches by phone, email, lastName)
            Console.WriteLine("Checking for test users...");
            var users = domainFacade.SearchUsers(null, "@example.com", null, 1, 100).Result;
            if (users.TotalCount > 0)
            {
                Console.WriteLine($"❌ Found {users.TotalCount} test users");
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
            var totalTestData = totalAgents + users.TotalCount;
            Assert.AreEqual(0, totalTestData, 
                $"Database should be clean but found {totalTestData} test records.");
        }
        finally
        {
            domainFacade?.Dispose();
        }
    }
}
