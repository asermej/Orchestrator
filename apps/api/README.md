# Platform API

The backend services for the Surrova Platform, built with .NET 9 and Clean Architecture principles.

**[← Back to Main Documentation](../../README.md)**

## 🏗️ Architecture

The API follows Clean Architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    Platform.Api                            │
│              (HTTP Controllers & API)                      │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                Platform.DomainLayer                        │
│              (Business Logic & Data Access)                │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                Platform.AcceptanceTests                    │
│              (Integration Testing)                         │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                Platform.Database                           │
│              (Liquibase Migrations)                        │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
apps/api/
├── README.md                          # This file
├── Platform.Api/                      # HTTP API Layer
│   ├── Controllers/                   # API Controllers
│   ├── Mappers/                      # DTO Mapping
│   ├── ResourcesModels/              # API Request/Response Models
│   ├── Middleware/                   # Custom Middleware
│   ├── Common/                       # Shared API Components
│   └── Program.cs                    # Application Entry Point
├── Platform.DomainLayer/             # Business Logic
│   ├── DomainFacade.cs              # Main Business Interface
│   ├── Managers/                     # Business Logic Managers
│   │   ├── Models/                   # Domain Models
│   │   ├── DataLayer/               # Data Access Layer
│   │   ├── ServiceLocators/         # Dependency Injection
│   │   └── Validators/              # Business Validation
│   └── Common/                       # Shared Domain Components
├── Platform.AcceptanceTests/         # Integration Tests
│   ├── DomainLayer/                 # Business Logic Tests
│   └── ServiceLocator/              # Test Infrastructure
└── Platform.Database/               # Database Management
    ├── changelog/                    # Liquibase Changesets
    └── liquibase/                   # Liquibase Configuration
```

## 🚀 Getting Started

### Prerequisites

From the main project directory:

```bash
# Install dependencies (from main README)
brew install postgresql@17 dotnet just openjdk@17
```

### Local Development

1. **Start PostgreSQL:**
   ```bash
   brew services start postgresql@17
   ```

2. **Run database migrations:**
   ```bash
   just db-update
   ```

3. **Build the API:**
   ```bash
   dotnet build apps/Platform.sln
   ```

4. **Run the API:**
   ```bash
   cd apps/api/Platform.Api
   dotnet run
   ```

5. **Access Swagger UI:**
   - Open: `https://localhost:5001/swagger`

### Quick Commands

From the main project directory:

```bash
# Build everything (API + Web)
just build

# Start both API and Web with browser launch
just start

# Run acceptance tests
just test

# Database operations
just db-update    # Run migrations
just db-connect   # Connect to database
```

## 🏛️ Clean Architecture Layers

### Platform.Api (Presentation Layer)
- **Controllers**: HTTP endpoints and request handling
- **Mappers**: Convert between API models and domain models
- **Middleware**: Cross-cutting concerns (exception handling, logging)
- **ResourceModels**: Request/response DTOs

**Key Files:**
- `Controllers/PersonaController.cs` - Persona API endpoints
- `Mappers/PersonaMapper.cs` - DTO mapping logic
- `Middleware/SurrovaExceptionHandlingMiddleware.cs` - Global exception handling

### Platform.DomainLayer (Business Logic)
- **DomainFacade**: Main business interface
- **Managers**: Business logic implementation
- **Models**: Domain entities
- **DataLayer**: Data access abstraction
- **Validators**: Business rule validation

**Key Files:**
- `DomainFacade.cs` - Main business interface
- `Managers/PersonaManager.cs` - Persona business logic
- `Managers/DataLayer/DataFacade.cs` - Data access interface
- `Managers/Models/Persona.cs` - Domain model

### Platform.AcceptanceTests (Testing Layer)
- **Integration Tests**: Test complete business workflows
- **Custom Test Doubles**: No external mocking frameworks
- **Database Testing**: Real database integration

**Key Files:**
- `DomainLayer/DomainFacadeTest.cs` - Main business tests
- `ServiceLocator/ServiceLocatorForAcceptanceTesting.cs` - Test configuration

