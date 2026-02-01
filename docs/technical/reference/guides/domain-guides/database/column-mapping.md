# Column Mapping Guide

## Overview

This guide explains how to handle column mapping between snake_case database columns and PascalCase C# properties using Dapper.

## The Problem

PostgreSQL typically uses snake_case for column names (e.g., `first_name`, `created_at`), while C# uses PascalCase for property names (e.g., `FirstName`, `CreatedAt`). Dapper needs to be configured to map between these naming conventions.

## Solution: DapperConfiguration

We use a centralized `DapperConfiguration` class to handle column mapping:

### 1. String Extensions

```csharp
public static class StringExtensions
{
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        // Split by underscore and capitalize each word
        var words = str.Split('_');
        var result = string.Join("", words.Select(word => 
        {
            if (word.Length == 0) return "";
            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        }));
        
        return result;
    }
}
```

### 2. Dapper Configuration

```csharp
public static class DapperConfiguration
{
    public static void ConfigureSnakeCaseMapping<T>()
    {
        SqlMapper.SetTypeMap(typeof(T), new CustomPropertyTypeMap(typeof(T), (type, columnName) =>
        {
            var pascalCaseName = columnName.ToPascalCase();
            return type.GetProperty(pascalCaseName);
        }));
    }
}
```

### 3. Usage in DataManager

```csharp
static UserDataManager()
{
    // Configure Dapper to map snake_case columns to PascalCase properties
    DapperConfiguration.ConfigureSnakeCaseMapping<User>();
}
```

## Timezone Handling

### Problem

PostgreSQL may store datetime values in different timezones, causing issues when comparing with UTC times in tests.

### Solution

Use PostgreSQL's `AT TIME ZONE 'UTC'` syntax:

```sql
-- For SELECT queries
SELECT created_at AT TIME ZONE 'UTC' as created_at
FROM users;

-- For INSERT/UPDATE queries
INSERT INTO users (created_at) VALUES (@createdAt AT TIME ZONE 'UTC');
```

### Helper Methods

```csharp
public static class DapperConfiguration
{
    public static string GetUtcTimezoneSql(string columnName)
    {
        return $"{columnName} AT TIME ZONE 'UTC' as {columnName}";
    }

    public static string GetUtcTimezoneValueSql(string parameterName)
    {
        return $"@{parameterName} AT TIME ZONE 'UTC'";
    }
}
```

## Best Practices

1. **Always use Column attributes** in domain models:
   ```csharp
   [Column("first_name")]
   public string FirstName { get; set; }
   ```

2. **Configure Dapper mapping** in the static constructor of each DataManager

3. **Use UTC timezone handling** for all datetime operations

4. **Test the mapping** with acceptance tests to ensure it works correctly

## Example

```csharp
// Domain Model
public class User : Entity
{
    [Column("first_name")]
    public string FirstName { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

// DataManager
static UserDataManager()
{
    DapperConfiguration.ConfigureSnakeCaseMapping<User>();
}

// SQL Query
const string sql = @"
    SELECT 
        first_name,
        created_at AT TIME ZONE 'UTC' as created_at
    FROM users 
    WHERE id = @Id";
```

## Troubleshooting

If column mapping is not working:

1. Check that `DapperConfiguration.ConfigureSnakeCaseMapping<T>()` is called
2. Verify that Column attributes are present on domain model properties
3. Ensure the database column names match the Column attribute values
4. Test with debug output to see the actual mapping process







