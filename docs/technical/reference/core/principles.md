# Backend Architecture Principles

**CRITICAL**: This document contains the foundational principles that govern the architecture and implementation of all backend services. It is the single source of truth for architectural boundaries, data handling, validation, and error management. All other rule files will reference these principles.

---

## 1. Clean Architecture Boundaries

The platform adheres to Clean Architecture. The dependencies flow in one direction, from the outer layers to the inner layers.

- **`Orchestrator.Api`**: The API layer. It references only `Orchestrator.Domain`. It is responsible for controllers, request/response models (`Resources`), and mapping.
- **`Orchestrator.Domain`**: The core business logic. It has **no project references**. It contains facades, managers, domain models, and the data access layer.
- **`Orchestrator.AcceptanceTests`**: References `Orchestrator.Domain` only. It has special access to internals via `[assembly: InternalsVisibleTo("Orchestrator.AcceptanceTests")]`.

---

## 2. Facade and Manager Pattern

- **`DomainFacade`**: The single public entry point to the `Orchestrator.Domain`. All business operations are exposed through this facade. The API layer should *only* interact with the `DomainFacade`.
- **`DataFacade`**: The single entry point for data access operations. It is internal to the `Domain` and used by managers.
- **Managers** (e.g., `PersonaManager`): Contain the core business logic for a specific domain entity. They use the `DataFacade` to retrieve and persist data and are invoked by the `DomainFacade`.

### **CRITICAL: Join Table Encapsulation**

**Join tables must NOT be exposed in DomainFacade.** They are implementation details that should be hidden from external consumers.

#### **The Problem with Exposing Join Tables**
```csharp
// ❌ DON'T: Exposing join table operations in DomainFacade
public sealed partial class DomainFacade
{
    // BAD: These expose internal implementation details
    public async Task<PersonaTopic> AddTopicToPersona(Guid personaId, Guid topicId, ...);
    public async Task<EntityJoin> AddJoinRecord(EntityJoin joinRecord);
}
```

**Why is this bad?**
1. **Leaks Implementation Details**: External code knows about the internal structure
2. **Breaks Encapsulation**: Can't refactor join table structure without breaking external code
3. **Violates Single Responsibility**: Join tables serve the relationship, not the business domain
4. **Creates Unnecessary Complexity**: External code must understand internal relationships

#### **The Correct Approach: Hide Join Tables**

Join table operations should be exposed through the **primary entity's manager** and facade methods:

```csharp
// ✅ DO: Expose through primary entity (Persona)
public sealed partial class DomainFacade
{
    // Good: These are business operations, not implementation details
    public async Task<PersonaTopic> AddTopicToPersona(Guid personaId, Guid topicId, ...);
    public async Task<IEnumerable<Tag>> GetPersonaTags(Guid personaId);
    public async Task<bool> RemoveTagFromPersona(Guid personaId, Guid tagId);
}

// Implementation: PersonaManager handles the join table internally
internal sealed class PersonaManager
{
    private PersonaTopicManager PersonaTopicManager { get; }
    private PersonaTagManager PersonaTagManager { get; }
    
    public async Task<PersonaTopic> AddTopicToPersona(Guid personaId, Guid topicId, ...)
    {
        // Delegates to join table manager internally
        return await PersonaTopicManager.AddTopicToPersona(personaId, topicId, ...);
    }
}
```

#### **Implementation Pattern**

1. **Join Table Managers Stay Internal**: `PersonaTopicManager`, `PersonaCategoryManager`, `TopicTagManager` are implementation details
2. **Primary Entity Exposes Operations**: `PersonaManager` exposes methods for managing persona-topic relationships
3. **DomainFacade Delegates to Primary Entity**: Consumers only see business operations on `Persona`, not join tables
4. **Join Table Models Can Be Returned**: While the join table itself isn't exposed, the data model (e.g., `PersonaTopic`) can be returned if it carries business-relevant information

#### **Examples by Entity Type**

**Many-to-Many Relationships:**
```csharp
// Topic ↔ Tag relationship
// ❌ DON'T expose TopicTagManager in DomainFacade
// ✅ DO expose through TopicManager:
public async Task<Tag> AddTagToTopic(Guid topicId, string tagName);
public async Task<IEnumerable<Tag>> GetTopicTags(Guid topicId);
public async Task<bool> RemoveTagFromTopic(Guid topicId, Guid tagId);
```