### Platform.Database (Data Layer)
- **Liquibase Migrations**: Version-controlled schema changes
- **Configuration**: Database connection settings

**Key Files:**
- `changelog/db.changelog-master.xml` - Migration master file
- `liquibase/config/liquibase.properties` - Database configuration

## 🔧 Configuration

### User Secrets (Recommended for Local Development)

The API uses .NET User Secrets to manage sensitive configuration locally. This keeps secrets out of source control and `appsettings.json` files.

#### Initial Setup

Initialize user secrets for the project (only needed once):

```bash
dotnet user-secrets init --project ./apps/api/Platform.Api/Platform.Api.csproj
```

#### Required Secrets

Set up the following secrets for local development:

```bash
# Database connection string
dotnet user-secrets set "ConnectionStrings:DbConnectionString" "Host=localhost;Port=5432;Database=surrova;Username=postgres;Password=postgres" \
  --project ./apps/api/Platform.Api/Platform.Api.csproj

# OpenAI API key (for AI features)
dotnet user-secrets set "Gateways:OpenAI:ApiKey" "<YOUR_OPENAI_KEY>" \
  --project ./apps/api/Platform.Api/Platform.Api.csproj

# Azure Blob Storage connection string (required for image uploads; container name "images" is in appsettings.json)
dotnet user-secrets set "Storage:BlobConnectionString" "<YOUR_AZURE_BLOB_CONNECTION_STRING>" \
  --project ./apps/api/Platform.Api/Platform.Api.csproj

# ElevenLabs API key (optional; for persona voice selection, preview, and cloning)
# If not set, run with Voice__UseFakeElevenLabs=true (e.g. just dev-fake) for fake voice mode
dotnet user-secrets set "Gateways:ElevenLabs:ApiKey" "<YOUR_ELEVENLABS_API_KEY>" \
  --project ./apps/api/Platform.Api/Platform.Api.csproj
```

#### Managing Secrets

```bash
# List all secrets
dotnet user-secrets list --project ./apps/api/Platform.Api/Platform.Api.csproj

# Remove a secret
dotnet user-secrets remove "KeyName" --project ./apps/api/Platform.Api/Platform.Api.csproj

# Clear all secrets
dotnet user-secrets clear --project ./apps/api/Platform.Api/Platform.Api.csproj
```

#### Where Secrets Are Stored

User secrets are stored outside the project directory:
- **macOS/Linux**: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`

The `UserSecretsId` is defined in `Platform.Api.csproj`.

### Configuration Files

- `Platform.Api/appsettings.json` - Default configuration (non-sensitive settings only)
- `Platform.Api/appsettings.Development.json` - Development environment overrides

> **Note**: User secrets automatically override values in `appsettings.json` during local development.

### Environment Variables (CI/CD)

For CI/CD and production environments, use environment variables:

```bash
ConnectionStrings__DbConnectionString="Host=localhost;Port=5432;Database=surrova;Username=postgres;Password=postgres"
Gateways__OpenAI__ApiKey="<YOUR_OPENAI_KEY>"
Storage__BlobConnectionString="<YOUR_AZURE_BLOB_CONNECTION_STRING>"
Gateways__ElevenLabs__ApiKey="<YOUR_ELEVENLABS_API_KEY>"   # Optional; use Voice__UseFakeElevenLabs=true for fake mode
```

`Storage:BlobContainer` defaults to `"images"` and can be overridden in appsettings or environment variables if needed.

## 🧪 Testing

### Acceptance Tests

Run comprehensive integration tests:

```bash
# From main directory
just test

# Or directly
dotnet test apps/api/Platform.AcceptanceTests/Platform.AcceptanceTests.csproj --verbosity normal
```

### Test Philosophy

- **No External Mocking**: Uses custom test doubles and stubs
- **Real Database**: Tests against actual PostgreSQL instance
- **Complete Workflows**: Tests entire business processes
- **Clean Architecture**: Tests through DomainFacade interface only

## 📊 Database Management

### Migrations

```bash
# Run all pending migrations
just db-update

