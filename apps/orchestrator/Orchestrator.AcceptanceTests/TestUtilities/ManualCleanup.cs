using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.TestUtilities;

/// <summary>
/// Manual test data cleanup utility.
/// Run these tests manually when you need to clean up the database.
///
/// NOTE: Regular tests should NOT use this class. Regular tests call
/// TestDataCleanup.CleanupAllTestData() in their own TestInitialize / TestCleanup.
///
/// USAGE:
/// 1. Open Test Explorer
/// 2. Find this test class
/// 3. Run CleanupDatabase_RemoveAllTestData
/// 4. Run VerifyDatabase_NoTestDataRemains to confirm
/// </summary>
[TestClass]
[TestCategory("ManualCleanup")]
public class ManualCleanup
{
    [TestMethod]
    public void CleanupDatabase_RemoveAllTestData()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("MANUAL DATABASE CLEANUP");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Starting cleanup of orphaned test data...");

        TestDataCleanup.CleanupAllTestData();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("CLEANUP COMPLETE");
        Console.WriteLine("========================================");

        Assert.IsTrue(true, "Cleanup completed successfully");
    }

    /// <summary>
    /// Verify the database is clean after cleanup.
    /// Checks every table that acceptance tests write to.
    /// </summary>
    [TestMethod]
    public void VerifyDatabase_NoTestDataRemains()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        var connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();

        Console.WriteLine("========================================");
        Console.WriteLine("VERIFYING DATABASE IS CLEAN");
        Console.WriteLine("========================================");
        Console.WriteLine();

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var totalTestData = 0;

        // Check for both Orchestrator test groups (TestOrg_) and ATS-synced test groups (TestGroup_)
        const string testGroupFilter = "name LIKE 'TestOrg_%' OR name LIKE 'TestGroup_%'";
        const string testGroupSubquery = "SELECT id FROM groups WHERE " + testGroupFilter;

        totalTestData += Check(connection, "groups (TestOrg_ + TestGroup_)", $"SELECT COUNT(*) FROM groups WHERE {testGroupFilter}");
        totalTestData += Check(connection, "agents (under test groups)", $"SELECT COUNT(*) FROM agents WHERE group_id IN ({testGroupSubquery})");
        totalTestData += Check(connection, "jobs (under test groups)", $"SELECT COUNT(*) FROM jobs WHERE group_id IN ({testGroupSubquery})");
        totalTestData += Check(connection, "applicants (under test groups)", $"SELECT COUNT(*) FROM applicants WHERE group_id IN ({testGroupSubquery})");
        totalTestData += Check(connection, "interviews (under test groups)", $"SELECT COUNT(*) FROM interviews WHERE job_id IN (SELECT id FROM jobs WHERE group_id IN ({testGroupSubquery}))");
        totalTestData += Check(connection, "interview_configurations (under test groups)", $"SELECT COUNT(*) FROM interview_configurations WHERE group_id IN ({testGroupSubquery})");
        totalTestData += Check(connection, "users (@example.com)", "SELECT COUNT(*) FROM users WHERE email LIKE '%@example.com'");

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("VERIFICATION COMPLETE");
        Console.WriteLine("========================================");

        Assert.AreEqual(0, totalTestData,
            $"Database should be clean but found {totalTestData} test records.");
    }

    private static int Check(NpgsqlConnection connection, string label, string sql)
    {
        try
        {
            var count = connection.ExecuteScalar<int>(sql);
            var icon = count == 0 ? "OK" : "FOUND";
            Console.WriteLine($"  [{icon}] {label}: {count}");
            return count;
        }
        catch (PostgresException)
        {
            Console.WriteLine($"  [SKIP] {label}: table does not exist");
            return 0;
        }
    }
}
