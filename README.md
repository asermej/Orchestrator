# Orchestrator Platform

A full-stack platform built with Clean Architecture principles, featuring a .NET API backend and Next.js frontend.

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Orchestrator.Web                            │
│              (Next.js Frontend)                            │
└─────────────────────────────────────────────────────────────┘
                              │ HTTP
┌─────────────────────────────────────────────────────────────┐
│                    Orchestrator.Api                            │
│              (.NET 9 API)                                  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                Orchestrator.Domain                        │
│              (Business Logic)                              │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    PostgreSQL                              │
│              (Database)                                    │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
Orchestrator/
├── README.md                          # This file
├── justfile                           # Build automation
├── apps/
│   ├── Orchestrator.sln                   # .NET Solution
│   ├── api/                          # Backend Services
│   │   ├── README.md                 # → API Documentation
│   │   ├── Orchestrator.Api/             # HTTP API Layer
│   │   ├── Orchestrator.Domain/     # Business Logic
│   │   ├── Orchestrator.AcceptanceTests/ # Integration Tests
│   │   └── Orchestrator.Database/        # Database Migrations
│   └── web/                          # Frontend Application
│       ├── README.md                 # → Web Documentation
│       └── Orchestrator.Web/             # Next.js Application
├── .github/
│   └── workflows/
│       └── build.yml                 # CI/CD Pipeline
└── infrastructure/                   # Infrastructure as Code
```

## 🚀 Quick Start

### Prerequisites

Install required Homebrew packages:

```bash
# Core dependencies
brew install postgresql@17
brew install node@18
brew install dotnet
brew install just
brew install liquibase
```

### Setup

1. **Clone and navigate to the project:**
   ```bash
   git clone <repository-url>
   cd Orchestrator
   ```

2. **Start PostgreSQL:**
   ```bash
   brew services start postgresql@17
   ```

3. **Create database and user:**
   ```bash
   createdb surrova
   createuser -s postgres  # If doesn't exist
   ```

4. **Configure secrets (first time setup):**
   ```bash
   # Initialize user secrets
   dotnet user-secrets init --project ./apps/api/Orchestrator.Api/Orchestrator.Api.csproj

   # Set database connection string
   dotnet user-secrets set "ConnectionStrings:DbConnectionString" \
     "Host=localhost;Port=5432;Database=surrova;Username=postgres;Password=postgres" \
     --project ./apps/api/Orchestrator.Api/Orchestrator.Api.csproj

   # Set Azure Blob Storage connection string (required for image uploads)
   dotnet user-secrets set "Storage:BlobConnectionString" "<YOUR_AZURE_BLOB_CONNECTION_STRING>" \
     --project ./apps/api/Orchestrator.Api/Orchestrator.Api.csproj

   # Set other required secrets (see apps/api/README.md for full list)
   ```

5. **Run database migrations:**
   ```bash
   just db-update
   ```

6. **Build both applications:**
   ```bash
   just build
   ```

7. **Start development servers:**
   ```bash
   just start
   ```

This will open:
- 🌍 **API Swagger**: `https://localhost:5001/swagger`
- 🌐 **Web App**: `http://localhost:3000`

## 🛠️ Available Commands

### Development
- `just build` - Build both API and web applications
- `just start` - Build and start both applications with browser launch
- `just test` - Run acceptance tests
- **Voice (ElevenLabs):** Use real API: `just dev` or `just start` (set ElevenLabs API key in user secrets). Use fake voice (no key): `just dev-api-fake`, `just dev-fake`, or `just start-fake`

### Database
- `just db-update` - Run database migrations
- `just db-connect` - Connect to PostgreSQL database

## 🎙️ Voice features (ElevenLabs)

Persona voice selection and custom voice cloning use **ElevenLabs**. You can:

- **Choose a prebuilt voice** or **create your own voice** from a recording on the General Training page for any persona.
- **Preview** a voice before selecting it.

**Local development:**