**Association with Additional Data:**
```csharp
// Persona ↔ Topic with training content
// ❌ DON'T expose PersonaTopicManager
// ✅ DO expose through PersonaManager:
public async Task<PersonaTopic> AddTopicToPersona(Guid personaId, Guid topicId, string content);
public async Task<string> GetPersonaTopicContent(Guid personaId, Guid topicId);
```

#### **Benefits of This Approach**

1. **Encapsulation**: Join table structure is hidden, can be refactored freely
2. **Intuitive API**: Consumers think in terms of business entities, not database structure
3. **Flexibility**: Can change from many-to-many to one-to-many without breaking API
4. **Testability**: Tests interact with business operations, not implementation details
5. **Single Responsibility**: Each manager handles its entity's relationships

#### **Migration Path for Existing Join Table Exposures**

If you have existing join table exposures in DomainFacade:

1. **Identify Primary Entity**: Determine which entity owns the relationship (e.g., `Persona` owns `PersonaTopic`)
2. **Move Operations to Manager**: Add relationship methods to the primary entity's manager
3. **Update DomainFacade**: Expose through primary entity's facade extension
4. **Keep Join Table Manager Internal**: Remove it from `DomainFacade.cs` lazy initialization
5. **Delete Join Table Facade Extensions**: Remove `DomainFacade.PersonaTopic.cs` etc.
6. **Update API Layer**: Change controllers to call primary entity operations

---

## 3. Validation Strategy

Validation is handled at two distinct layers:

1.  **API Layer (`Orchestrator.Api`)**:
    - **Responsibility**: Validates the **shape and format** of incoming HTTP requests.
    - **Examples**: Checks for required fields, correct data types (e.g., is a number actually a number?), and valid enum values. This is "can't be null" or "must be a valid email format" style validation.
    - **Implementation**: Uses attributes on `Resource` models.

2.  **Domain Layer (`Orchestrator.Domain`)**:
    - **Responsibility**: Enforces all **business rules and data integrity**.
    - **Examples**: Checks if a user email already exists, if a requested inventory item is in stock, or if a status transition is valid.
    - **Implementation**: Logic is placed within Domain Managers before any data access occurs.

---

## 4. Exception Handling and Error Responses

- **DO NOT** catch and handle exceptions in the `DataLayer` or `Domain` managers (e.g., `PostgresException`, `NullReferenceException`).
- **Let Exceptions Propagate**: All exceptions should be allowed to bubble up from the inner layers to the `Orchestrator.Api` layer.
- **Middleware Translation**: The `SurrovaExceptionHandlingMiddleware` in `Orchestrator.Api` is responsible for catching all unhandled exceptions.
- **`ExceptionToHttpTranslator`**: This service translates specific exception types (`SurrovaNotFoundBaseException`, `SurrovaBusinessBaseException`, etc.) into standard, user-friendly `ErrorResponse` objects with appropriate HTTP status codes. This ensures a consistent error contract for all API clients.

---

## 5. Result and Pagination Patterns

- **`Result<T>`**: All `DomainFacade` methods that can fail must return a `Result<T>`. This object wraps either the successful value (`Value`) or an error (`Error`), allowing the API layer to handle failures gracefully without relying on exceptions for control flow.
- **`PaginatedResult<T>`**: All search or list operations must return a `PaginatedResult<T>` to ensure consistent and performant pagination across the entire API.

---

## 6. Database and Data Access Rules

### Liquibase and Schema Management
- **One Change per File**: Each new table or significant alteration should be in its own Liquibase XML file in `Orchestrator.Database/changelog/changes/`.
- **Required Columns**: All tables **must** include:
  - `id`: A `SERIAL` or `BIGSERIAL` primary key.
  - `created_at`: `TIMESTAMP WITH TIME ZONE`, non-nullable.
  - `updated_at`: `TIMESTAMP WITH TIME ZONE`, non-nullable.
- **Constraints**: Always define constraints using the `<constraints>` element inside `<column>`, not as attributes on the column itself.
- **Indexing**: Always create indexes for foreign keys and any columns frequently used in `WHERE`, `JOIN`, or `ORDER BY` clauses.

### Data Integrity
- **Database-Level Validation**: Use `NOT NULL`, `UNIQUE`, and `FOREIGN KEY` constraints to enforce data integrity at the lowest level.
- **Domain-Level Validation**: Business rules must be validated in the domain layer *before* making a database call.

