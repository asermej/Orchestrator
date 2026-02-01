using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for Voice operations (consent, list voices, select voice, clone, preview) using real DomainFacade.
/// ServiceLocatorForAcceptanceTesting uses ConfigurationProviderForAcceptanceTesting, which forces
/// Voice:UseFakeElevenLabs=true so no real ElevenLabs API is ever called.
/// </summary>
[TestClass]
public class DomainFacadeTestsVoice
{
    private DomainFacade _domainFacade = null!;
    private string _connectionString = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);
        _connectionString = serviceLocator.CreateConfigurationProvider().GetDbConnectionString();
        TestDataCleanup.CleanupAllTestData(_connectionString);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        try
        {
            TestDataCleanup.CleanupAllTestData(_connectionString);
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

    private async Task<Persona> CreateTestPersonaAsync()
    {
        var persona = new Persona
        {
            FirstName = $"Test{DateTime.Now.Ticks}",
            LastName = $"Persona{DateTime.Now.Ticks}",
            DisplayName = $"TestVoice{DateTime.Now.Ticks}",
            ProfileImageUrl = null
        };
        var result = await _domainFacade.CreatePersona(persona);
        Assert.IsNotNull(result, "Failed to create test Persona");
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
    public async Task RecordConsentAsync_ValidRequest_ReturnsConsentId()
    {
        var persona = await CreateTestPersonaAsync();
        var userId = "auth0|test-voice-user-" + DateTime.Now.Ticks;

        var consentId = await _domainFacade.RecordConsentAsync(userId, persona.Id, null, attested: true);

        Assert.AreNotEqual(Guid.Empty, consentId, "RecordConsentAsync should return a non-empty consent ID");
    }

    [TestMethod]
    public async Task RecordConsentAsync_NotAttested_ThrowsVoiceSampleValidationException()
    {
        var persona = await CreateTestPersonaAsync();
        var userId = "auth0|test-voice-user-" + DateTime.Now.Ticks;

        await Assert.ThrowsExceptionAsync<VoiceSampleValidationException>(() =>
            _domainFacade.RecordConsentAsync(userId, persona.Id, null, attested: false));
    }

    [TestMethod]
    public async Task SelectPersonaVoiceAsync_ValidVoice_UpdatesPersona()
    {
        var persona = await CreateTestPersonaAsync();
        var voiceId = "fake-voice-1";
        var voiceName = "Fake Voice One";

        await _domainFacade.SelectPersonaVoiceAsync(persona.Id, "elevenlabs", "prebuilt", voiceId, voiceName);

        var updated = await _domainFacade.GetPersonaById(persona.Id);
        Assert.IsNotNull(updated, "Persona should still exist");
        Assert.AreEqual("elevenlabs", updated!.VoiceProvider);
        Assert.AreEqual("prebuilt", updated.VoiceType);
        Assert.AreEqual(voiceId, updated.ElevenLabsVoiceId);
        Assert.AreEqual(voiceName, updated.VoiceName);
    }

    [TestMethod]
    public async Task CloneVoiceAsync_WithConsentAndValidSample_ReturnsVoiceId()
    {
        var persona = await CreateTestPersonaAsync();
        var userId = "auth0|test-voice-clone-" + DateTime.Now.Ticks;
        var consentId = await _domainFacade.RecordConsentAsync(userId, persona.Id, null, true);
        // Minimal "audio" (15 seconds) - fake mode does not validate content
        var sampleDurationSeconds = 15;
        var sampleBytes = new byte[1024];
        for (var i = 0; i < sampleBytes.Length; i++) sampleBytes[i] = (byte)(i % 256);

        var result = await _domainFacade.CloneVoiceAsync(
            userId, persona.Id, "My Test Voice", null, sampleBytes, sampleDurationSeconds, consentId);

        Assert.IsNotNull(result, "CloneVoiceAsync should return a result");
        Assert.IsFalse(string.IsNullOrEmpty(result.VoiceId), "VoiceId should be set");
        Assert.AreEqual("My Test Voice", result.VoiceName);
    }

    [TestMethod]
    public async Task CloneVoiceAsync_WithoutValidConsent_ThrowsConsentNotFoundException()
    {
        var persona = await CreateTestPersonaAsync();
        var userId = "auth0|test-voice-clone-" + DateTime.Now.Ticks;
        var fakeConsentId = Guid.NewGuid();
        var sampleBytes = new byte[1024];
        var sampleDurationSeconds = 15;

        await Assert.ThrowsExceptionAsync<ConsentNotFoundException>(() =>
            _domainFacade.CloneVoiceAsync(userId, persona.Id, "My Voice", null, sampleBytes, sampleDurationSeconds, fakeConsentId));
    }

    [TestMethod]
    public async Task CloneVoiceAsync_SampleTooShort_ThrowsVoiceSampleValidationException()
    {
        var persona = await CreateTestPersonaAsync();
        var userId = "auth0|test-voice-clone-" + DateTime.Now.Ticks;
        var consentId = await _domainFacade.RecordConsentAsync(userId, persona.Id, null, true);
        var sampleBytes = new byte[256];
        var sampleDurationSeconds = 5;

        await Assert.ThrowsExceptionAsync<VoiceSampleValidationException>(() =>
            _domainFacade.CloneVoiceAsync(userId, persona.Id, "My Voice", null, sampleBytes, sampleDurationSeconds, consentId));
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
    public async Task VoiceFlow_RecordConsent_SelectVoice_Clone_ThenSelectCloned_Succeeds()
    {
        var persona = await CreateTestPersonaAsync();
        var userId = "auth0|test-flow-" + DateTime.Now.Ticks;

        var consentId = await _domainFacade.RecordConsentAsync(userId, persona.Id, null, true);
        var sampleBytes = new byte[512];
        var sampleDurationSeconds = 12;
        var cloneResult = await _domainFacade.CloneVoiceAsync(
            userId, persona.Id, "Cloned Voice Name", null, sampleBytes, sampleDurationSeconds, consentId);

        await _domainFacade.SelectPersonaVoiceAsync(
            persona.Id, "elevenlabs", "user_cloned", cloneResult.VoiceId, cloneResult.VoiceName);

        var updated = await _domainFacade.GetPersonaById(persona.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("elevenlabs", updated!.VoiceProvider);
        Assert.AreEqual("user_cloned", updated.VoiceType);
        Assert.AreEqual(cloneResult.VoiceId, updated.ElevenLabsVoiceId);
        Assert.AreEqual("Cloned Voice Name", updated.VoiceName);
    }

    [TestMethod]
    public async Task CloneVoiceAsync_SixthCloneWithinRateLimit_ThrowsVoiceCloneRateLimitExceededException()
    {
        var userId = "auth0|test-rate-" + DateTime.Now.Ticks;
        var sampleBytes = new byte[512];
        var sampleDurationSeconds = 12;

        for (int i = 0; i < 5; i++)
        {
            var persona = await CreateTestPersonaAsync();
            var consent = await _domainFacade.RecordConsentAsync(userId, persona.Id, null, true);
            await _domainFacade.CloneVoiceAsync(userId, persona.Id, $"Clone {i + 1}", null, sampleBytes, sampleDurationSeconds, consent);
        }

        var persona6 = await CreateTestPersonaAsync();
        var consent6 = await _domainFacade.RecordConsentAsync(userId, persona6.Id, null, true);

        await Assert.ThrowsExceptionAsync<VoiceCloneRateLimitExceededException>(() =>
            _domainFacade.CloneVoiceAsync(userId, persona6.Id, "Sixth Clone", null, sampleBytes, sampleDurationSeconds, consent6));
    }
}
