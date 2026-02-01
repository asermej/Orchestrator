using Npgsql;
using Dapper;
using System;

namespace Orchestrator.AcceptanceTests.TestUtilities;

/// <summary>
/// Centralized test data cleanup utility for all acceptance tests.
/// 
/// PURPOSE:
/// - Ensures ALL test data is cleaned up regardless of which test file created it
/// - Handles cleanup in correct order respecting foreign key constraints
/// - Uses comprehensive patterns to catch all test data variations
/// - Prevents test data accumulation from failed/interrupted tests
/// 
/// USAGE:
/// Call CleanupAllTestData() in both TestInitialize and TestCleanup of every test class
/// </summary>
public static class TestDataCleanup
{
    /// <summary>
    /// Comprehensive cleanup of ALL test data across all entities.
    /// Cleans up in reverse order of foreign key dependencies to avoid constraint violations.
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    public static void CleanupAllTestData(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var totalDeleted = 0;

            // PHASE 1: Delete dependent data (children first)
            
            // Clean up messages (depends on chats)
            var messagesDeleted = connection.Execute(@"
                DELETE FROM messages 
                WHERE content LIKE 'Test%'
                   OR content LIKE 'test%'
                   OR content LIKE 'User message%'
                   OR content LIKE 'Hello%'
                   OR content LIKE '%simulated%'
                   OR content LIKE '%Searchable%'
                   OR content LIKE '%Lifecycle%'
                   OR chat_id IN (SELECT id FROM chats WHERE title LIKE 'Test%' OR title LIKE '%Force%' OR title LIKE '%High School%')");
            totalDeleted += messagesDeleted;

            // Clean up chats (depends on personas and users)
            var chatsDeleted = connection.Execute(@"
                DELETE FROM chats 
                WHERE title LIKE 'Test%'
                   OR title LIKE 'test%'
                   OR title LIKE 'Search%'
                   OR title LIKE 'Update%'
                   OR title LIKE 'Delete%'
                   OR title LIKE 'Lifecycle%'
                   OR title LIKE '%Force%'
                   OR title LIKE '%High School%'
                   OR title LIKE '%Chat%'
                   OR persona_id IN (SELECT id FROM personas WHERE display_name LIKE 'Test%')");
            totalDeleted += chatsDeleted;

            // Clean up voice_clone_jobs (depends on personas; FK to personas)
            var voiceCloneJobsDeleted = connection.Execute(@"
                DELETE FROM voice_clone_jobs 
                WHERE persona_id IN (SELECT id FROM personas WHERE display_name LIKE 'Test%' OR display_name LIKE 'test%' OR display_name LIKE 'Search%' OR display_name LIKE 'Update%' OR display_name LIKE 'Delete%' OR display_name LIKE 'Yoda%' OR display_name LIKE 'displayname%' OR display_name LIKE 'Lifecycle%' OR first_name LIKE 'Test%' OR first_name LIKE 'test%' OR first_name LIKE 'firstname%' OR last_name LIKE 'Test%' OR last_name LIKE 'test%' OR last_name LIKE 'lastname%' OR last_name LIKE 'Persona%')");
            totalDeleted += voiceCloneJobsDeleted;

            // Clean up consent_audit (depends on personas; FK to personas)
            var consentAuditDeleted = connection.Execute(@"
                DELETE FROM consent_audit 
                WHERE persona_id IN (SELECT id FROM personas WHERE display_name LIKE 'Test%' OR display_name LIKE 'test%' OR display_name LIKE 'Search%' OR display_name LIKE 'Update%' OR display_name LIKE 'Delete%' OR display_name LIKE 'Yoda%' OR display_name LIKE 'displayname%' OR display_name LIKE 'Lifecycle%' OR first_name LIKE 'Test%' OR first_name LIKE 'test%' OR first_name LIKE 'firstname%' OR last_name LIKE 'Test%' OR last_name LIKE 'test%' OR last_name LIKE 'lastname%' OR last_name LIKE 'Persona%')");
            totalDeleted += consentAuditDeleted;

            // Clean up persona_categories (depends on personas and categories)
            var personaCategoriesDeleted = connection.Execute(@"
                DELETE FROM persona_categories 
                WHERE persona_id IN (SELECT id FROM personas WHERE display_name LIKE 'Test%')
                   OR category_id IN (SELECT id FROM categories WHERE name LIKE 'TestCat%')");
            totalDeleted += personaCategoriesDeleted;

            // Clean up topic_tags (depends on topics and tags)
            var topicTagsDeleted = connection.Execute(@"
                DELETE FROM topic_tags 
                WHERE topic_id IN (SELECT id FROM topics WHERE name LIKE 'Test%')
                   OR tag_id IN (SELECT id FROM tags WHERE name LIKE 'Test%')");
            totalDeleted += topicTagsDeleted;

            // PHASE 2: Delete parent entities
            
            // Clean up personas
            var personasDeleted = connection.Execute(@"
                DELETE FROM personas 
                WHERE display_name IN ('TestPersona', 'SearchPersona', 'UpdatePersona', 'DeletePersona', 'Madonna', 'Cher')
                   OR display_name LIKE 'Test%'
                   OR display_name LIKE 'test%'
                   OR display_name LIKE 'Search%'
                   OR display_name LIKE 'Update%'
                   OR display_name LIKE 'Delete%'
                   OR display_name LIKE 'Yoda%'
                   OR display_name LIKE 'displayname%'
                   OR display_name LIKE 'Lifecycle%'
                   OR first_name LIKE 'Test%'
                   OR first_name LIKE 'test%'
                   OR first_name LIKE 'firstname%'
                   OR last_name LIKE 'Test%'
                   OR last_name LIKE 'test%'
                   OR last_name LIKE 'lastname%'
                   OR last_name LIKE 'Persona%'");
            totalDeleted += personasDeleted;

            // Clean up topics
            var topicsDeleted = connection.Execute(@"
                DELETE FROM topics 
                WHERE name LIKE 'Test%'
                   OR name LIKE 'test%'
                   OR name LIKE 'Search%'
                   OR name LIKE 'Update%'
                   OR name LIKE 'Delete%'
                   OR name LIKE 'Lifecycle%'
                   OR name LIKE 'TopicTag%'");
            totalDeleted += topicsDeleted;

            // Clean up tags (comprehensive patterns for all test tags)
            var tagsDeleted = connection.Execute(@"
                DELETE FROM tags 
                WHERE name LIKE 'Test%'
                   OR name LIKE 'test%'
                   OR name LIKE 'Search%'
                   OR name LIKE 'Update%'
                   OR name LIKE 'Delete%'
                   OR name LIKE 'Lifecycle%'
                   OR name LIKE '%delete%'
                   OR name LIKE 'searchtesttag%'
                   OR LOWER(name) IN ('duplicatetag', 'celtics', 'searchbyname', 'basketball', 'differenttag', 'existingtag', 'newtag', 'searchtesttag')");
            totalDeleted += tagsDeleted;

            // Clean up categories (be careful not to delete seed data like "General")
            var categoriesDeleted = connection.Execute(@"
                DELETE FROM categories 
                WHERE name LIKE 'Test%'
                   OR name LIKE 'test%'
                   OR name LIKE '%Test%'
                   OR name LIKE 'Search%'
                   OR name LIKE 'Duplicate%'
                   OR name LIKE 'Lifecycle%'
                   OR name IN ('OtherCategory', 'InactiveCategory', 'AnotherCategoryForUpdate')");
            totalDeleted += categoriesDeleted;

            // Clean up users (only test users with obvious test patterns)
            var usersDeleted = connection.Execute(@"
                DELETE FROM users 
                WHERE email LIKE '%@example.com'
                   OR email LIKE '%@test.com'
                   OR email LIKE '%@unittest.com'
                   OR email LIKE 'test%'
                   OR first_name LIKE 'Test%'
                   OR first_name LIKE 'test%'");
            totalDeleted += usersDeleted;

            if (totalDeleted > 0)
            {
                Console.WriteLine($"[TestDataCleanup] Cleaned up {totalDeleted} total test records:");
                Console.WriteLine($"  - Messages: {messagesDeleted}");
                Console.WriteLine($"  - Chats: {chatsDeleted}");
                Console.WriteLine($"  - VoiceCloneJobs: {voiceCloneJobsDeleted}");
                Console.WriteLine($"  - ConsentAudit: {consentAuditDeleted}");
                Console.WriteLine($"  - PersonaCategories: {personaCategoriesDeleted}");
                Console.WriteLine($"  - TopicTags: {topicTagsDeleted}");
                Console.WriteLine($"  - Personas: {personasDeleted}");
                Console.WriteLine($"  - Topics: {topicsDeleted}");
                Console.WriteLine($"  - Tags: {tagsDeleted}");
                Console.WriteLine($"  - Categories: {categoriesDeleted}");
                Console.WriteLine($"  - Users: {usersDeleted}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestDataCleanup] Warning: Error during test data cleanup: {ex.Message}");
            // Don't fail the test due to cleanup issues
        }
    }

    /// <summary>
    /// More aggressive cleanup for database reset scenarios.
    /// Use with caution - this will remove ANY data that looks like test data.
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    public static void AggressiveCleanup(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            // Delete all test data with even more permissive patterns
            var resetSql = @"
                -- Delete messages
                DELETE FROM messages WHERE content LIKE '%Test%' OR content LIKE '%test%';
                
                -- Delete chats
                DELETE FROM chats WHERE title LIKE '%Test%' OR title LIKE '%test%';
                
                -- Delete persona_categories
                DELETE FROM persona_categories WHERE persona_id IN (SELECT id FROM personas WHERE display_name LIKE '%Test%' OR display_name LIKE '%test%');
                
                -- Delete topic_tags
                DELETE FROM topic_tags WHERE topic_id IN (SELECT id FROM topics WHERE name LIKE '%Test%' OR name LIKE '%test%');
                
                -- Delete personas
                DELETE FROM personas WHERE display_name LIKE '%Test%' OR display_name LIKE '%test%' OR first_name LIKE '%Test%' OR first_name LIKE '%test%';
                
                -- Delete topics
                DELETE FROM topics WHERE name LIKE '%Test%' OR name LIKE '%test%';
                
                -- Delete tags
                DELETE FROM tags WHERE name LIKE '%Test%' OR name LIKE '%test%';
                
                -- Delete categories (excluding seed data)
                DELETE FROM categories WHERE name LIKE '%Test%' OR name LIKE '%test%';
                
                -- Delete users
                DELETE FROM users WHERE email LIKE '%@example.com' OR email LIKE '%@test.com%';";

            var rowsAffected = connection.Execute(resetSql);
            Console.WriteLine($"[TestDataCleanup] Aggressive cleanup removed {rowsAffected} test records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestDataCleanup] Error during aggressive cleanup: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Cleanup test training files that may have been created during tests.
    /// Since the training-data folder is only used for tests, we can safely delete all .txt files.
    /// </summary>
    public static void CleanupTestTrainingFiles()
    {
        try
        {
            var trainingPath = Path.Combine(Directory.GetCurrentDirectory(), "training-data", "personas");
            if (Directory.Exists(trainingPath))
            {
                // Get all .txt files in the training directory
                // Safe to delete all since this directory is only used for tests
                var testFiles = Directory.GetFiles(trainingPath, "*.txt");
                
                var filesDeleted = 0;
                foreach (var file in testFiles)
                {
                    try
                    {
                        File.Delete(file);
                        filesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TestDataCleanup] Warning: Could not delete file {Path.GetFileName(file)}: {ex.Message}");
                        // Ignore file deletion errors
                    }
                }
                
                if (filesDeleted > 0)
                {
                    Console.WriteLine($"[TestDataCleanup] Deleted {filesDeleted} training file(s) from {trainingPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestDataCleanup] Warning: Error during training file cleanup: {ex.Message}");
        }
    }
}

