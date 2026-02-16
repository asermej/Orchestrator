using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orchestrator.Api.Common;

namespace Orchestrator.Api.ResourcesModels;

/// <summary>
/// Represents a User in API responses
/// </summary>
public class UserResource
{
    /// <summary>
    /// The unique identifier of the User
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The first name of the user
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The last name of the user
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The phone number of the user
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// The Auth0 subject identifier for this user
    /// </summary>
    public string? Auth0Sub { get; set; }

    /// <summary>
    /// The profile image URL for this user
    /// </summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// When this User was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this User was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new User
/// </summary>
public class CreateUserResource
{
    /// <summary>
    /// The first name of the user (optional for Auth0 users)
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The last name of the user (optional for Auth0 users)
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The phone number of the user
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// The Auth0 subject identifier for this user
    /// </summary>
    public string? Auth0Sub { get; set; }
}

/// <summary>
/// Request model for updating an existing User
/// </summary>
public class UpdateUserResource
{
    /// <summary>
    /// The first name of the user
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// The last name of the user
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// The email address of the user
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The phone number of the user
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// The Auth0 subject identifier for this user
    /// </summary>
    public string? Auth0Sub { get; set; }
}

/// <summary>
/// Request model for searching Users
/// </summary>
public class SearchUserRequest : PaginatedRequest
{
    /// <summary>
    /// Filter by Phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Filter by Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Filter by LastName
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Filter by date range
    /// </summary>
    public DateTimeRange? CreatedAtRange { get; set; }

    /// <summary>
    /// Filter by date range
    /// </summary>
    public DateTimeRange? UpdatedAtRange { get; set; }
}

/// <summary>
/// Represents a date range for filtering
/// </summary>
public class DateTimeRange
{
    /// <summary>
    /// The start of the date range (inclusive)
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// The end of the date range (inclusive)
    /// </summary>
    public DateTime? To { get; set; }
}

/// <summary>
/// Response model for the me-context endpoint (current user's display info from ATS)
/// </summary>
public class UserMeContextResource
{
    /// <summary>
    /// The user's display name from the ATS
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The user's Auth0 sub claim
    /// </summary>
    public string? Auth0Sub { get; set; }
}