# Connect to database for manual inspection
just db-connect
```

### Adding New Migrations

1. Create new changeset in `Platform.Database/changelog/changes/`
2. Update `Platform.Database/changelog/db.changelog-master.xml`
3. Run `just db-update` to apply

Example changeset structure:
```xml
<changeSet id="003-add-new-table" author="developer">
    <createTable tableName="new_table">
        <column name="id" type="BIGSERIAL">
            <constraints primaryKey="true"/>
        </column>
        <!-- Additional columns -->
    </createTable>
</changeSet>
```

## 🚦 CI/CD Integration

The API is automatically built and tested in GitHub Actions:

1. ✅ .NET 9 setup and restore
2. ✅ PostgreSQL service startup
3. ✅ Database migration execution
4. ✅ API build and compilation
5. ✅ Acceptance test execution

See: [GitHub Actions Configuration](../../.github/workflows/build.yml)

## 🎙️ Voice features (ElevenLabs)

Persona voice selection, preview, and custom voice cloning use the **ElevenLabs** API.

### Configuration

- **Real API:** Set `Gateways:ElevenLabs:ApiKey` in user secrets (or env `Gateways__ElevenLabs__ApiKey`). Default in appsettings: `Voice:UseFakeElevenLabs` is `false`.
- **Fake mode:** Set `Voice:UseFakeElevenLabs=true` (e.g. `Voice__UseFakeElevenLabs=true` in env) to skip real API calls and return deterministic fake voices. Use for local dev without a key or in acceptance tests.

### Endpoints

- `GET /api/v1/Voice/elevenlabs` – List available voices (prebuilt + user clones).
- `POST /api/v1/Voice/consent` – Record consent for voice cloning (required before clone).
- `POST /api/v1/Voice/clone` – Clone a voice from an audio sample (multipart: file + personaId, voiceName, consentRecordId, sampleDurationSeconds).
- `POST /api/v1/Voice/preview` – Preview a voice (returns audio stream).
- `POST /api/v1/Persona/{personaId}/voice/select` – Set the persona’s voice (provider, type, id, name).

Voice sample must be **10–300 seconds**. Clone is rate-limited (e.g. one successful clone per 24 hours per user).

## 📷 Image Storage

Image uploads (e.g. persona profile images) are stored in **Azure Blob Storage** with a private container. Images are served through the API, not via direct blob URLs.

- **Upload**: `POST /api/v1/image/upload` (authenticated) returns a URL like `/api/v1/images/{key}.jpg`
- **Retrieve**: `GET /api/v1/images/{key}` streams the image from blob storage (no auth required)

**Local development:** Set `Storage:BlobConnectionString` in user-secrets (see Configuration above). The container name defaults to `"images"` in appsettings; create this container in your Azure Storage account (or it will be created on first upload).

## 🔍 API Endpoints

### Swagger Documentation

When running locally, comprehensive API documentation is available at:
- **Development**: `https://localhost:5001/swagger`

### Current Endpoints

- **Personas**: CRUD operations for persona management
  - `GET /api/personas` - List personas
  - `POST /api/personas` - Create persona
  - `GET /api/personas/{id}` - Get persona details
  - `PUT /api/personas/{id}` - Update persona
  - `DELETE /api/personas/{id}` - Delete persona

## 🛠️ Development Guidelines

### Adding New Features

1. **Domain Model**: Add/update models in `Platform.DomainLayer/Managers/Models/`
2. **Business Logic**: Implement in appropriate manager class
3. **Data Access**: Add methods to DataFacade and DataManager
4. **API Layer**: Create/update controller and resource models
5. **Tests**: Add acceptance tests covering the complete workflow
6. **Database**: Create migration if schema changes needed

### Code Standards

- Follow Clean Architecture principles
- Use dependency injection through ServiceLocator
- Implement proper exception handling
- Write acceptance tests for all new features
- Use async/await for all I/O operations

## 📋 Cursor Rules & Templates

The API layer uses comprehensive Cursor rules and Handlebars templates for consistent code generation and development patterns.

### 🔧 API Rules (.cursor/rules/)

