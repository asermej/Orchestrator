using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Agent operations using real DomainFacade.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsAgent
{
    private DomainFacade _domainFacade = null!;
    private Guid _testOrganizationId;

    [TestInitialize]
    public async Task TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);

        // Create a test organization for agent tests
        var testOrg = await _domainFacade.CreateOrganization(new Organization
        {
            Name = $"TestOrg_{Guid.NewGuid():N}"
        });
        _testOrganizationId = testOrg.Id;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            TestDataCleanup.CleanupAllTestData();
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
    /// Helper method to create test Agent
    /// </summary>
    private async Task<Agent> CreateTestAgentAsync(string suffix = "")
    {
        var agent = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = $"TestDisplay{suffix}_{Guid.NewGuid():N}",
            ProfileImageUrl = null
        };

        var result = await _domainFacade.CreateAgent(agent);
        Assert.IsNotNull(result, "Failed to create test Agent");
        return result;
    }

    [TestMethod]
    public async Task CreateAgent_ValidData_ReturnsCreatedAgent()
    {
        // Arrange
        var agent = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = $"JohnDoe_{Guid.NewGuid():N}",
            ProfileImageUrl = "https://example.com/image.jpg"
        };

        // Act
        var result = await _domainFacade.CreateAgent(agent);

        // Assert
        Assert.IsNotNull(result, "Create should return an Agent");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Agent should have a valid ID");
        Assert.AreEqual(agent.DisplayName, result.DisplayName, "DisplayName should match");
        Assert.AreEqual(agent.ProfileImageUrl, result.ProfileImageUrl, "ProfileImageUrl should match");
    }

    [TestMethod]
    public async Task CreateAgent_OnlyDisplayName_ReturnsCreatedAgent()
    {
        // Arrange - Only display name is required
        var agent = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = $"Yoda_{Guid.NewGuid():N}",
            ProfileImageUrl = null
        };

        // Act
        var result = await _domainFacade.CreateAgent(agent);

        // Assert
        Assert.IsNotNull(result, "Create should return an Agent");
        Assert.AreNotEqual(Guid.Empty, result.Id, "Agent should have a valid ID");
        Assert.AreEqual(agent.DisplayName, result.DisplayName, "DisplayName should match");
    }

    [TestMethod]
    public async Task CreateAgent_InvalidData_ThrowsValidationException()
    {
        // Arrange - Agent with empty required DisplayName
        var agent = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = "", // Required field empty
            ProfileImageUrl = null
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<AgentValidationException>(() => 
            _domainFacade.CreateAgent(agent), 
            "Should throw validation exception for empty DisplayName");
    }

    [TestMethod]
    public async Task CreateAgent_DuplicateDisplayName_ThrowsDuplicateException()
    {
        // Arrange - Create first agent
        var firstAgent = await CreateTestAgentAsync("First");
        
        // Create second agent with same display name
        var secondAgent = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = firstAgent.DisplayName, // Same display name
            ProfileImageUrl = null
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<AgentDuplicateDisplayNameException>(() => 
            _domainFacade.CreateAgent(secondAgent), 
            "Should throw duplicate display name exception");
    }

    [TestMethod]
    public async Task CreateAgent_InvalidUrlFormat_ThrowsValidationException()
    {
        // Arrange - Agent with invalid URL format
        var agent = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = $"TestDisplay_{Guid.NewGuid():N}",
            ProfileImageUrl = "not-a-valid-url" // Invalid URL format
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<AgentValidationException>(() => 
            _domainFacade.CreateAgent(agent), 
            "Should throw validation exception for invalid URL");
    }

    [TestMethod]
    public async Task GetAgentById_ExistingId_ReturnsAgent()
    {
        // Arrange - Create a test Agent
        var createdAgent = await CreateTestAgentAsync();

        // Act
        var result = await _domainFacade.GetAgentById(createdAgent.Id);

        // Assert
        Assert.IsNotNull(result, $"Should return Agent with ID: {createdAgent.Id}");
        Assert.AreEqual(createdAgent.Id, result.Id, "ID should match");
        Assert.AreEqual(createdAgent.DisplayName, result.DisplayName, "DisplayName should match");
    }

    [TestMethod]
    public async Task GetAgentById_NonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.GetAgentById(nonExistingId);

        // Assert
        Assert.IsNull(result, "Should return null for non-existing ID");
    }

    [TestMethod]
    public async Task SearchAgents_WithResults_ReturnsPaginatedList()
    {
        // Arrange - Create some test Agents with known display name pattern
        var uniquePrefix = $"SearchTest_{Guid.NewGuid():N}";
        
        var agent1 = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = $"{uniquePrefix}_1"
        };
        var created1 = await _domainFacade.CreateAgent(agent1);
        
        var agent2 = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = $"{uniquePrefix}_2"
        };
        var created2 = await _domainFacade.CreateAgent(agent2);

        // Act - Search by displayName pattern
        var result = await _domainFacade.SearchAgents(null, uniquePrefix, null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results");
        Assert.AreEqual(2, result.TotalCount, $"Should find exactly 2 Agents with prefix '{uniquePrefix}'");
        Assert.AreEqual(2, result.Items.Count(), "Should return 2 items");
    }

    [TestMethod]
    public async Task SearchAgents_NoResults_ReturnsEmptyList()
    {
        // Arrange - Use a unique search term that won't match anything
        var uniqueSearchTerm = $"NonExistent_{Guid.NewGuid():N}";
        
        // Act
        var result = await _domainFacade.SearchAgents(null, uniqueSearchTerm, null, null, 1, 10);

        // Assert
        Assert.IsNotNull(result, "Search should return results even if empty");
        Assert.AreEqual(0, result.TotalCount, "Should return 0 results for non-existent search term");
        Assert.IsFalse(result.Items.Any(), "Items should be empty");
    }

    [TestMethod]
    public async Task UpdateAgent_ValidData_UpdatesSuccessfully()
    {
        // Arrange - Create a test Agent
        var agent = await CreateTestAgentAsync();
        
        // Modify the Agent
        agent.DisplayName = $"UpdatedDisplay_{Guid.NewGuid():N}";
        agent.ProfileImageUrl = "https://example.com/updated.jpg";

        // Act
        var result = await _domainFacade.UpdateAgent(agent);

        // Assert
        Assert.IsNotNull(result, "Update should return the updated Agent");
        Assert.AreEqual(agent.DisplayName, result.DisplayName, "DisplayName should be updated");
        Assert.AreEqual(agent.ProfileImageUrl, result.ProfileImageUrl, "ProfileImageUrl should be updated");
    }

    [TestMethod]
    public async Task UpdateAgent_InvalidData_ThrowsValidationException()
    {
        // Arrange - Create a test Agent
        var agent = await CreateTestAgentAsync();
        
        // Set invalid data
        agent.DisplayName = ""; // Invalid empty value

        // Act & Assert
        await Assert.ThrowsExceptionAsync<AgentValidationException>(() => 
            _domainFacade.UpdateAgent(agent), 
            "Should throw validation exception for empty DisplayName");
    }

    [TestMethod]
    public async Task UpdateAgent_DuplicateDisplayName_ThrowsDuplicateException()
    {
        // Arrange - Create two test Agents
        var agent1 = await CreateTestAgentAsync("Agent1");
        var agent2 = await CreateTestAgentAsync("Agent2");
        
        // Try to update agent2 with agent1's display name
        agent2.DisplayName = agent1.DisplayName;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<AgentDuplicateDisplayNameException>(() => 
            _domainFacade.UpdateAgent(agent2), 
            "Should throw duplicate display name exception");
    }

    [TestMethod]
    public async Task DeleteAgent_ExistingId_DeletesSuccessfully()
    {
        // Arrange - Create a test Agent
        var agent = await CreateTestAgentAsync();
        var agentId = agent.Id;

        // Act
        var result = await _domainFacade.DeleteAgent(agentId);

        // Assert
        Assert.IsTrue(result, "Should return true when deleting existing Agent");
        var deletedAgent = await _domainFacade.GetAgentById(agentId);
        Assert.IsNull(deletedAgent, "Should not find deleted Agent");
    }

    [TestMethod]
    public async Task DeleteAgent_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _domainFacade.DeleteAgent(nonExistingId);

        // Assert
        Assert.IsFalse(result, "Should return false for non-existing ID");
    }

    [TestMethod]
    public async Task AgentLifecycleTest_CreateGetUpdateSearchDelete_WorksCorrectly()
    {
        // Create
        var uniquePrefix = $"Lifecycle_{Guid.NewGuid():N}";
        var agent = new Agent
        {
            OrganizationId = _testOrganizationId,
            DisplayName = uniquePrefix
        };
        var created = await _domainFacade.CreateAgent(agent);
        Assert.IsNotNull(created, "Agent should be created");
        var createdId = created.Id;
        
        // Get
        var retrievedAgent = await _domainFacade.GetAgentById(createdId);
        Assert.IsNotNull(retrievedAgent, "Should retrieve created Agent");
        Assert.AreEqual(createdId, retrievedAgent.Id, "Retrieved ID should match");
        
        // Update
        var updatedDisplayName = $"Updated_{Guid.NewGuid():N}";
        retrievedAgent.DisplayName = updatedDisplayName;
        
        var updatedAgent = await _domainFacade.UpdateAgent(retrievedAgent);
        Assert.IsNotNull(updatedAgent, "Should update Agent");
        Assert.AreEqual(updatedDisplayName, updatedAgent.DisplayName, "DisplayName should be updated");
        
        // Search - Search by updated displayName
        var searchResult = await _domainFacade.SearchAgents(null, updatedDisplayName, null, null, 1, 10);
        Assert.IsNotNull(searchResult, "Search should return results");
        Assert.AreEqual(1, searchResult.TotalCount, "Should find exactly 1 updated Agent");
        
        // Delete
        var deleteResult = await _domainFacade.DeleteAgent(createdId);
        Assert.IsTrue(deleteResult, "Should successfully delete Agent");
        
        // Verify deletion
        var deletedAgent = await _domainFacade.GetAgentById(createdId);
        Assert.IsNull(deletedAgent, "Should not find deleted Agent");
    }
}
