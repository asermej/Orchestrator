# Validation Rules Reference

## Overview

This document provides the reference implementation for common validation rules using FluentValidation in the API layer.

## Implementation

```csharp
using FluentValidation;
using System;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Orchestrator.Api.Common.Validation;

public static class CommonValidationRules
{
    /// <summary>
    /// Validates that a property has the correct Column attribute for database mapping
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidColumnMapping<T>(
        this IRuleBuilder<T, string> ruleBuilder, 
        string expectedColumnName)
    {
        return ruleBuilder
            .Must((obj, propertyName) =>
            {
                var property = typeof(T).GetProperty(propertyName);
                if (property == null) return false;
                
                var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
                return columnAttribute?.Name == expectedColumnName;
            })
            .WithMessage($"Property must have [Column(\"{expectedColumnName}\")] attribute");
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }

    /// <summary>
    /// Validates phone number format (E.164 international format)
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
    }

    /// <summary>
    /// Validates URL format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidUrl<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("URL is required")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid URL format");
    }

    /// <summary>
    /// Validates date string format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidDate<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Date is required")
            .Must(date => DateTime.TryParse(date, out _))
            .WithMessage("Invalid date format");
    }

    /// <summary>
    /// Validates enum value
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidEnum<T, TEnum>(
        this IRuleBuilder<T, string> ruleBuilder)
        where TEnum : struct, Enum
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Value is required")
            .Must(value => Enum.TryParse<TEnum>(value, true, out _))
            .WithMessage($"Invalid value. Must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}");
    }

    /// <summary>
    /// Validates GUID format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidGuid<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("GUID is required")
            .Must(guid => Guid.TryParse(guid, out _))
            .WithMessage("Invalid GUID format");
    }

    /// <summary>
    /// Validates JSON format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidJson<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("JSON is required")
            .Must(json => 
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(json);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Invalid JSON format");
    }

    /// <summary>
    /// Validates against a custom regex pattern
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidRegex<T>(
        this IRuleBuilder<T, string> ruleBuilder, 
        string pattern, 
        string message)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Value is required")
            .Matches(pattern).WithMessage(message);
    }

    /// <summary>
    /// Conditional required validation
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> RequiredIf<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder,
        Func<T, bool> predicate,
        string message = "Field is required when condition is met")
    {
        return ruleBuilder
            .Must((obj, value) => !predicate(obj) || value != null)
            .WithMessage(message);
    }
}
```

## Usage Examples

### Basic Validator

```csharp
public class CreateUserResourceValidator : AbstractValidator<CreateUserResource>
{
    public CreateUserResourceValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Phone).ValidPhoneNumber();
        RuleFor(x => x.Website).ValidUrl();
    }
}
```

### Conditional Validation

```csharp
public class UpdateUserResourceValidator : AbstractValidator<UpdateUserResource>
{
    public UpdateUserResourceValidator()
    {
        RuleFor(x => x.Email)
            .ValidEmail()
            .When(x => !string.IsNullOrEmpty(x.Email));
            
        RuleFor(x => x.AlternateEmail)
            .RequiredIf(x => x.RequiresBackupEmail, "Alternate email required");
    }
}
```

### Custom Pattern Validation

```csharp
RuleFor(x => x.EmployeeId)
    .ValidRegex(@"^EMP-\d{6}$", "Employee ID must match format EMP-XXXXXX");
```

## Best Practices

1. **Use built-in validators when possible** - They're well-tested and performant
2. **Chain validators** - Combine multiple rules for comprehensive validation
3. **Use conditional validation** - Only validate when relevant
4. **Provide clear error messages** - Help users understand what went wrong
5. **Keep validators focused** - One validator per resource model







