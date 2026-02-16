using Orchestrator.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orchestrator.AcceptanceTests.Domain;
using Orchestrator.AcceptanceTests.TestUtilities;

namespace Orchestrator.AcceptanceTests.Domain;

/// <summary>
/// Tests for InterviewInvite and CandidateSession operations using real DomainFacade.
/// Depends on Group, Agent, Job, Applicant, Interview.
/// Cleanup: centralized SQL cleanup in TestInitialize/TestCleanup via TestDataCleanup.
/// </summary>
[TestClass]
public class DomainFacadeTestsInterviewInvite
{
    private DomainFacade _domainFacade = null!;
    private Guid _testGroupId;
    private Guid _testAgentId;
    private Guid _testJobId;
    private Guid _testApplicantId;
    private Guid _testInterviewId;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    [TestInitialize]
    public async Task TestInitialize()
    {
        TestDataCleanup.CleanupAllTestData();
        var serviceLocator = new ServiceLocatorForAcceptanceTesting();
        _domainFacade = new DomainFacade(serviceLocator);

        // Create prerequisite entities
        var group = await _domainFacade.CreateGroup(new Group
        {
            Name = Truncate($"TestOrg_Invite_{Guid.NewGuid():N}", 50),
            ApiKey = "",
            IsActive = true
        });
        _testGroupId = group.Id;

        var agent = await _domainFacade.CreateAgent(new Agent
        {
            GroupId = _testGroupId,
            DisplayName = Truncate($"TestAgent_Invite_{Guid.NewGuid():N}", 80),
            ProfileImageUrl = null
        });
        _testAgentId = agent.Id;

        var job = await _domainFacade.CreateJob(new Job
        {
            GroupId = _testGroupId,
            ExternalJobId = Truncate($"ext_job_{Guid.NewGuid():N}", 50),
            Title = Truncate($"TestJob_Invite_{Guid.NewGuid():N}", 80),
            Status = "active"
        });
        _testJobId = job.Id;

        var applicant = await _domainFacade.CreateApplicant(new Applicant
        {
            GroupId = _testGroupId,
            ExternalApplicantId = Truncate($"ext_app_{Guid.NewGuid():N}", 50),
            FirstName = "Invite",
            LastName = "Test",
            Email = Truncate($"invite_test_{Guid.NewGuid():N}", 20) + "@example.com"
        });
        _testApplicantId = applicant.Id;

        var interview = await _domainFacade.CreateInterview(new Interview
        {
            JobId = _testJobId,
            ApplicantId = _testApplicantId,
            AgentId = _testAgentId,
            Token = Truncate($"token_{Guid.NewGuid():N}", 50),
            Status = InterviewStatus.Pending,
            InterviewType = InterviewType.Voice
        });
        _testInterviewId = interview.Id;
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

    // ─── Invite Creation Tests ───────────────────────────────────────

    [TestMethod]
    public async Task CreateInterviewInvite_ValidData_ReturnsInviteWithShortCode()
    {
        // Act
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId, maxUses: 3, expiryDays: 7);

        // Assert
        Assert.IsNotNull(invite, "Invite should be created");
        Assert.AreNotEqual(Guid.Empty, invite.Id);
        Assert.AreEqual(_testInterviewId, invite.InterviewId);
        Assert.AreEqual(_testGroupId, invite.GroupId);
        Assert.IsFalse(string.IsNullOrEmpty(invite.ShortCode), "ShortCode should not be empty");
        Assert.AreEqual(12, invite.ShortCode.Length, "ShortCode should be 12 characters");
        Assert.AreEqual(InviteStatus.Active, invite.Status);
        Assert.AreEqual(3, invite.MaxUses);
        Assert.AreEqual(0, invite.UseCount);
        Assert.IsTrue(invite.ExpiresAt > DateTime.UtcNow, "ExpiresAt should be in the future");
    }

