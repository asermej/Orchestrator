# Error Handling Reference

## Overview

This document provides the reference implementation for error handling in the API layer, including error responses, validation errors, and result pattern utilities.

## Implementation

```csharp
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using FluentValidation.Results;

namespace Orchestrator.Api.Common;

public class ErrorResponse
{
    public string Message { get; set; }
    public string? Code { get; set; }
    public string? Details { get; set; }
    public IDictionary<string, string[]>? ValidationErrors { get; set; }
    public string? StackTrace { get; set; }

    public static ErrorResponse FromException(Exception ex, bool includeStackTrace = false)
    {
        return new ErrorResponse
        {
            Message = ex.Message,
            Code = ex.GetType().Name,
            Details = ex.InnerException?.Message,
            StackTrace = includeStackTrace ? ex.StackTrace : null
        };
    }

    public static ErrorResponse FromValidationErrors(ValidationResult validationResult)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var error in validationResult.Errors)
        {
            if (!errors.ContainsKey(error.PropertyName))
            {
                errors[error.PropertyName] = new string[] { error.ErrorMessage };
            }
            else
            {
                var messages = new List<string>(errors[error.PropertyName]) { error.ErrorMessage };
                errors[error.PropertyName] = messages.ToArray();
            }
        }

        return new ErrorResponse
        {
            Message = "Validation failed",
            Code = "ValidationError",
            ValidationErrors = errors
        };
    }
}

public static class ErrorResponseExtensions
{
    public static ActionResult<ErrorResponse> ToErrorResponse(this Exception ex, bool includeStackTrace = false)
    {
        var response = ErrorResponse.FromException(ex, includeStackTrace);
        return ex switch
        {
            ArgumentException => new BadRequestObjectResult(response),
            UnauthorizedAccessException => new UnauthorizedObjectResult(response),
            KeyNotFoundException => new NotFoundObjectResult(response),
            InvalidOperationException => new BadRequestObjectResult(response),
            _ => new ObjectResult(response) { StatusCode = 500 }
        };
    }

    public static ActionResult<ErrorResponse> ToErrorResponse(this ValidationResult validationResult)
    {
        var response = ErrorResponse.FromValidationErrors(validationResult);
        return new BadRequestObjectResult(response);
    }
}

public static class ControllerExtensions
{
    public static ActionResult<T> HandleResult<T>(this ControllerBase controller, Result<T> result)
    {
        return result.Match<ActionResult<T>>(
            success => controller.Ok(success),
            error => error.ToErrorResponse()
        );
    }

    public static ActionResult HandleResult(this ControllerBase controller, Result result)
    {
        return result.Match<ActionResult>(
            success => controller.Ok(),
            error => error.ToErrorResponse()
        );
    }
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Exception? Error { get; }

    private Result(bool isSuccess, T? value, Exception? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Exception error) => new(false, default, error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}

public class Result
{
    public bool IsSuccess { get; }
    public Exception? Error { get; }

    private Result(bool isSuccess, Exception? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Exception error) => new(false, error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Exception, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error!);
    }
}
```

## Usage

### In Controllers

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<UserResource>> GetById(Guid id)
{
    var result = await _domainFacade.GetUserById(id);
    return this.HandleResult(result);
}
```

### Creating Error Responses

```csharp
// From exception
var errorResponse = ErrorResponse.FromException(ex);

// From validation result
var errorResponse = ErrorResponse.FromValidationErrors(validationResult);
```







