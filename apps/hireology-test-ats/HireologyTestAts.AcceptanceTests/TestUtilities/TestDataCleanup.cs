using Npgsql;
using Dapper;
using HireologyTestAts.AcceptanceTests.Domain;

namespace HireologyTestAts.AcceptanceTests.TestUtilities;

/// <summary>
/// Centralized test data cleanup utility.
///
/// Called in TestInitialize (to clean orphans from previous crashed/failed runs)
/// and TestCleanup (courtesy cleanup after each test) of every test class.
///
/// Strategy: pattern-based SQL cleanup.
/// All test groups have names starting with "TestGroup_".
/// All test organizations have names starting with "TestOrg_".
/// All test users have emails ending with "@test-example.com".
/// Cleanup finds those rows and deletes all child rows in reverse FK order.
/// </summary>
public static class TestDataCleanup
{
    /// <summary>
    /// Removes all test data from the database in correct FK dependency order.
    /// Uses ServiceLocatorForAcceptanceTesting to obtain the connection string.
    /// Safe to call repeatedly; skips tables that don't exist.
    /// </summary>
    public static void CleanupAllTestData()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        var connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        CleanupAllTestData(connectionString);
    }

    /// <summary>
    /// Removes all test data from the database using the provided connection string.
    /// Deletes in reverse FK dependency order (children before parents).
    /// </summary>
    public static void CleanupAllTestData(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            const string testOrgIds =
                "SELECT id FROM organizations WHERE name LIKE 'TestOrg_%'";

            const string testUserIds =
                "SELECT id FROM users WHERE email LIKE '%@test-example.com'";

            // 1. User access tables (join tables)
            TryExecute(connection,
                "DELETE FROM user_organization_access WHERE user_id IN (" + testUserIds + ")");
            TryExecute(connection,
                "DELETE FROM user_group_access WHERE user_id IN (" + testUserIds + ")");

            // 2. User sessions
            TryExecute(connection,
                "DELETE FROM user_sessions WHERE user_id IN (" + testUserIds + ")");

            // 3. Jobs (depend on organizations)
            TryExecute(connection,
                "DELETE FROM jobs WHERE organization_id IN (" + testOrgIds + ")");

            // 4. Organizations (depend on groups)
            TryExecute(connection,
                "DELETE FROM organizations WHERE name LIKE 'TestOrg_%'");

            // 5. Groups (root)
            TryExecute(connection,
                "DELETE FROM groups WHERE name LIKE 'TestGroup_%'");

            // 6. Users (by email pattern)
            TryExecute(connection,
                "DELETE FROM users WHERE email LIKE '%@test-example.com'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestDataCleanup] Warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes SQL, silently returning 0 when the table or column does not exist.
    /// </summary>
    private static int TryExecute(NpgsqlConnection connection, string sql)
    {
        try
        {
            return connection.Execute(sql);
        }
        catch (PostgresException)
        {
            return 0;
        }
    }
}
