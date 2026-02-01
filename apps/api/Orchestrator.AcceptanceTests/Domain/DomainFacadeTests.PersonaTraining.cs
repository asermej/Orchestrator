using System;
using System.IO;
using System.Threading.Tasks;
using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;
using Npgsql;
using Dapper;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Persona Training operations using real DomainFacade
/// Tests the integration between personas and training data storage
/// </summary>
[TestClass]
public class DomainFacadeTestsPersonaTraining
{
    private DomainFacade _domainFacade;
    private string _connectionString;

    public DomainFacadeTestsPersonaTraining()
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
        
        // Clean up ALL test data before each test
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

    [TestMethod]
    public async Task UpdatePersonaTraining_ValidContent_SavesAndRetrievesCorrectly()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        var trainingContent = "This is test training content. The persona is wise and speaks in riddles.";

        // Act
        var updatedPersona = await _domainFacade.UpdatePersonaTraining(persona.Id, trainingContent);

        // Assert
        Assert.IsNotNull(updatedPersona);
        Assert.IsNotNull(updatedPersona.TrainingFilePath);
        Assert.IsTrue(updatedPersona.TrainingFilePath.StartsWith("file:///"));

        // Verify content can be retrieved
        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);
        Assert.AreEqual(trainingContent, retrievedContent);
    }

    [TestMethod]
    public async Task UpdatePersonaTraining_EmptyContent_ClearsTraining()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        // Add initial training
        await _domainFacade.UpdatePersonaTraining(persona.Id, "Initial training");

        // Act - Clear training with empty string
        var updatedPersona = await _domainFacade.UpdatePersonaTraining(persona.Id, "");

        // Assert
        Assert.IsTrue(string.IsNullOrEmpty(updatedPersona.TrainingFilePath));

        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);
        Assert.AreEqual(string.Empty, retrievedContent);
    }

    [TestMethod]
    public async Task UpdatePersonaTraining_UpdateExistingContent_ReplacesOldFile()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        var originalContent = "Original training content";
        var updatedContent = "Updated training content with new information";

        // Add initial training
        var firstUpdate = await _domainFacade.UpdatePersonaTraining(persona.Id, originalContent);
        var originalFilePath = firstUpdate.TrainingFilePath?.Substring(8); // Remove "file:///"

        // Act - Update training
        var secondUpdate = await _domainFacade.UpdatePersonaTraining(persona.Id, updatedContent);
        var newFilePath = secondUpdate.TrainingFilePath?.Substring(8);

        // Assert
        Assert.AreEqual(originalFilePath, newFilePath, "File path should remain the same");
        
        // Verify new content
        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);
        Assert.AreEqual(updatedContent, retrievedContent);
    }

    [TestMethod]
    public async Task UpdatePersonaTraining_MaxLength_SavesCorrectly()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        // Create content at max length (5000 chars)
        var maxLengthContent = new string('A', 5000);

        // Act
        var updatedPersona = await _domainFacade.UpdatePersonaTraining(persona.Id, maxLengthContent);

        // Assert
        Assert.IsNotNull(updatedPersona.TrainingFilePath);

        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);
        Assert.AreEqual(5000, retrievedContent.Length);
        Assert.AreEqual(maxLengthContent, retrievedContent);
    }

    [TestMethod]
    public async Task UpdatePersonaTraining_ExceedsMaxLength_ThrowsException()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        // Create content exceeding max length (5001 chars)
        var tooLongContent = new string('A', 5001);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TrainingStorageException>(() =>
            _domainFacade.UpdatePersonaTraining(persona.Id, tooLongContent));
    }

    [TestMethod]
    public async Task GetPersonaTraining_NonExistentPersona_ReturnsEmpty()
    {
        // Act
        var retrievedContent = await _domainFacade.GetPersonaTraining(Guid.NewGuid());

        // Assert
        Assert.AreEqual(string.Empty, retrievedContent);
    }

    [TestMethod]
    public async Task GetPersonaTraining_PersonaWithNoTraining_ReturnsEmpty()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        // Act - Get training without setting any
        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);

        // Assert
        Assert.AreEqual(string.Empty, retrievedContent);
    }

    [TestMethod]
    public async Task UpdatePersonaTraining_MultilineContent_PreservesFormatting()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        var multilineContent = @"Line 1: Introduction
Line 2: Personality traits
Line 3: Background
Line 4: Speaking style
Line 5: Knowledge areas";

        // Act
        await _domainFacade.UpdatePersonaTraining(persona.Id, multilineContent);
        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);

        // Assert
        Assert.AreEqual(multilineContent, retrievedContent);
        Assert.IsTrue(retrievedContent.Contains("\r\n") || retrievedContent.Contains("\n"));
    }

    [TestMethod]
    public async Task UpdatePersonaTraining_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        var specialCharContent = "Content with special chars: @#$%^&*()_+-=[]{}|;':\",./<>?`~émoji🎭";

        // Act
        await _domainFacade.UpdatePersonaTraining(persona.Id, specialCharContent);
        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);

        // Assert
        Assert.AreEqual(specialCharContent, retrievedContent);
    }

    [TestMethod]
    public async Task UpdatePersonaTraining_ConcurrentUpdates_LastWriteWins()
    {
        // Arrange
        var persona = new Persona { DisplayName = $"TestTraining{DateTime.Now.Ticks}" };
        persona = await _domainFacade.CreatePersona(persona);

        // Act - Simulate concurrent updates
        await _domainFacade.UpdatePersonaTraining(persona.Id, "First update");
        await _domainFacade.UpdatePersonaTraining(persona.Id, "Second update");
        await _domainFacade.UpdatePersonaTraining(persona.Id, "Third update");

        var retrievedContent = await _domainFacade.GetPersonaTraining(persona.Id);

        // Assert
        Assert.AreEqual("Third update", retrievedContent);
    }
}

