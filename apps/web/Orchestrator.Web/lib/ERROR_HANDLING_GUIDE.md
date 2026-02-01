# Error Handling Guide

This guide explains the error handling architecture implemented in the Surrova Platform.

## Overview

The platform now has a comprehensive error handling system that provides:
- **Structured error responses** from the API
- **Differentiation between business and technical errors**
- **Automatic session timeout handling**
- **Centralized error display** with toast notifications
- **Easy-to-use hooks** for server action error handling

## Architecture

### Backend (API)

#### 1. Exception Types

The API uses two main exception types:

- **`BusinessBaseException`**: User-facing errors (validation, not found, business rules)
  - HTTP Status: 400 or 404
  - User-friendly messages that can be displayed directly to users
  - Examples: "Email already exists", "Invalid file format"

- **`TechnicalBaseException`**: System errors (database, external services)
  - HTTP Status: 500
  - Internal error details that should NOT be exposed to users
  - Examples: Database connection errors, external API failures

#### 2. Error Response Format

All errors return a structured JSON response:

```json
{
  "statusCode": 400,
  "message": "Email already exists",
  "exceptionType": "UserValidationException",
  "isBusinessException": true,
  "isTechnicalException": false,
  "timestamp": "2025-01-15T10:30:00Z"
}
```

#### 3. Authorization

All controllers now have proper authorization:
- Class-level `[Authorize]` attribute for protected endpoints
- Method-level `[AllowAnonymous]` for public endpoints
- Automatic 401 responses for unauthenticated requests

### Frontend

#### 1. API Clients

**For Client Components:**
```typescript
import { apiClient } from '@/lib/api-client';

// Automatically handles authentication and 401 redirects
const personas = await apiClient.get<Persona[]>('/Persona');
```

**For Server Actions:**
```typescript
'use server';
import { apiPost } from '@/lib/api-client-server';

export async function createPersona(data: CreatePersonaRequest) {
  return await apiPost<Persona>('/Persona', data);
}
```

**For File Uploads (Server Actions):**
```typescript
'use server';
import { apiPostFormData } from '@/lib/api-client-server';

export async function uploadImage(formData: FormData) {
  const data = await apiPostFormData<{ url: string }>(
    '/Image/upload',
    formData
  );
  return data.url;
}
```

#### 2. Error Handling in Components

**Using the useServerAction Hook (Recommended):**

```typescript
import { useServerAction } from '@/lib/use-server-action';
import { createPersona } from './actions';

export function CreatePersonaForm() {
  const { execute, isLoading, error } = useServerAction(createPersona, {
    successMessage: 'Persona created successfully!',
    onSuccess: () => router.push('/personas')
  });

  const handleSubmit = async (formData: FormData) => {
    await execute(formData);
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* Form fields */}
      {error && <div className="text-destructive">{error}</div>}
      <Button type="submit" disabled={isLoading}>
        {isLoading ? 'Creating...' : 'Create'}
      </Button>
    </form>
  );
}
```

**Manual Error Handling:**

```typescript
import { handleApiError, showSuccess } from '@/lib/error-handler';

try {
  const result = await createPersona(formData);
  showSuccess('Persona created successfully!');
  router.push('/personas');
} catch (error) {
  // Business exceptions show actual error message
  // Technical exceptions show generic message
  handleApiError(error);
}
```

**Custom Error Handling:**

```typescript
import { handleApiError, ApiClientError } from '@/lib/error-handler';

try {
  await updatePersona(id, data);
} catch (error) {
  handleApiError(error, (apiError) => {
    // Custom handling for specific errors
    if (apiError.error.message.includes('duplicate')) {
      showWarning('This persona name is already taken');
      return true; // Handled
    }
    return false; // Use default handling
  });
}
```

## Session Timeout Handling

The platform now handles session timeouts gracefully:

1. **Rolling Sessions**: Sessions automatically extend on activity (24 hours)
2. **Automatic Refresh**: Auth0 SDK automatically refreshes tokens
3. **Graceful Redirect**: 401 responses automatically redirect to login
4. **No Silent Failures**: Users always know when they need to re-authenticate