#### endpoint-workflow.mdc
- **Type**: Interactive Development Workflow
- **Purpose**: Guides step-by-step creation of new API endpoints
- **Scope**: Complete feature development from database to API
- **Key Features**:
  - Interactive prompts for feature definition
  - Automatic code generation using templates
  - Verification against template expectations
  - Phase-based development (Database → Domain → API)

#### principles.mdc
- **Type**: Architectural Guidelines
- **Purpose**: Core principles for Clean Architecture implementation
- **Scope**: All backend development
- **Key Areas**:
  - Clean Architecture boundaries and dependencies
  - Facade and Manager patterns
  - Validation strategy (API vs Domain layer)
  - Exception handling and error responses
  - Result and pagination patterns
  - Database and data access rules

### 🏗️ API Templates (.cursor/templates/)

#### Create Templates (create/)
- **Controller.hbs** - Complete API controller with all CRUD operations
- **Manager.hbs** - Domain manager with business logic
- **DataManager.hbs** - Data access layer with SQL queries
- **DataFacade.hbs** - Data facade extensions
- **DomainFacade.Base.hbs** - Domain facade extensions
- **ResourceModel.hbs** - API request/response models
- **Mapper.hbs** - Mapping between API and domain models
- **Mapping.hbs** - AutoMapper configuration
- **DomainModel.hbs** - Core domain entity
- **Validator.hbs** - Business validation logic
- **Test.hbs** - Comprehensive acceptance tests
- **ValidationException.hbs** - Custom validation exceptions
- **NotFoundException.hbs** - Not found exceptions
- **DuplicateException.hbs** - Duplicate entity exceptions

#### Update Templates (update/)
- **AddMethods.hbs** - Add new methods to existing classes

#### Common Templates (common/)
- **Mapping.hbs** - Shared mapping configurations

### 🎯 Template Features

- **Handlebars-based**: Uses `.hbs` syntax for dynamic code generation
- **Feature-driven**: Templates generate complete features, not just individual files
- **Consistent patterns**: Ensures all generated code follows architectural principles
- **Validation included**: Generated code includes proper validation and error handling
- **Test coverage**: Automatic generation of acceptance tests

## 🗄️ Database Rules & Templates

The database layer maintains separate rules and templates for Liquibase migrations.

### 📋 Database Rules (Platform.Database/.cursor/rules/)

#### database-workflow.mdc
- **Type**: Database Development Workflow
- **Purpose**: Standard procedures for creating and updating database tables
- **Scope**: All Liquibase migrations
- **Key Features**:
  - Interactive prompts for table creation/updates
  - Changeset naming conventions
  - Required table structure (audit fields, constraints)
  - Performance guidelines (indexing)

#### liquibase-best-practices.mdc
- **Type**: Best Practices Guide
- **Purpose**: Liquibase-specific guidelines and patterns
- **Scope**: Database schema management
- **Key Areas**:
  - Changeset structure and naming
  - Constraint definitions
  - Index creation strategies
  - Migration rollback considerations

### 🏗️ Database Templates (Platform.Database/.cursor/templates/)

#### Create Templates (create/)
- **Table.hbs** - Complete table creation with constraints and indexes
- **Index.hbs** - Database index creation
- **Function.hbs** - PostgreSQL function definitions
- **Trigger.hbs** - Database trigger creation

#### Update Templates (update/)
- **AlterTable.hbs** - Table modification changesets
- **AddColumn.hbs** - Column addition
- **AddConstraint.hbs** - Constraint addition

### 🎯 Database Template Features

- **Liquibase XML**: Generates proper Liquibase changeset XML
- **Timestamp-based naming**: Automatic timestamp generation for changesets
- **Constraint compliance**: Ensures proper constraint definitions
- **Audit field inclusion**: Automatic inclusion of created_at/updated_at fields
- **Index generation**: Automatic index creation for foreign keys

---

**Navigation:**
- [← Main Documentation](../../README.md)
- [Web Documentation →](../web/Platform.Web/README.md)
- [GitHub Actions →](../../.github/workflows/build.yml) 