- **Real ElevenLabs:** Set the ElevenLabs API key (see [API Documentation](apps/api/README.md#voice-features-elevenlabs)) and run `just dev` or `just start`. Voice list, preview, and cloning will use the live API.
- **Fake mode (no API key):** Run `just dev-fake`, `just start-fake`, or `just dev-api-fake`. The API returns deterministic fake voices and skips real ElevenLabs calls. Use this when you don’t have an API key or for quick UI testing.

**Acceptance tests** run with fake voice enabled so they don’t call ElevenLabs.

## 📚 Documentation

### Application-Specific Documentation
- **[API Documentation](apps/api/README.md)** - .NET API, Domain Layer, and Database
- **[Web Documentation](apps/web/Orchestrator.Web/README.md)** - Next.js Frontend Application

### Architecture Documentation
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Database**: PostgreSQL with Liquibase migrations
- **Testing**: Acceptance tests with custom test doubles (no external mocking frameworks)
- **CI/CD**: GitHub Actions with full integration testing

## 🔧 Technology Stack

### Backend
- **.NET 9** - API framework
- **PostgreSQL 17** - Database
- **Liquibase** - Database migrations
- **Clean Architecture** - Architectural pattern

### Frontend
- **Next.js 15** - React framework
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling
- **Radix UI** - Component library

### Development Tools
- **just** - Command runner
- **GitHub Actions** - CI/CD pipeline

## 🚦 CI/CD Pipeline

The GitHub Actions pipeline automatically:
1. ✅ Sets up .NET 9 and Node.js 18
2. ✅ Installs dependencies for both applications
3. ✅ Runs database migrations
4. ✅ Builds both API and web applications
5. ✅ Runs acceptance tests with PostgreSQL

## 🤝 Contributing

1. Follow the Clean Architecture principles
2. Write acceptance tests for new features
3. Use the provided `just` commands for consistency
4. Update documentation when adding new features

### 📋 Cursor Rules & Templates

The project uses distributed Cursor rules and templates across different components:

#### 🏛️ Main Platform Rules (.cursor/rules/)

- **architecture-overview.mdc** - Complete Clean Architecture guidelines and patterns
  - Clean Architecture layer definitions and dependencies
  - InternalsVisibleTo patterns for testing
  - Result and pagination patterns
  - Test double strategies (no external mocking frameworks)
  - Data flow diagrams and communication patterns

- **dev_workflow.mdc** - Taskmaster development workflow integration
  - Basic development loop (list → next → show → expand → implement)
  - Multi-context workflows with tagged task lists
  - Git feature branching integration
  - PRD-driven feature development

- **taskmaster.mdc** - Comprehensive Taskmaster tool reference
  - MCP tools vs CLI commands
  - Task creation, modification, and status management
  - AI-powered task analysis and expansion
  - Project complexity analysis

- **self_improve.mdc** - Rule improvement and maintenance guidelines
  - Pattern recognition for new rules
  - Rule quality checks and continuous improvement
  - Documentation synchronization

- **cursor_rules.mdc** - Rule formatting and structure standards
  - Required rule structure with YAML frontmatter
  - File references and code example patterns
  - Rule maintenance best practices

#### 🔧 Component-Specific Rules & Templates

- **[API Rules & Templates](apps/api/README.md#cursor-rules--templates)** - .NET API development patterns
- **[Database Rules & Templates](apps/api/README.md#database-rules--templates)** - Liquibase and database patterns
- **[Web Rules](apps/web/Orchestrator.Web/README.md#cursor-rules)** - Next.js and React development patterns

Each component maintains its own specific rules and code generation templates for consistent development patterns.

## 🔌 MCP Servers

The project integrates with several Model Context Protocol (MCP) servers to enhance development capabilities through AI-powered tools and integrations.

### 🚀 LaunchDarkly MCP Server
- **Package**: `@launchdarkly/mcp-server`
- **Purpose**: Feature flag management and configuration
- **Capabilities**:
  - Create and manage feature flags programmatically
  - Configure targeting rules and rollout strategies
  - Manage AI configurations for LLM experimentation
  - List and update feature flag variations
  - Delete deprecated feature flags
- **Authentication**: Requires LaunchDarkly API key
- **Use Cases**:
  - A/B testing for new features
  - Gradual feature rollouts
  - AI model configuration management
  - Environment-specific feature control

### ✨ Magic MCP (21st.dev)
- **Package**: `@21st-dev/magic@latest`
- **Purpose**: UI component generation and design assistance
- **Capabilities**:
  - Generate React/Next.js components from descriptions
  - Search and retrieve UI component inspiration
  - Refine and improve existing UI components
  - Logo search and integration
  - Component library integration
- **Authentication**: Requires 21st.dev API key
- **Use Cases**:
  - Rapid UI prototyping
  - Component library expansion
  - Design system consistency
  - Logo and branding integration

### 📚 Context7
- **Type**: Remote MCP server
- **URL**: `https://mcp.context7.com/mcp`
- **Purpose**: Documentation and library reference
- **Capabilities**:
  - Resolve library names to Context7-compatible IDs
  - Fetch up-to-date documentation for libraries
  - Provide context-aware code examples
  - Library version-specific documentation
- **Authentication**: No API key required
- **Use Cases**:
  - Quick library documentation lookup
  - API reference during development
  - Version-specific implementation guidance
  - Code example generation

### 🔧 Configuration

MCP servers are configured in `.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "LaunchDarkly": {
      "command": "npx",
      "args": [
        "-y", "--package", "@launchdarkly/mcp-server", "--", "mcp", "start",
        "--api-key", "your-launchdarkly-api-key"
      ]
    },
    "Magic MCP": {
      "command": "npx",
      "args": [
        "-y", "@21st-dev/magic@latest",
        "API_KEY=\"your-21st-dev-api-key\""
      ]
    },
    "context7": {
      "url": "https://mcp.context7.com/mcp"
    }
  }
}
```

### 🔑 Setup Instructions

1. **Copy the template**:
   ```bash
   cp .cursor/mcp.json.template .cursor/mcp.json
   ```

2. **Add your API keys**:
   - **LaunchDarkly**: Replace `<fill in>` with your LaunchDarkly API key
   - **Magic MCP**: Replace `<fill in>` with your 21st.dev API key
   - **Context7**: No setup required (remote server)

3. **Restart Cursor** to load the MCP servers

### 🎯 Integration Benefits

- **Feature Management**: Control feature rollouts and A/B testing through LaunchDarkly
- **Rapid Development**: Generate UI components quickly with Magic MCP
- **Documentation Access**: Instant access to library documentation via Context7
- **AI-Enhanced Workflow**: Leverage AI tools for code generation and improvement
- **Consistent Patterns**: Maintain design system consistency across components

## 📋 Requirements

- **macOS** (for Homebrew packages)
- **Node.js 18+** (for Next.js 15 compatibility)
- **.NET 9 SDK**
- **PostgreSQL 17**
- **Liquibase** (for database migrations)

---

**Quick Navigation:**
- [API Documentation →](apps/api/README.md)
- [Web Documentation →](apps/web/Orchestrator.Web/README.md)
- [GitHub Actions →](.github/workflows/build.yml) 