### Configuration

Auth0 is configured with rolling sessions in `lib/auth0.ts`:

```typescript
session: {
  rolling: true,
  rollingDuration: 60 * 60 * 24, // 24 hours
}
```

## Best Practices

### 1. Always Use API Clients

❌ **Don't do this:**
```typescript
const response = await fetch('/api/v1/Persona', {
  headers: { Authorization: `Bearer ${token}` }
});
```

✅ **Do this:**
```typescript
const persona = await apiClient.get<Persona>('/Persona');
```

### 2. Handle Errors Appropriately

❌ **Don't do this:**
```typescript
try {
  await action();
} catch (error) {
  console.error(error); // Silent failure
}
```

✅ **Do this:**
```typescript
try {
  await action();
  showSuccess('Action completed!');
} catch (error) {
  handleApiError(error);
}
```

### 3. Use the useServerAction Hook

❌ **Don't do this:**
```typescript
const [isLoading, setIsLoading] = useState(false);
const [error, setError] = useState<string | null>(null);

const handleSubmit = async () => {
  setIsLoading(true);
  try {
    await action();
    setError(null);
  } catch (err) {
    setError(err.message);
  } finally {
    setIsLoading(false);
  }
};
```

✅ **Do this:**
```typescript
const { execute, isLoading, error } = useServerAction(action, {
  successMessage: 'Success!',
  onSuccess: () => router.push('/next-page')
});
```

### 4. Provide User-Friendly Error Messages

When throwing exceptions in the API, use clear, actionable messages:

❌ **Don't do this:**
```csharp
throw new UserValidationException("ERR_001");
```

✅ **Do this:**
```csharp
throw new UserValidationException("Email address is already registered. Please use a different email or try logging in.");
```

### 5. Differentiate Business vs Technical Errors

**Business Exceptions** (user can fix):
```csharp
throw new UserValidationException("Display name must be unique");
throw new TopicValidationException("Image file size must be under 5MB");
```

**Technical Exceptions** (system issue):
```csharp
throw new TechnicalBaseException("Failed to connect to database");
throw new TechnicalBaseException("External API timeout");
```

## Testing Error Handling

### Test Business Exceptions

```typescript
test('shows validation error when email is duplicate', async () => {
  const { execute, error } = useServerAction(createUser);
  
  await execute({ email: 'existing@example.com' });
  
  expect(error).toBe('Email already exists');
  expect(screen.getByRole('alert')).toHaveTextContent('Email already exists');
});
```

### Test Technical Exceptions

```typescript
test('shows generic error for technical failures', async () => {
  // Mock technical exception
  mockApiClient.get.mockRejectedValue(
    new ApiClientError({
      statusCode: 500,
      message: 'Database connection failed',
      isTechnicalException: true,
      // ...
    })
  );
  
  const { execute } = useServerAction(fetchData);
  await execute();
  
  // Should show generic message, not internal error
  expect(screen.getByRole('alert')).toHaveTextContent('Something went wrong');
});
```

## Migration Checklist

When updating existing code to use the new error handling:

- [ ] Replace direct `fetch` calls with `apiClient` or `apiClientServer`
- [ ] Update server actions to use `apiClientServer` helper
- [ ] Replace manual error state with `useServerAction` hook
- [ ] Remove old try-catch error handling if using `useServerAction`
- [ ] Add success messages for user actions
- [ ] Test error scenarios (validation, auth, server errors)
- [ ] Verify session timeout redirects work correctly

## Examples

See these files for complete examples:
- `lib/upload-image.ts` - Server action with file upload
- `components/image-upload.tsx` - Component with error handling
- `app/create-persona/page.tsx` - Form with server action

## Support

If you encounter issues with error handling:
1. Check the browser console for detailed error logs
2. Verify Auth0 session is valid (check `/api/auth/me`)
3. Check API logs for exception details
4. Ensure proper authorization attributes on controllers

