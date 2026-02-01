using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Persona operations using real DomainFacade and real DataFacade with data cleanup
/// 
/// TEST APPROACH:
/// - Uses real DomainFacade and DataFacade instances for acceptance tests
/// - Tests the actual integration between layers
/// - No external mocking frameworks used
/// - ServiceLocatorForAcceptanceTesting provides real implementations
/// - Tests clean up their own data to ensure complete independence
/// 
/// ENHANCED CLEANUP APPROACH:
/// - Database-level cleanup before AND after each test for complete isolation
/// - Identifies test data by patterns (display name patterns, specific names)
/// - Robust error handling that doesn't break tests
/// - Ensures 100% test reliability and independence
/// </summary>
[TestClass]
public class DomainFacadeTestsPersona
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsPersona()
    {
        _domainFacade = null!;
        _connectionString = null!;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        _connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        
        // Clean up ALL test data before each test to ensure complete isolation
        TestDataCleanup.CleanupAllTestData(_connectionString);
        TestDataCleanup.CleanupTestTrainingFiles();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            // Clean up any remaining test data after the test
            TestDataCleanup.CleanupAllTestData(_connectionString);
            TestDataCleanup.CleanupTestTrainingFiles();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error during test cleanup: {ex.Message}");
        }
        finally
        {
            _domainFacade?.Dispose();
        }
    }

    /// <summary>
    /// Helper method to create test Persona with unique data and track it for cleanup
    /// </summary>
    private async Task<Persona> CreateTestPersonaAsync(string suffix = "")
    {
        var persona = new Persona
        {
            FirstName = $"Test{suffix}{DateTime.Now.Ticks}",
            LastName = $"Persona{suffix}{DateTime.Now.Ticks}",
            DisplayName = $"TestDisplay{suffix}{DateTime.Now.Ticks}",
            ProfileImageUrl = null
        };

        var result = await _domainFacade.CreatePersona(persona);
        Assert.IsNotNull(result, "Failed to create test Persona");
        return result;
    }

    /// <summary>
    /// Simple helper method to create test Persona with basic data
    /// </summary>
    private async Task<Persona> CreateTestPersona()
    {
        var persona = new Persona
        {
            FirstName = $"Test{DateTime.Now.Ticks}",
            LastName = $"Persona{DateTime.Now.Ticks}",
            DisplayName = $"TestDisplay{DateTime.Now.Ticks}",
            ProfileImageUrl = null
        };

        var result = await _domainFacade.CreatePersona(persona);
        return result;
    }

    [TestMethod]
    public async Task CreatePersona_ValidData_ReturnsCreatedPersona()
    {
        // Arrange
        var persona = new Persona
        {
            FirstName = $"firstname{DateTime.Now.Ticks}",
            LastName = $"lastname{DateTime.Now.Ticks}",
            DisplayName = $"displayname{DateTime.Now.Ticks}",
            ProfileImageUrl = "https://example.com/image.jpg"
        };

        // Act
        var result = await _domainFacade.CreatePersona(persona);

        // Assert
        Assert.IsNotNull(result, "Create should return a Persona");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Persona should have a valid ID");
        Assert.AreEqual(persona.FirstName, result.FirstName);
        Assert.AreEqual(persona.LastName, result.LastName);
        Assert.AreEqual(persona.DisplayName, result.DisplayName);
        Assert.AreEqual(persona.ProfileImageUrl, result.ProfileImageUrl);
        
        Console.WriteLine($"Persona created with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreatePersona_OnlyDisplayName_ReturnsCreatedPersona()
    {
        // Arrange - Like "Yoda", only needs display name
        var persona = new Persona
        {
            FirstName = null,
            LastName = null,
            DisplayName = $"Yoda{DateTime.Now.Ticks}",
            ProfileImageUrl = null
        };

        // Act
        var result = await _domainFacade.CreatePersona(persona);

        // Assert
        Assert.IsNotNull(result, "Create should return a Persona");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Persona should have a valid ID");
        Assert.IsNull(result.FirstName);
        Assert.IsNull(result.LastName);
        Assert.AreEqual(persona.DisplayName, result.DisplayName);
        
        Console.WriteLine($"Persona 'Yoda' created with ID: {result.Id}");
    }

    [TestMethod]
    public async Task CreatePersona_InvalidData_ThrowsValidationException()
    {
        // Arrange - Persona with empty required DisplayName
        var persona = new Persona
        {
            FirstName = "Test",
            LastName = "Test",
            DisplayName = "", // Required field empty
            ProfileImageUrl = null
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaValidationException>(() => 
            _domainFacade.CreatePersona(persona), "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task CreatePersona_DuplicateDisplayName_ThrowsDuplicateException()
    {
        // Arrange - Create first persona
        var firstPersona = await CreateTestPersonaAsync("First");
        
        // Create second persona with same display name
        var secondPersona = new Persona
        {
            FirstName = "Different",
            LastName = "Person",
            DisplayName = firstPersona.DisplayName, // Same display name
            ProfileImageUrl = null
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaDuplicateDisplayNameException>(() => 
            _domainFacade.CreatePersona(secondPersona), "Should throw duplicate display name exception");
    }

    [TestMethod]
    public async Task CreatePersona_InvalidUrlFormat_ThrowsValidationException()
    {
        // Arrange - Persona with invalid URL format
        var persona = new Persona
        {
            FirstName = "Test",
            LastName = "Test",
            DisplayName = $"TestDisplay{DateTime.Now.Ticks}",
            ProfileImageUrl = "not-a-valid-url" // Invalid URL format
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaValidationException>(() => 
            _domainFacade.CreatePersona(persona), "Should throw validation exception for invalid URL");
    }

    [TestMethod]
    public async Task GetPersonaById_ExistingId_ReturnsPersona()
    {
        // Arrange - Create a test Persona
        var createdPersona = await CreateTestPersonaAsync();

        // Act
        var result = await _domainFacade.GetPersonaById(createdPersona.Id);

        // Assert
        Assert.IsNotNull(result, $"Should return Persona with ID: {createdPersona.Id}");
        Assert.AreEqual(createdPersona.Id, result.Id);
        Assert.AreEqual(createdPersona.FirstName, result.FirstName);
        Assert.AreEqual(createdPersona.LastName, result.LastName);
        Assert.AreEqual(createdPersona.DisplayName, result.DisplayName);
        Assert.AreEqual(createdPersona.ProfileImageUrl, result.ProfileImageUrl);
    }

    [TestMethod]
    public async Task GetPersonaById_NonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetPersonaById(nonExistingId);

        // Assert
        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task SearchPersonas_WithResults_ReturnsPaginatedList()
    {
        // Arrange - Create some test Personas
        var persona1 = await CreateTestPersonaAsync("Test1");
        var persona2 = await CreateTestPersonaAsync("Test2");

        // Act - Search by displayName pattern
        var result = await _domainFacade.SearchPersonas(null, null, "TestDisplay", null, null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.IsTrue(result.TotalCount >= 2, $"Should find at least 2 Personas, found {result.TotalCount}");
        Assert.IsTrue(result.Items.Count() >= 2, $"Should return at least 2 items, returned {result.Items.Count()}");
        
        Console.WriteLine($"Search returned {result.TotalCount} total Personas");
    }

    [TestMethod]
    public async Task SearchPersonas_NoResults_ReturnsEmptyList()
    {
        // Act
        var result = await _domainFacade.SearchPersonas("NonExistentSearchTerm", "NonExistentSearchTerm", "NonExistentSearchTerm", null, null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results for non-existent search term");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task UpdatePersona_ValidData_UpdatesSuccessfully()
    {
        // Arrange - Create a test Persona
        var persona = await CreateTestPersonaAsync();
        
        // Modify the Persona
        persona.FirstName = $"Updated{DateTime.Now.Ticks}";
        persona.LastName = $"UpdatedLast{DateTime.Now.Ticks}";
        persona.DisplayName = $"UpdatedDisplay{DateTime.Now.Ticks}";
        persona.ProfileImageUrl = "https://example.com/updated.jpg";

        // Act
        var result = await _domainFacade.UpdatePersona(persona);

        // Assert
        Assert.IsNotNull(result, "Update should return the updated Persona");
        Assert.AreEqual(persona.FirstName, result.FirstName);
        Assert.AreEqual(persona.LastName, result.LastName);
        Assert.AreEqual(persona.DisplayName, result.DisplayName);
        Assert.AreEqual(persona.ProfileImageUrl, result.ProfileImageUrl);
        
        Console.WriteLine($"Persona updated successfully");
    }

    [TestMethod]
    public async Task UpdatePersona_InvalidData_ThrowsValidationException()
    {
        // Arrange - Create a test Persona
        var persona = await CreateTestPersonaAsync();
        
        // Set invalid data
        persona.DisplayName = ""; // Invalid empty value

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaValidationException>(() => 
            _domainFacade.UpdatePersona(persona), 
            "Should throw validation exception for invalid data");
    }

    [TestMethod]
    public async Task UpdatePersona_DuplicateDisplayName_ThrowsDuplicateException()
    {
        // Arrange - Create two test Personas
        var persona1 = await CreateTestPersonaAsync("Persona1");
        var persona2 = await CreateTestPersonaAsync("Persona2");
        
        // Try to update persona2 with persona1's display name
        persona2.DisplayName = persona1.DisplayName;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PersonaDuplicateDisplayNameException>(() => 
            _domainFacade.UpdatePersona(persona2), 
            "Should throw duplicate display name exception");
    }

    [TestMethod]
    public async Task DeletePersona_ExistingId_DeletesSuccessfully()
    {
        // Arrange - Create a test Persona
        var persona = await CreateTestPersonaAsync();

        // Act
        var result = await _domainFacade.DeletePersona(persona.Id);

        // Assert
        Assert.IsTrue(result, "Should return true when deleting existing Persona");
        var deletedPersona = await _domainFacade.GetPersonaById(persona.Id);
        Assert.IsNull(deletedPersona, "Should not find deleted Persona");
        
        Console.WriteLine($"Persona deleted successfully");
    }

    [TestMethod]
    public async Task DeletePersona_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeletePersona(nonExistingId);

        // Assert
        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task PersonaLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        // Create
        var persona = await CreateTestPersonaAsync("Lifecycle");
        Assert.IsNotNull(persona, "Persona should be created");
        var createdId = persona.Id;
        
        // Get
        var retrievedPersona = await _domainFacade.GetPersonaById(createdId);
        Assert.IsNotNull(retrievedPersona, "Should retrieve created Persona");
        Assert.AreEqual(createdId, retrievedPersona.Id);
        
        // Update
        retrievedPersona.FirstName = $"Updated{DateTime.Now.Ticks}";
        retrievedPersona.LastName = $"UpdatedLast{DateTime.Now.Ticks}";
        retrievedPersona.DisplayName = $"UpdatedDisplay{DateTime.Now.Ticks}";
        retrievedPersona.ProfileImageUrl = "https://example.com/updated.jpg";
        
        var updatedPersona = await _domainFacade.UpdatePersona(retrievedPersona);
        Assert.IsNotNull(updatedPersona, "Should update Persona");
        
        // Search - Search by displayName pattern
        var searchResult = await _domainFacade.SearchPersonas(null, null, "UpdatedDisplay", null, null, null, 1, 10);
        Assert.IsNotNull(searchResult, "Search should return results");
        Assert.IsTrue(searchResult.TotalCount > 0, "Should find updated Persona");
        
        // Delete
        var deleteResult = await _domainFacade.DeletePersona(createdId);
        Assert.IsTrue(deleteResult, "Should successfully delete Persona");
        
        // Verify deletion
        var deletedPersona = await _domainFacade.GetPersonaById(createdId);
        Assert.IsNull(deletedPersona, "Should not find deleted Persona");
        
        Console.WriteLine("Persona lifecycle test completed successfully");
    }

    /// <summary>
    /// Optional: Call this method to completely reset the database after all tests
    /// This can be useful for integration test scenarios or when you want a completely clean slate
    /// </summary>
    [TestMethod]
    [TestCategory("DatabaseReset")]
    public void ResetDatabase_RemoveAllTestData()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            // More aggressive cleanup that removes ALL potential test data
            var resetSql = @"
                DELETE FROM personas 
                WHERE display_name LIKE '%Test%'
                   OR display_name LIKE '%test%'
                   OR display_name LIKE '%Search%'
                   OR display_name LIKE '%Update%'
                   OR display_name LIKE '%Delete%'
                   OR display_name LIKE '%Lifecycle%'
                   OR display_name LIKE '%Yoda%'
                   OR first_name LIKE '%Test%'
                   OR first_name LIKE '%test%'
                   OR last_name LIKE '%Test%'
                   OR last_name LIKE '%test%';";

            var rowsAffected = connection.Execute(resetSql);
            
            Console.WriteLine($"Database reset: Removed {rowsAffected} test Persona records");
            Assert.IsTrue(true, $"Successfully reset database - removed {rowsAffected} records");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to reset database: {ex.Message}");
        }
    }

    /// <summary>
    /// Optional: Call this to verify the database is clean after reset
    /// </summary>
    [TestMethod]
    [TestCategory("DatabaseReset")]
    public async Task VerifyDatabaseClean_NoTestDataRemains()
    {
        // Search for any remaining test data
        var result = await _domainFacade.SearchPersonas(null, null, "Test", null, null, null, 1, 100);
        
        Assert.IsNotNull(result, "Search should return results object");
        Assert.AreEqual(0, result.TotalCount, $"Database should be clean but found {result.TotalCount} test records");
        Assert.IsFalse(result.Items.Any(), "No test items should remain in database");
        
        Console.WriteLine("✅ Database verification passed - no test data found");
    }
}

