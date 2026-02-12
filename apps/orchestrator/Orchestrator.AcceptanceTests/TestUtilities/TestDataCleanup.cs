using Npgsql;
using Dapper;
using System;
using Orchestrator.AcceptanceTests.Domain;

namespace Orchestrator.AcceptanceTests.TestUtilities;

/// <summary>
/// Centralized test data cleanup utility.
///
/// Called in TestInitialize (to clean orphans from previous crashed/failed runs)
/// and TestCleanup (courtesy cleanup after each test) of every test class.
///
/// Strategy: organization-scoped SQL cleanup.
/// All test entities live under organizations whose name starts with "TestOrg_".
/// Cleanup finds those org IDs and deletes all child rows in reverse FK order,
/// then deletes the organizations themselves and test users (by email pattern).
///
/// SCALABILITY: When a new entity type is added that references organizations,
/// add a single DELETE statement to this method in the correct FK position.
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

            // ── Building-block subqueries (organization-scoped) ──────────────
            const string testOrgIds =
                "SELECT id FROM organizations WHERE name LIKE 'TestOrg_%'";

            const string testJobIds =
                "SELECT id FROM jobs WHERE organization_id IN (" + testOrgIds + ")";

            const string testInterviewIds =
                "SELECT id FROM interviews WHERE job_id IN (" + testJobIds + ")";

            const string testInterviewConfigIds =
                "SELECT id FROM interview_configurations WHERE organization_id IN (" + testOrgIds + ")";

            const string testInterviewConfigQuestionIds =
                "SELECT id FROM interview_configuration_questions WHERE interview_configuration_id IN (" + testInterviewConfigIds + ")";

            const string testAgentIds =
                "SELECT id FROM agents WHERE organization_id IN (" + testOrgIds + ")";

            // ── Delete in reverse FK order ───────────────────────────────────

            // 1. Follow-up selection logs → interview_responses → interviews
            TryExecute(connection,
                "DELETE FROM follow_up_selection_logs WHERE interview_response_id IN (" +
                "SELECT id FROM interview_responses WHERE interview_id IN (" + testInterviewIds + "))");

            // 2. Follow-up templates → interview_configuration_questions
            TryExecute(connection,
                "DELETE FROM follow_up_templates WHERE interview_config_question_id IN (" + testInterviewConfigQuestionIds + ")");

            // 3. Interview responses → interviews
            TryExecute(connection,
                "DELETE FROM interview_responses WHERE interview_id IN (" + testInterviewIds + ")");

            // 4. Interview results → interviews
            TryExecute(connection,
                "DELETE FROM interview_results WHERE interview_id IN (" + testInterviewIds + ")");

            // 5. Interviews → jobs, applicants, agents
            TryExecute(connection,
                "DELETE FROM interviews WHERE job_id IN (" + testJobIds + ")");

            // 6. Interview configuration questions → interview_configurations
            TryExecute(connection,
                "DELETE FROM interview_configuration_questions WHERE interview_configuration_id IN (" + testInterviewConfigIds + ")");

            // 7. Interview configurations → organizations, agents
            TryExecute(connection,
                "DELETE FROM interview_configurations WHERE organization_id IN (" + testOrgIds + ")");

            // 8. Applicants → organizations
            TryExecute(connection,
                "DELETE FROM applicants WHERE organization_id IN (" + testOrgIds + ")");

            // 9. Jobs → organizations
            TryExecute(connection,
                "DELETE FROM jobs WHERE organization_id IN (" + testOrgIds + ")");

            // 10. Consent audit → agents
            TryExecute(connection,
                "DELETE FROM consent_audit WHERE agent_id IN (" + testAgentIds + ")");

            // 11. Webhook deliveries → webhook_configs → organizations
            TryExecute(connection,
                "DELETE FROM webhook_deliveries WHERE webhook_config_id IN (" +
                "SELECT id FROM webhook_configs WHERE organization_id IN (" + testOrgIds + "))");
            TryExecute(connection,
                "DELETE FROM webhook_configs WHERE organization_id IN (" + testOrgIds + ")");

            // 12. Agents → organizations
            TryExecute(connection,
                "DELETE FROM agents WHERE organization_id IN (" + testOrgIds + ")");

            // 13. Organizations (root)
            TryExecute(connection,
                "DELETE FROM organizations WHERE name LIKE 'TestOrg_%'");

            // 14. Users (no org FK — identify by email pattern)
            TryExecute(connection,
                "DELETE FROM users WHERE email LIKE '%@example.com'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestDataCleanup] Warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Legacy alias kept for ManualCleanup compatibility.
    /// </summary>
    public static void ManualCleanupAllTestData(string connectionString)
        => CleanupAllTestData(connectionString);

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
            // Table or column may not exist in this DB version — skip gracefully
            return 0;
        }
    }
}
