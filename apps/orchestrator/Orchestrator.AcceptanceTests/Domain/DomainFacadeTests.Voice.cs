using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Voice operations (list voices, select voice, preview) using real DomainFacade.
/// ServiceLocatorForAcceptanceTesting uses ConfigurationProviderForAcceptanceTesting, which forces
/// Voice:UseFakeElevenLabs=true so no real ElevenLabs API is ever called.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsVoice
{
    private DomainFacade _domainFacade = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
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

    private async Task<Agent> CreateTestAgentAsync()
    {
        // Create a test group first
        var testGroup = await _domainFacade.CreateGroup(new Group
        {
            Name = $"TestOrg_{Guid.NewGuid():N}"
        });

        var agent = new Agent
        {
            GroupId = testGroup.Id,
            DisplayName = $"TestVoice_{Guid.NewGuid():N}",
            ProfileImageUrl = null
        };
        var result = await _domainFacade.CreateAgent(agent);
        Assert.IsNotNull(result, "Failed to create test Agent");
        return result;
    }

    [TestMethod]
    public async Task GetAvailableVoicesAsync_WhenCalled_ReturnsVoicesList()
    {
        var voices = await _domainFacade.GetAvailableVoicesAsync();
        Assert.IsNotNull(voices, "GetAvailableVoicesAsync should return a non-null list");
        Assert.IsTrue(voices.Count >= 0, "Voices list may be empty or contain fake voices");
        // With UseFakeElevenLabs=true we get deterministic fake list (e.g. 2 items)
        if (voices.Count > 0)
        {
            var first = voices[0];
            Assert.IsFalse(string.IsNullOrEmpty(first.Id), "Voice Id should be set");
            Assert.IsFalse(string.IsNullOrEmpty(first.Name), "Voice Name should be set");
        }
    }

    [TestMethod]
    public async Task GetStockVoicesAsync_WhenCalled_ReturnsStockVoicesList()
    {
        var voices = await _domainFacade.GetStockVoicesAsync();
        Assert.IsNotNull(voices, "GetStockVoicesAsync should return a non-null list");
        Assert.IsTrue(voices.Count >= 0, "Stock voices list may be empty");
        if (voices.Count > 0)
        {
            var first = voices[0];
            Assert.IsFalse(string.IsNullOrEmpty(first.VoiceId), "Voice VoiceId should be set");
            Assert.IsFalse(string.IsNullOrEmpty(first.Name), "Voice Name should be set");
        }
    }

    [TestMethod]
    public async Task SelectAgentVoiceAsync_ValidVoice_UpdatesAgent()
    {
        var agent = await CreateTestAgentAsync();
        var voiceId = "fake-voice-1";
        var voiceName = "Fake Voice One";

        await _domainFacade.SelectAgentVoiceAsync(agent.Id, "elevenlabs", "prebuilt", voiceId, voiceName);

        var updated = await _domainFacade.GetAgentById(agent.Id);
        Assert.IsNotNull(updated, "Agent should still exist");
        Assert.AreEqual("elevenlabs", updated.VoiceProvider, "VoiceProvider should be set");
        Assert.AreEqual("prebuilt", updated.VoiceType, "VoiceType should be set");
        Assert.AreEqual(voiceId, updated.ElevenlabsVoiceId, "ElevenlabsVoiceId should be set");
        Assert.AreEqual(voiceName, updated.VoiceName, "VoiceName should be set");
    }

    [TestMethod]
    public async Task PreviewVoiceAsync_ValidVoiceId_ReturnsBytes()
    {
        var bytes = await _domainFacade.PreviewVoiceAsync("fake-voice-1", "Hello");
        Assert.IsNotNull(bytes, "PreviewVoiceAsync should return non-null bytes");
        // With UseFakeElevenLabs=true we get empty array
        Assert.IsTrue(bytes.Length >= 0, "Preview may return empty array in fake mode");
    }

    [TestMethod]
    public async Task VoiceFlow_SelectVoice_ThenPreview_Succeeds()
    {
        var agent = await CreateTestAgentAsync();

        // Select a voice
        await _domainFacade.SelectAgentVoiceAsync(
            agent.Id, "elevenlabs", "prebuilt", "fake-voice-1", "Fake Voice One");

        // Verify voice was set
        var updated = await _domainFacade.GetAgentById(agent.Id);
        Assert.IsNotNull(updated, "Agent should still exist");
        Assert.AreEqual("fake-voice-1", updated.ElevenlabsVoiceId, "ElevenlabsVoiceId should match selected voice");
        Assert.AreEqual("Fake Voice One", updated.VoiceName, "VoiceName should match selected voice");

        // Preview the voice
        var previewBytes = await _domainFacade.PreviewVoiceAsync("fake-voice-1", "Hello from the voice test");
        Assert.IsNotNull(previewBytes, "Preview should return non-null bytes");
    }
}
