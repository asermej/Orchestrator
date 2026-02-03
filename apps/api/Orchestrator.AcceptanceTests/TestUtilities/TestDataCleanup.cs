using Npgsql;
using Dapper;
using System;

namespace Orchestrator.AcceptanceTests.TestUtilities;

/// <summary>
/// Manual test data cleanup utility for emergency/maintenance scenarios.
/// 
/// NOTE: Regular tests should NOT use this class. Instead, tests should:
/// 1. Track the IDs of entities they create
/// 2. Delete only those specific entities in TestCleanup
/// 
/// This utility is ONLY for:
/// - Manual database cleanup after test failures leave orphaned data
/// - Resetting test database to clean state during development
/// - Emergency cleanup scenarios
/// </summary>
public static class TestDataCleanup
{
    /// <summary>
    /// MANUAL ONLY: Removes data matching test patterns.
    /// Do NOT use in regular test Initialize/Cleanup methods.
    /// </summary>
    public static void ManualCleanupAllTestData(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var totalDeleted = 0;

            // Clean up agents (has FK to organizations - delete first)
            var agentsDeleted = TryExecute(connection, @"
                DELETE FROM agents 
                WHERE display_name LIKE 'Test%'
                   OR display_name LIKE 'SearchTest_%'
                   OR display_name LIKE 'Lifecycle_%'
                   OR display_name LIKE 'Updated_%'
                   OR display_name LIKE 'Yoda_%'
                   OR display_name LIKE 'JohnDoe_%'");
            totalDeleted += agentsDeleted;

            // Clean up organizations
            var organizationsDeleted = TryExecute(connection, @"
                DELETE FROM organizations 
                WHERE name LIKE 'TestOrg_%'");
            totalDeleted += organizationsDeleted;

            // Clean up users
            var usersDeleted = TryExecute(connection, @"
                DELETE FROM users 
                WHERE email LIKE '%@example.com'");
            totalDeleted += usersDeleted;

            if (totalDeleted > 0)
            {
                Console.WriteLine($"[ManualCleanup] Cleaned up {totalDeleted} total records:");
                Console.WriteLine($"  - Agents: {agentsDeleted}");
                Console.WriteLine($"  - Organizations: {organizationsDeleted}");
                Console.WriteLine($"  - Users: {usersDeleted}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ManualCleanup] Warning: Error during cleanup: {ex.Message}");
        }
    }

    private static int TryExecute(NpgsqlConnection connection, string sql)
    {
        try
        {
            return connection.Execute(sql);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01") // table does not exist
        {
            return 0;
        }
    }
}
