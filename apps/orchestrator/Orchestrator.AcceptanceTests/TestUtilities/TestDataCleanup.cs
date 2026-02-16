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
/// Strategy: group-scoped SQL cleanup.
/// All test entities live under groups whose name starts with "TestOrg_"
/// (Orchestrator acceptance tests) or "TestGroup_" (synced from test ATS).
/// Cleanup finds those group IDs and deletes all child rows in reverse FK order,
/// then deletes the groups themselves and test users (by email pattern).
///
/// SCALABILITY: When a new entity type is added that references groups,
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

            // ── Building-block subqueries (group-scoped) ──────────────
            // Match both Orchestrator test groups (TestOrg_) and ATS-synced test groups (TestGroup_)
            const string testGroupIds =
                "SELECT id FROM groups WHERE name LIKE 'TestOrg_%' OR name LIKE 'TestGroup_%'";

            const string testJobIds =
                "SELECT id FROM jobs WHERE group_id IN (" + testGroupIds + ")";

            const string testInterviewIds =
                "SELECT id FROM interviews WHERE job_id IN (" + testJobIds + ")";

            const string testInterviewConfigIds =
                "SELECT id FROM interview_configurations WHERE group_id IN (" + testGroupIds + ")";

            const string testInterviewConfigQuestionIds =
                "SELECT id FROM interview_configuration_questions WHERE interview_configuration_id IN (" + testInterviewConfigIds + ")";

            const string testInterviewGuideIds =
                "SELECT id FROM interview_guides WHERE group_id IN (" + testGroupIds + ")";

            const string testInterviewGuideQuestionIds =
                "SELECT id FROM interview_guide_questions WHERE interview_guide_id IN (" + testInterviewGuideIds + ")";

            const string testAgentIds =
                "SELECT id FROM agents WHERE group_id IN (" + testGroupIds + ")";

            // ── Candidate session / invite subqueries ──────────────────────
            const string testInviteIds =
                "SELECT id FROM interview_invites WHERE group_id IN (" + testGroupIds + ")";

            // ── Delete in reverse FK order ───────────────────────────────────

            // 0a. Interview audit logs → interviews, invites, sessions
            TryExecute(connection,
                "DELETE FROM interview_audit_logs WHERE interview_id IN (" + testInterviewIds + ")");

            // 0b. Candidate sessions → invites, interviews
            TryExecute(connection,
                "DELETE FROM candidate_sessions WHERE invite_id IN (" + testInviteIds + ")");

            // 0c. Interview invites → interviews, groups
            TryExecute(connection,
                "DELETE FROM interview_invites WHERE group_id IN (" + testGroupIds + ")");

            // 1. Follow-up selection logs → interview_responses → interviews
            TryExecute(connection,
                "DELETE FROM follow_up_selection_logs WHERE interview_response_id IN (" +
                "SELECT id FROM interview_responses WHERE interview_id IN (" + testInterviewIds + "))");

            // 2a. Follow-up templates → interview_guide_questions
            TryExecute(connection,
                "DELETE FROM follow_up_templates WHERE interview_guide_question_id IN (" + testInterviewGuideQuestionIds + ")");

            // 2b. Follow-up templates → interview_configuration_questions
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

            // 7. Interview configurations → groups, agents, interview_guides
            TryExecute(connection,
                "DELETE FROM interview_configurations WHERE group_id IN (" + testGroupIds + ")");

            // 7a. Interview guide questions → interview_guides
            TryExecute(connection,
                "DELETE FROM interview_guide_questions WHERE interview_guide_id IN (" + testInterviewGuideIds + ")");

            // 7b. Interview guides → groups
            TryExecute(connection,
                "DELETE FROM interview_guides WHERE group_id IN (" + testGroupIds + ")");

            // 8. Applicants → groups
            TryExecute(connection,
                "DELETE FROM applicants WHERE group_id IN (" + testGroupIds + ")");

            // 9. Jobs → groups
            TryExecute(connection,
                "DELETE FROM jobs WHERE group_id IN (" + testGroupIds + ")");

            // 10. Consent audit → agents
            TryExecute(connection,
                "DELETE FROM consent_audit WHERE agent_id IN (" + testAgentIds + ")");

            // 11. Webhook deliveries → webhook_configs → groups
            TryExecute(connection,
                "DELETE FROM webhook_deliveries WHERE webhook_config_id IN (" +
                "SELECT id FROM webhook_configs WHERE group_id IN (" + testGroupIds + "))");
            TryExecute(connection,
                "DELETE FROM webhook_configs WHERE group_id IN (" + testGroupIds + ")");

            // 12. Agents → groups
            TryExecute(connection,
                "DELETE FROM agents WHERE group_id IN (" + testGroupIds + ")");

            // 13. Groups (root — Orchestrator test groups + ATS-synced test groups)
            TryExecute(connection,
                "DELETE FROM groups WHERE name LIKE 'TestOrg_%' OR name LIKE 'TestGroup_%'");

            // 14. Users (no group FK — identify by email pattern)
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
