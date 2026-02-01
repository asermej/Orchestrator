# Timezone Handling Guide

## Overview

This guide explains how to handle timezone issues when working with PostgreSQL and .NET applications.

## The Problem

PostgreSQL may store datetime values in different timezones depending on the database configuration, which can cause issues when:

1. Comparing datetime values in tests
2. Ensuring consistent timezone handling across the application
3. Working with UTC timestamps

## Solution: PostgreSQL UTC Timezone Handling

### 1. For SELECT Queries

Use `AT TIME ZONE 'UTC'` to ensure datetime columns are returned in UTC:

```sql
SELECT 
    id,
    first_name,
    created_at AT TIME ZONE 'UTC' as created_at,
    updated_at AT TIME ZONE 'UTC' as updated_at
FROM users
WHERE id = @Id;
```

### 2. For INSERT Queries

Use `AT TIME ZONE 'UTC'` when inserting datetime values:

```sql
INSERT INTO users (first_name, created_at, updated_at)
VALUES (@FirstName, @CreatedAt AT TIME ZONE 'UTC', @UpdatedAt AT TIME ZONE 'UTC');
```

### 3. For UPDATE Queries

Use `AT TIME ZONE 'UTC'` when updating datetime values:

```sql
UPDATE users 
SET updated_at = @UpdatedAt AT TIME ZONE 'UTC'
WHERE id = @Id;
```

## Helper Methods

The `DapperConfiguration` class provides helper methods for timezone handling:

```csharp
public static class DapperConfiguration
{
    /// <summary>
    /// Gets UTC timezone SQL fragment for datetime columns
    /// </summary>
    public static string GetUtcTimezoneSql(string columnName)
    {
        return $"{columnName} AT TIME ZONE 'UTC' as {columnName}";
    }

    /// <summary>
    /// Gets UTC timezone SQL fragment for datetime values
    /// </summary>
    public static string GetUtcTimezoneValueSql(string parameterName)
    {
        return $"@{parameterName} AT TIME ZONE 'UTC'";
    }
}
```

## Usage in DataManager Templates

### SELECT Queries

```csharp
const string sql = @"
    SELECT 
        id,
        first_name,
        created_at AT TIME ZONE 'UTC' as created_at,
        updated_at AT TIME ZONE 'UTC' as updated_at
    FROM users 
    WHERE id = @Id";
```

### INSERT Queries

```csharp
const string sql = @"
    INSERT INTO users (first_name, created_at, created_by)
    VALUES (@FirstName, @CreatedAt AT TIME ZONE 'UTC', @CreatedBy)
    RETURNING id";
```

### UPDATE Queries

```csharp
const string sql = @"
    UPDATE users 
    SET 
        first_name = @FirstName,
        updated_at = @UpdatedAt AT TIME ZONE 'UTC'
    WHERE id = @Id
    RETURNING 
        id,
        first_name,
        created_at AT TIME ZONE 'UTC' as created_at,
        updated_at AT TIME ZONE 'UTC' as updated_at";
```

## Best Practices

1. **Always use UTC for datetime storage**: Store all datetime values in UTC
2. **Use timezone handling in SQL**: Always use `AT TIME ZONE 'UTC'` in SQL queries
3. **Consistent datetime handling**: Use `DateTime.UtcNow` for new timestamps
4. **Test timezone handling**: Include timezone tests in acceptance tests
5. **Document timezone requirements**: Clearly document timezone expectations

## Testing

When writing tests that involve datetime comparisons:

```csharp
[TestMethod]
public async Task CreateUser_ValidData_SetsCorrectCreatedAt()
{
    // Arrange
    var dto = new CreateUserDto { FirstName = "John" };

    // Act
    var result = await _domainFacade.CreateUser(dto);

    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.IsTrue(result.Data.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    Assert.IsTrue(result.Data.CreatedAt <= DateTime.UtcNow.AddMinutes(1));
}
```

## Common Issues and Solutions

### Issue: Tests failing due to timezone differences

**Solution**: Use `AT TIME ZONE 'UTC'` in all SQL queries

### Issue: Inconsistent datetime handling

**Solution**: Always use `DateTime.UtcNow` for new timestamps

### Issue: Database storing local time instead of UTC

**Solution**: Use `AT TIME ZONE 'UTC'` when inserting/updating datetime values

## Template Integration

The template system automatically includes timezone handling in:

- DataManager templates (SELECT, INSERT, UPDATE queries)
- DomainModel templates (Column attributes for datetime fields)
- Test templates (datetime assertions)

This ensures consistent timezone handling across all generated code.