    [TestMethod]
    public async Task CreateInterviewInvite_InvalidInterviewId_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InviteValidationException>(() =>
            _domainFacade.CreateInterviewInvite(Guid.Empty, _testGroupId),
            "Should throw validation exception for empty interview ID");
    }

    // ─── Invite Lookup Tests ─────────────────────────────────────────

    [TestMethod]
    public async Task GetInterviewInviteById_ExistingId_ReturnsInvite()
    {
        // Arrange
        var created = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);

        // Act
        var result = await _domainFacade.GetInterviewInviteById(created.Id);

        // Assert
        Assert.IsNotNull(result, $"Should find invite with ID: {created.Id}");
        Assert.AreEqual(created.ShortCode, result.ShortCode);
    }

    [TestMethod]
    public async Task GetInterviewInviteByShortCode_ExistingCode_ReturnsInvite()
    {
        // Arrange
        var created = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);

        // Act
        var result = await _domainFacade.GetInterviewInviteByShortCode(created.ShortCode);

        // Assert
        Assert.IsNotNull(result, $"Should find invite with short code: {created.ShortCode}");
        Assert.AreEqual(created.Id, result.Id);
    }

    [TestMethod]
    public async Task GetInterviewInviteByShortCode_NonExistingCode_ReturnsNull()
    {
        // Act
        var result = await _domainFacade.GetInterviewInviteByShortCode("nonexistent12");

        // Assert
        Assert.IsNull(result, "Should return null for non-existing short code");
    }

    [TestMethod]
    public async Task GetInterviewInviteByInterviewId_ExistingInterview_ReturnsInvite()
    {
        // Arrange
        var created = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);

        // Act
        var result = await _domainFacade.GetInterviewInviteByInterviewId(_testInterviewId);

        // Assert
        Assert.IsNotNull(result, "Should find invite for interview");
        Assert.AreEqual(created.Id, result.Id);
    }

    // ─── Invite Revocation Tests ─────────────────────────────────────

    [TestMethod]
    public async Task RevokeInterviewInvite_ActiveInvite_SetsStatusToRevoked()
    {
        // Arrange
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);

        // Act
        var revoked = await _domainFacade.RevokeInterviewInvite(invite.Id, "test-admin");

        // Assert
        Assert.IsNotNull(revoked);
        Assert.AreEqual(InviteStatus.Revoked, revoked.Status);
        Assert.IsNotNull(revoked.RevokedAt, "RevokedAt should be set");
        Assert.AreEqual("test-admin", revoked.RevokedBy);
    }

    [TestMethod]
    public async Task RevokeInterviewInvite_NonExistingId_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InviteNotFoundException>(() =>
            _domainFacade.RevokeInterviewInvite(Guid.NewGuid()),
            "Should throw not found for non-existing invite");
    }

    // ─── Invite Consume Tests ────────────────────────────────────────

    [TestMethod]
    public async Task ConsumeInterviewInvite_ActiveInvite_SetsStatusToConsumed()
    {
        // Arrange
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);

        // Act
        var consumed = await _domainFacade.ConsumeInterviewInvite(invite.Id);

        // Assert
        Assert.IsNotNull(consumed);
        Assert.AreEqual(InviteStatus.Consumed, consumed.Status);
    }

    // ─── Invite Delete Tests ─────────────────────────────────────────

    [TestMethod]
    public async Task DeleteInterviewInvite_ExistingId_DeletesSuccessfully()
    {
        // Arrange
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);

        // Act
        var result = await _domainFacade.DeleteInterviewInvite(invite.Id);

        // Assert
        Assert.IsTrue(result, "Delete should succeed");
        Assert.IsNull(await _domainFacade.GetInterviewInviteById(invite.Id),
            "Invite should not be found after deletion");
    }

    // ─── Candidate Session / Redeem Invite Tests ─────────────────────

    [TestMethod]
    public async Task RedeemInterviewInvite_ValidShortCode_ReturnsSessionWithJwt()
    {
        // Arrange
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);

        // Act
        var result = await _domainFacade.RedeemInterviewInvite(
            invite.ShortCode, "127.0.0.1", "TestAgent/1.0");

        // Assert
        Assert.IsNotNull(result, "Redeem should return a result");
        Assert.IsFalse(string.IsNullOrEmpty(result.Token), "Token should not be empty");
        Assert.IsNotNull(result.Interview, "Interview should be included");
        Assert.AreEqual(_testInterviewId, result.Interview.Id);
        Assert.IsNotNull(result.Agent, "Agent should be included");
        Assert.IsNotNull(result.Job, "Job should be included");
        Assert.IsNotNull(result.Session, "Session should be included");
        Assert.IsTrue(result.Session.IsActive, "Session should be active");

        // Verify use count was incremented
        var updatedInvite = await _domainFacade.GetInterviewInviteById(invite.Id);
        Assert.AreEqual(1, updatedInvite!.UseCount, "Use count should be incremented to 1");
    }

    [TestMethod]
    public async Task RedeemInterviewInvite_NonExistingCode_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InviteNotFoundException>(() =>
            _domainFacade.RedeemInterviewInvite("nonexistent12", null, null),
            "Should throw not found for non-existing short code");
    }

    [TestMethod]
    public async Task RedeemInterviewInvite_RevokedInvite_ThrowsNotActiveException()
    {
        // Arrange
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);
        await _domainFacade.RevokeInterviewInvite(invite.Id);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InviteNotActiveException>(() =>
            _domainFacade.RedeemInterviewInvite(invite.ShortCode, null, null),
            "Should throw not active for revoked invite");
    }

    [TestMethod]
    public async Task RedeemInterviewInvite_MaxUsesExceeded_ThrowsMaxUsesException()
    {
        // Arrange - create invite with max 1 use
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId, maxUses: 1);

        // First redemption should succeed
        await _domainFacade.RedeemInterviewInvite(invite.ShortCode, null, null);

        // Act & Assert - second redemption should fail
        await Assert.ThrowsExceptionAsync<InviteMaxUsesExceededException>(() =>
            _domainFacade.RedeemInterviewInvite(invite.ShortCode, null, null),
            "Should throw max uses exceeded after 1 use");
    }

    [TestMethod]
    public async Task RedeemInterviewInvite_MultipleRedemptions_DeactivatesPreviousSessions()
    {
        // Arrange - create invite with max 3 uses
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId, maxUses: 3);

        // Act - redeem twice
        var result1 = await _domainFacade.RedeemInterviewInvite(invite.ShortCode, "1.1.1.1", null);
        var result2 = await _domainFacade.RedeemInterviewInvite(invite.ShortCode, "2.2.2.2", null);

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.AreNotEqual(result1.Token, result2.Token, "Each redemption should produce a different token");
        Assert.IsTrue(result2.Session.IsActive, "Latest session should be active");

        // Verify use count
        var updatedInvite = await _domainFacade.GetInterviewInviteById(invite.Id);
        Assert.AreEqual(2, updatedInvite!.UseCount, "Use count should be 2");
    }

    // ─── Candidate Session Validation Tests ──────────────────────────

    [TestMethod]
    public async Task ValidateCandidateSession_ValidJti_ReturnsSession()
    {
        // Arrange
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);
        var redeemResult = await _domainFacade.RedeemInterviewInvite(invite.ShortCode, null, null);

        // Act
        var session = await _domainFacade.ValidateCandidateSession(redeemResult.Session.Jti);

        // Assert
        Assert.IsNotNull(session, "Should return session for valid jti");
        Assert.AreEqual(redeemResult.Session.Id, session.Id);
        Assert.IsTrue(session.IsActive, "Session should be active");
    }

    [TestMethod]
    public async Task ValidateCandidateSession_NonExistingJti_ThrowsNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<CandidateSessionNotFoundException>(() =>
            _domainFacade.ValidateCandidateSession("nonexistent_jti_value"),
            "Should throw not found for non-existing jti");
    }

    // ─── Lifecycle Test ──────────────────────────────────────────────

    [TestMethod]
    public async Task InviteLifecycleTest_CreateRedeemCompleteConsume_WorksCorrectly()
    {
        // 1. Create invite
        var invite = await _domainFacade.CreateInterviewInvite(
            _testInterviewId, _testGroupId);
        Assert.AreEqual(InviteStatus.Active, invite.Status);

        // 2. Redeem invite (candidate clicks link)
        var sessionResult = await _domainFacade.RedeemInterviewInvite(
            invite.ShortCode, "10.0.0.1", "Chrome/120");
        Assert.IsNotNull(sessionResult.Token);
        Assert.AreEqual(_testInterviewId, sessionResult.Interview.Id);

        // 3. Validate session (middleware would do this)
        var session = await _domainFacade.ValidateCandidateSession(sessionResult.Session.Jti);
        Assert.IsTrue(session.IsActive);

        // 4. Start interview
        var started = await _domainFacade.StartInterview(_testInterviewId);
        Assert.AreEqual(InterviewStatus.InProgress, started.Status);

        // 5. Complete interview
        var completed = await _domainFacade.CompleteInterview(_testInterviewId);
        Assert.AreEqual(InterviewStatus.Completed, completed.Status);

        // 6. Consume invite
        var consumed = await _domainFacade.ConsumeInterviewInvite(invite.Id);
        Assert.AreEqual(InviteStatus.Consumed, consumed.Status);

        // 7. Verify consumed invite can't be redeemed
        await Assert.ThrowsExceptionAsync<InviteNotActiveException>(() =>
            _domainFacade.RedeemInterviewInvite(invite.ShortCode, null, null),
            "Consumed invite should not be redeemable");
    }
}
