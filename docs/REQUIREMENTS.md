# Orchestrator Platform — Requirements Document

> A comprehensive specification for building the Orchestrator platform from scratch.
> This document describes **what** the system does and **how** it should be structured,
> without providing the actual source code.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Technology Stack](#2-technology-stack)
3. [Architecture](#3-architecture)
4. [Database Schema](#4-database-schema)
5. [Domain Layer](#5-domain-layer)
6. [API Layer](#6-api-layer)
7. [Frontend (Web Layer)](#7-frontend-web-layer)
8. [Authentication & Authorization](#8-authentication--authorization)
9. [External Service Integrations](#9-external-service-integrations)
10. [Testing Strategy](#10-testing-strategy)
11. [Build, Run & CI/CD](#11-build-run--cicd)
12. [Secondary Application: Hireology Test ATS](#12-secondary-application-hireology-test-ats)

---

## 1. Project Overview

### What Is It?

The **Orchestrator** (branded "Surrova") is a **multi-tenant AI interview platform**. It allows organizations to:

- Create and customize **AI interviewer agents** with configurable voices and personalities
- Define **interview guides** (structured question sets with scoring criteria)
- Combine agents and guides into **interview configurations** for specific job positions
- Conduct **voice-based AI interviews** where candidates speak with an AI agent in real time
- Automatically **score and analyze** interview responses
- Integrate with external **Applicant Tracking Systems (ATS)** to sync jobs, applicants, and results

### Core User Flows

1. **Admin Flow**: An admin creates an AI agent, builds an interview guide with questions, links them in an interview configuration, and assigns it to a job/applicant pairing.
2. **Candidate Flow**: A candidate receives an invite link (short code), redeems it to start a session, conducts a voice interview with the AI agent, and their responses are recorded and scored.
3. **ATS Integration Flow**: An external ATS pushes jobs and applicants via API, creates interviews, and retrieves results via webhooks or polling.

### Multi-Tenancy Model

- A **Group** is the top-level tenant (represents an ATS customer).
- Each Group has an API key for server-to-server communication.
- Within a Group, entities are further scoped by **Organization ID** (an external ATS concept — there is no local organizations table).
- Users can have access to multiple organizations within a Group.
- **Superadmins** and **Group Admins** can see all organizations in their Group.

---

## 2. Technology Stack

### Backend
| Component | Technology | Version |
|---|---|---|
| Runtime | .NET | 9.0 |
| Web Framework | ASP.NET Core Web API | (comes with .NET 9) |
| ORM / Data Access | Dapper | 2.1.x |
| Database | PostgreSQL | 17 |
| Migrations | Liquibase | 4.32.x |
| Validation | FluentValidation | 11.3.x |
| OpenAPI | Swashbuckle (Swagger) | 8.1.x |
| Auth | Auth0 JWT Bearer | via Microsoft.AspNetCore.Authentication.JwtBearer |
| File Storage | Azure Blob Storage | Azure.Storage.Blobs 12.x |
| Testing | MSTest | 3.x |

### Frontend
| Component | Technology | Version |
|---|---|---|
| Framework | Next.js (App Router) | 15.x |
| UI Library | React | 18.x |
| Language | TypeScript | 5.x |
| Styling | Tailwind CSS | 3.x |
| Component Library | Radix UI | (multiple packages) |
| Forms | React Hook Form + Zod | 7.x / 3.x |
| Auth | @auth0/nextjs-auth0 | 4.x |
| Animations | Framer Motion | 12.x |
| Icons | Lucide React | latest |
| Toasts | Sonner | 1.x |

### Infrastructure & Tooling
| Component | Technology |
|---|---|
| Build Automation | `just` (justfile command runner) |
| CI/CD | GitHub Actions |
| Liquibase Driver | PostgreSQL JDBC 42.x |
| Java (for Liquibase) | Java 17 |

---

## 3. Architecture

### Clean Architecture Layers

```
┌──────────────────────────────────────────────┐
│  Orchestrator.Web         (Next.js Frontend) │  ← Communicates via HTTP only
├──────────────────────────────────────────────┤
│  Orchestrator.Api         (ASP.NET Core API) │  ← Controllers, Middleware, Mappers
├──────────────────────────────────────────────┤
│  Orchestrator.DomainLayer (Business Logic)   │  ← Zero project references (self-contained)
├──────────────────────────────────────────────┤
│  Orchestrator.Database    (Liquibase)        │  ← Schema migrations only
├──────────────────────────────────────────────┤
│  Orchestrator.AcceptanceTests (MSTest)       │  ← References DomainLayer only
└──────────────────────────────────────────────┘
```

### Dependency Rules (Strict)

| Project | Can Reference |
|---|---|
| `DomainLayer` | Nothing (completely self-contained; uses Npgsql + Dapper internally) |
| `Api` | `DomainLayer` only |
| `AcceptanceTests` | `DomainLayer` only (via `InternalsVisibleTo`) |
| `Web` | Nothing (HTTP calls to `Api` only) |
| `Database` | Nothing (standalone Liquibase project) |

### Key Architecture Patterns

1. **Facade Pattern**: `DomainFacade` is the single public entry point to all business logic. `DataFacade` is the single internal entry point to all data access. `GatewayFacade` is the single internal entry point to all external services.
2. **Partial Classes**: Each facade is split into feature-specific partial class files (e.g., `DomainFacade.Interview.cs`, `DomainFacade.Agent.cs`).
3. **ServiceLocator**: The Domain layer uses a `ServiceLocator` (not an external DI container) to resolve dependencies. An abstract `ServiceLocatorBase` is subclassed for production (`ServiceLocator`) and testing (`ServiceLocatorForAcceptanceTesting`).
4. **Manager Pattern**: Business logic lives in `Manager` classes (e.g., `InterviewManager`, `AgentManager`). Managers are lazily instantiated inside the `DomainFacade`.
5. **DataManager Pattern**: Each entity has a `DataManager` class for raw SQL data access via Dapper.
6. **Result Pattern**: Operations can return results; validation failures throw typed exceptions rather than returning error results.
7. **Soft Delete**: Most entities support soft delete (`is_deleted`, `deleted_at`, `deleted_by`).
8. **Pagination**: All search/list operations return `PaginatedResult<T>`.
9. **InternalsVisibleTo**: The DomainLayer exposes internals to AcceptanceTests so custom test doubles can be built without external mocking frameworks.

### Data Flow

```
Create: HTTP Request → Controller → Mapper → DomainFacade → Manager → Validator → DataFacade → DataManager → PostgreSQL
Read:   HTTP Request → Controller → DomainFacade → Manager → DataFacade → DataManager → PostgreSQL → Mapper → Response
```

---

## 4. Database Schema

### Convention: All Tables Follow These Patterns

**Audit Fields** (on nearly every table):
- `created_at` TIMESTAMP NOT NULL DEFAULT now()
- `updated_at` TIMESTAMP (nullable)
- `created_by` VARCHAR(100) (nullable)
- `updated_by` VARCHAR(100) (nullable)

**Soft Delete Fields** (on most tables):
- `is_deleted` BOOLEAN NOT NULL DEFAULT false
- `deleted_at` TIMESTAMP (nullable)
- `deleted_by` VARCHAR(100) (nullable)

**Primary Keys**: UUID (`id`) on all tables, generated as `gen_random_uuid()`.

**Migration Naming**: `YYYYMMDDHHMMSS-DescriptionInPascalCase.xml` (Liquibase XML format).

---

### 4.1 `groups`

The top-level multi-tenant entity.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK, default gen_random_uuid() |
| `name` | VARCHAR(255) | NOT NULL |
| `api_key` | VARCHAR(255) | UNIQUE |
| `external_group_id` | VARCHAR(255) | (nullable) |
| `ats_base_url` | VARCHAR(500) | (nullable) |
| `webhook_url` | VARCHAR(500) | (nullable) |
| + audit fields | | |
| + soft delete fields | | |

---

### 4.2 `users`

System users authenticated via Auth0.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `email` | VARCHAR(255) | NOT NULL, UNIQUE |
| `auth0_sub` | VARCHAR(255) | UNIQUE |
| `first_name` | VARCHAR(100) | |
| `last_name` | VARCHAR(100) | |
| `phone` | VARCHAR(50) | |
| `profile_image_url` | VARCHAR(500) | |
| `external_user_id` | VARCHAR(255) | |
| + audit fields | | |
| + soft delete fields | | |

---

### 4.3 `agents`

AI interviewer personas with voice configuration.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `group_id` | UUID | FK → groups, NOT NULL |
| `organization_id` | VARCHAR(255) | (nullable, external ATS org ref) |
| `display_name` | VARCHAR(255) | NOT NULL |
| `system_prompt` | TEXT | |
| `interview_guidelines` | TEXT | |
| `profile_image_url` | VARCHAR(500) | |
| `elevenlabs_voice_id` | VARCHAR(255) | |
| `voice_stability` | DECIMAL | |
| `voice_similarity_boost` | DECIMAL | |
| `voice_provider` | VARCHAR(50) | |
| `voice_type` | VARCHAR(50) | |
| `voice_name` | VARCHAR(255) | |
| `visibility_scope` | VARCHAR(50) | DEFAULT 'owner_only' |
| + audit fields | | |
| + soft delete fields | | |

---

### 4.4 `jobs`

Jobs synced from an external ATS.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `group_id` | UUID | FK → groups, NOT NULL |
| `organization_id` | VARCHAR(255) | |
| `external_job_id` | VARCHAR(255) | |
| `title` | VARCHAR(255) | NOT NULL |
| `description` | TEXT | |
| `status` | VARCHAR(50) | |
| `location` | VARCHAR(255) | |
| + audit fields | | |
| + soft delete fields | | |

**Unique constraint**: `(group_id, external_job_id)`

---

### 4.5 `applicants`

Applicants synced from an external ATS.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `group_id` | UUID | FK → groups, NOT NULL |
| `organization_id` | VARCHAR(255) | |
| `external_applicant_id` | VARCHAR(255) | |
| `first_name` | VARCHAR(100) | |
| `last_name` | VARCHAR(100) | |
| `email` | VARCHAR(255) | |
| `phone` | VARCHAR(50) | |
| + audit fields | | |
| + soft delete fields | | |

**Unique constraint**: `(group_id, external_applicant_id)`

---

### 4.6 `interview_guides`

Reusable question sets for interviews.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `group_id` | UUID | FK → groups, NOT NULL |
| `organization_id` | VARCHAR(255) | |
| `name` | VARCHAR(255) | NOT NULL |
| `description` | TEXT | |
| `opening_template` | TEXT | |
| `closing_template` | TEXT | |
| `visibility_scope` | VARCHAR(50) | DEFAULT 'owner_only' |
| `is_active` | BOOLEAN | DEFAULT true |
| + audit fields | | |
| + soft delete fields | | |

---

### 4.7 `interview_guide_questions`

Individual questions belonging to a guide.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `interview_guide_id` | UUID | FK → interview_guides (CASCADE DELETE) |
| `question` | TEXT | NOT NULL |
| `display_order` | INT | NOT NULL |
| `scoring_weight` | DECIMAL | |
| `scoring_guidance` | TEXT | |
| `follow_ups_enabled` | BOOLEAN | DEFAULT false |
| `max_follow_ups` | INT | |
| + audit fields | | |

*(No soft delete on questions — they cascade delete with the guide.)*

---

### 4.8 `interview_configurations`

Links an agent + interview guide for a specific use case.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `group_id` | UUID | FK → groups, NOT NULL |
| `organization_id` | VARCHAR(255) | |
| `agent_id` | UUID | FK → agents |
| `interview_guide_id` | UUID | FK → interview_guides |
| `name` | VARCHAR(255) | NOT NULL |
| `description` | TEXT | |
| `is_active` | BOOLEAN | DEFAULT true |
| + audit fields | | |
| + soft delete fields | | |

---

### 4.9 `interviews`

A single interview session between a candidate and an AI agent.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `job_id` | UUID | FK → jobs |
| `applicant_id` | UUID | FK → applicants |
| `agent_id` | UUID | FK → agents |
| `interview_configuration_id` | UUID | FK → interview_configurations (nullable) |
| `group_id` | UUID | FK → groups |
| `token` | VARCHAR(255) | UNIQUE |
| `status` | VARCHAR(50) | (e.g., pending, in_progress, completed) |
| `interview_type` | VARCHAR(50) | DEFAULT 'voice' |
| `scheduled_at` | TIMESTAMP | |
| `started_at` | TIMESTAMP | |
| `completed_at` | TIMESTAMP | |
| `current_question_index` | INT | |
| + audit fields | | |
| + soft delete fields | | |

---

### 4.10 `interview_responses`

Individual candidate responses to interview questions.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `interview_id` | UUID | FK → interviews |
| `question_id` | UUID | (nullable, links to interview_guide_questions for structured interviews) |
| `response_text` | TEXT | |
| `audio_url` | VARCHAR(500) | |
| `question_type` | VARCHAR(50) | |
| `follow_up_template_id` | UUID | FK → follow_up_templates (nullable) |
| + audit fields | | |
| + soft delete fields | | |

---

### 4.11 `interview_results`

AI-generated scoring and analysis of a completed interview.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `interview_id` | UUID | FK → interviews |
| `overall_score` | DECIMAL | |
| `transcript` | TEXT | |
| `analysis` | TEXT | |
| `question_scores` | JSONB | (per-question scoring breakdown) |
| + audit fields | | |

---

### 4.12 `interview_invites`

Short-code invitations sent to candidates.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `group_id` | UUID | FK → groups |
| `interview_id` | UUID | FK → interviews |
| `invite_code` | VARCHAR(50) | UNIQUE |
| `expires_at` | TIMESTAMP | |
| `sent_at` | TIMESTAMP | |
| + audit fields | | |

---

### 4.13 `candidate_sessions`

JWT-based sessions for candidates conducting interviews.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `invite_id` | UUID | FK → interview_invites |
| `session_token` | VARCHAR(500) | UNIQUE |
| `started_at` | TIMESTAMP | |
| `last_activity_at` | TIMESTAMP | |
| + audit fields | | |

---

### 4.14 `follow_up_templates`

AI-suggested follow-up questions linked to guide questions.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `interview_guide_question_id` | UUID | FK → interview_guide_questions |
| `interview_configuration_question_id` | UUID | (nullable) |
| `template_text` | TEXT | |
| + audit fields | | |

---

### 4.15 `follow_up_selection_logs`

Tracks which follow-up templates were actually used during interviews.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `interview_response_id` | UUID | FK → interview_responses |
| `follow_up_template_id` | UUID | FK → follow_up_templates |
| `selected_at` | TIMESTAMP | |

---

### 4.16 `interview_audit_logs`

Event-sourced audit trail of interview lifecycle events.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `interview_id` | UUID | FK → interviews |
| `event_type` | VARCHAR(100) | |
| `event_data` | JSONB | |
| `created_at` | TIMESTAMP | |

---

### 4.17 `stock_voices` (seed data table)

Curated voice options for agents.

| Column | Type | Constraints |
|---|---|---|
| `id` | UUID | PK |
| `voice_provider` | VARCHAR(50) | |
| `voice_type` | VARCHAR(50) | |
| `voice_id` | VARCHAR(255) | |
| `voice_name` | VARCHAR(255) | |
| `description` | TEXT | |

---

### 4.18 `webhook_configs` / `webhook_deliveries`

Webhook configuration and delivery tracking per group.

---

## 5. Domain Layer

### 5.1 Structure

```
Orchestrator.DomainLayer/
├── Common/
│   ├── PaginatedResult.cs          # Generic paginated result wrapper
│   └── ConversationTurn.cs         # Voice conversation history model
├── DomainFacade.cs                 # Base partial class (constructor, manager setup)
├── DomainFacade.{Feature}.cs       # One partial file per feature area
├── Managers/
│   ├── Models/                     # Domain entity models
│   │   ├── Entity.cs               # Base class (Id, audit, soft delete)
│   │   ├── Agent.cs
│   │   ├── Group.cs
│   │   ├── Interview.cs
│   │   ├── InterviewGuide.cs
│   │   ├── InterviewGuideQuestion.cs
│   │   ├── InterviewConfiguration.cs
│   │   ├── InterviewInvite.cs
│   │   ├── InterviewResponse.cs
│   │   ├── InterviewResult.cs
│   │   ├── CandidateSession.cs
│   │   ├── FollowUpTemplate.cs
│   │   ├── Job.cs
│   │   ├── Applicant.cs
│   │   ├── User.cs
│   │   ├── StockVoice.cs
│   │   └── OrphanedEntitySummary.cs
│   ├── Validators/                 # One validator per entity
│   │   ├── AgentValidator.cs
│   │   ├── JobValidator.cs
│   │   ├── ApplicantValidator.cs
│   │   ├── InterviewValidator.cs
│   │   ├── GroupValidator.cs
│   │   ├── InterviewConfigurationValidator.cs
│   │   ├── InterviewGuideValidator.cs
│   │   ├── InterviewInviteValidator.cs
│   │   ├── UserValidator.cs
│   │   └── ImageValidator.cs
│   ├── Exceptions/                 # Structured exception hierarchy
│   │   ├── BaseException.cs
│   │   ├── BusinessBaseException.cs
│   │   ├── NotFoundBaseException.cs
│   │   ├── TechnicalBaseException.cs
│   │   ├── GatewayConnectionException.cs
│   │   ├── GatewayApiException.cs
│   │   └── GatewayConfigurationException.cs
│   ├── {Feature}Manager.cs         # One manager per feature area
│   ├── DataLayer/
│   │   ├── DataFacade.cs           # Base partial class
│   │   ├── DataFacade.{Feature}.cs # One partial file per feature
│   │   └── DataManagers/           # Raw SQL via Dapper
│   │       ├── {Entity}DataManager.cs
│   │       └── ...
│   ├── GatewayLayer/
│   │   ├── GatewayFacade.cs        # Base partial class
│   │   ├── GatewayFacade.OpenAI.cs
│   │   ├── GatewayFacade.ElevenLabs.cs
│   │   ├── GatewayFacade.Ats.cs
│   │   └── GatewayManagers/
│   │       ├── OpenAIManager.cs
│   │       ├── ElevenLabsManager.cs
│   │       └── AtsGatewayManager.cs
│   └── ServiceLocators/
│       ├── ServiceLocatorBase.cs    # Abstract base
│       ├── ServiceLocator.cs        # Production implementation
│       └── ConfigurationProvider.cs # Reads env vars / config
└── Properties/
    └── AssemblyInfo.cs              # [InternalsVisibleTo("Orchestrator.AcceptanceTests")]
```

### 5.2 Entity Base Class

All domain models inherit from `Entity`:

```
Entity
├── Id : Guid
├── CreatedAt : DateTime
├── UpdatedAt : DateTime?
├── CreatedBy : string?
├── UpdatedBy : string?
├── IsDeleted : bool
├── DeletedAt : DateTime?
└── DeletedBy : string?
```

### 5.3 DomainFacade Methods (Complete Catalog)

#### User Operations
- `SearchUsers(searchTerm, pageNumber, pageSize, groupId)` → `PaginatedResult<User>`
- `CreateUser(user)` → `User`
- `GetUserById(id)` → `User?`
- `GetUserByAuth0Sub(auth0Sub)` → `User?`
- `UpdateUser(user)` → `User`
- `DeleteUser(id)`

#### Agent Operations
- `CreateAgent(agent)` → `Agent`
- `GetAgentById(id)` → `Agent?`
- `SearchAgents(searchTerm, pageNumber, pageSize, groupId, organizationIds)` → `PaginatedResult<Agent>`
- `UpdateAgent(agent)` → `Agent`
- `DeleteAgent(id)`
- `SelectAgentVoiceAsync(agentId, voiceProvider, voiceType, voiceId, voiceName, stability, similarityBoost)`

#### Group Operations
- `CreateGroup(group)` → `Group`
- `GetGroupById(id)` → `Group?`
- `GetGroupByExternalId(externalGroupId)` → `Group?`
- `GetGroupByApiKey(apiKey)` → `Group?`
- `SearchGroups(searchTerm, pageNumber, pageSize)` → `PaginatedResult<Group>`
- `UpdateGroup(group)` → `Group`
- `DeleteGroup(id)`
- `CreateOrUpdateGroupFromAts(group)` → `Group` (upsert by external ID)

#### Job Operations
- `CreateJob(job)` → `Job`
- `CreateOrUpdateJobFromAts(job, groupId)` → `Job` (upsert by external job ID)
- `GetJobById(id)` → `Job?`
- `GetJobByExternalId(externalJobId, groupId)` → `Job?`
- `SearchJobs(searchTerm, pageNumber, pageSize, groupId, organizationIds)` → `PaginatedResult<Job>`
- `DeleteJobByExternalId(externalJobId, groupId)`

#### Applicant Operations
- `CreateApplicant(applicant)` → `Applicant`
- `CreateOrUpdateApplicantFromAts(applicant, groupId)` → `Applicant` (upsert)
- `GetApplicantById(id)` → `Applicant?`
- `GetApplicantByExternalId(externalApplicantId, groupId)` → `Applicant?`
- `SearchApplicants(searchTerm, pageNumber, pageSize, groupId, organizationIds)` → `PaginatedResult<Applicant>`

#### Interview Guide Operations
- `CreateInterviewGuide(guide)` → `InterviewGuide`
- `GetInterviewGuideById(id, includeQuestions)` → `InterviewGuide?`
- `SearchInterviewGuides(searchTerm, pageNumber, pageSize, groupId, organizationIds)` → `PaginatedResult<InterviewGuide>`
- `UpdateInterviewGuide(guide)` → `InterviewGuide`
- `UpdateInterviewGuideWithQuestions(guide, questions)` → `InterviewGuide`
- `DeleteInterviewGuide(id)`
- `GetInterviewGuideQuestions(guideId)` → `List<InterviewGuideQuestion>`
- `AddInterviewGuideQuestion(question)` → `InterviewGuideQuestion`
- `DeleteInterviewGuideQuestion(guideId, questionId)`

#### Interview Configuration Operations
- `CreateInterviewConfiguration(config)` → `InterviewConfiguration`
- `GetInterviewConfigurationById(id)` → `InterviewConfiguration?`
- `SearchInterviewConfigurations(searchTerm, pageNumber, pageSize, groupId, organizationIds)` → `PaginatedResult<InterviewConfiguration>`
- `UpdateInterviewConfiguration(config)` → `InterviewConfiguration`
- `DeleteInterviewConfiguration(id)`

#### Interview Lifecycle Operations
- `CreateInterview(interview)` → `Interview`
- `CreateTestInterview(agentId, groupId)` → `Interview` (creates a temporary test interview)
- `GetInterviewById(id)` → `Interview?`
- `GetInterviewByToken(token)` → `Interview?`
- `SearchInterviews(searchTerm, pageNumber, pageSize, groupId, organizationIds)` → `PaginatedResult<Interview>`
- `StartInterview(id)` → `Interview` (sets status to in_progress, records started_at)
- `CompleteInterview(id)` → `Interview` (sets status to completed, records completed_at)
- `UpdateInterview(interview)` → `Interview`
- `DeleteInterview(id)`
- `ScoreTestInterview(interviewId)` → `InterviewResult`

#### Interview Response Operations
- `AddInterviewResponse(response)` → `InterviewResponse`
- `GetInterviewResponsesByInterviewId(interviewId)` → `List<InterviewResponse>`

#### Interview Result Operations
- `CreateInterviewResult(result)` → `InterviewResult`
- `GetInterviewResultByInterviewId(interviewId)` → `InterviewResult?`

#### Interview Invite Operations
- `CreateInterviewInvite(invite)` → `InterviewInvite`
- `GetInterviewInviteByCode(code)` → `InterviewInvite?`
- `RefreshInterviewInvite(interviewId)` → `InterviewInvite`

#### Candidate Session Operations
- `RedeemInviteAndCreateSession(inviteCode)` → `CandidateSession` (validates invite, creates JWT session)

#### Follow-Up Question Operations
- `GenerateFollowUpSuggestions(questionId, configurationId)` → `List<FollowUpTemplate>` (AI-generated)
- `ApproveFollowUps(questionId, templateIds)`
- `GetFollowUpTemplatesByQuestionId(questionId)` → `List<FollowUpTemplate>`
- `SelectAndReturnFollowUp(interviewId, questionId, responseText)` → `FollowUpTemplate?` (AI selects best follow-up)

#### Voice & Audio Operations (Async)
- `GetAvailableVoicesAsync()` → `List<Voice>` (from ElevenLabs)
- `GetStockVoicesAsync()` → `List<StockVoice>` (curated subset)
- `PreviewVoiceAsync(voiceId, text)` → `Stream` (audio stream)
- `PreviewAgentVoiceAsync(agentId, text)` → `Stream`
- `StreamVoiceAsync(agentId, text)` → `Stream` (chunked streaming)
- `WarmupInterviewAudioAsync(interviewId)` (pre-cache opening audio)
- `IsVoiceEnabled()` → `bool`

#### Conversation Operations (Async)
- `StreamAudioResponseAsync(interviewId, conversationHistory, userMessage)` → `Stream` (real-time voice AI)
- `GenerateChatResponse(conversationHistory, systemPrompt)` → `string` (widget chatbot)

#### ATS Gateway Operations
- `GetUserAccessFromAts(auth0Sub, groupExternalId)` → user permissions object
- `GetOrganizationsFromAts(groupId)` → list of organizations

#### Image Operations
- `UploadImage(stream, fileName, contentType)` → `string` (returns URL/key)
- `GetImage(key)` → `Stream`

#### Orphan Detection
- `GetOrphanedEntitySummary(groupId)` → `OrphanedEntitySummary`

### 5.4 Exception Hierarchy

```
BaseException (abstract)
├── BusinessBaseException          → HTTP 400 (user-facing message via Reason property)
│   ├── NotFoundBaseException      → HTTP 404
│   ├── AgentValidationException
│   ├── JobValidationException
│   ├── ApplicantValidationException
│   ├── InterviewValidationException
│   ├── GroupValidationException
│   ├── InterviewConfigurationValidationException
│   ├── InterviewGuideValidationException
│   ├── InterviewInviteValidationException
│   ├── UserValidationException
│   ├── ImageValidationException
│   └── ElevenLabsDisabledException → HTTP 503
└── TechnicalBaseException         → HTTP 500 (generic message; details hidden)
    ├── GatewayConnectionException
    ├── GatewayApiException
    └── GatewayConfigurationException
```

### 5.5 Validation Pattern

Each entity has a dedicated `Validator` class that:
- Validates required fields, string lengths, format constraints
- Throws a typed `{Entity}ValidationException` (extends `BusinessBaseException`) on failure
- Is called by the Manager before any create/update operation
- The exception's `Reason` property contains a user-friendly message

### 5.6 PaginatedResult<T>

```
PaginatedResult<T>
├── Items : IEnumerable<T>
├── TotalCount : int
├── PageNumber : int
├── PageSize : int
├── TotalPages : int (calculated)
├── HasPreviousPage : bool (calculated)
├── HasNextPage : bool (calculated)
└── static Empty() → PaginatedResult<T>
```

---

## 6. API Layer

### 6.1 Middleware Pipeline (Order Matters)

1. **Swagger** (development only)
2. **PlatformExceptionHandlingMiddleware** — catches all exceptions, translates to structured error responses
3. **Request Logging Middleware** (development only)
4. **HTTPS Redirection** (production only)
5. **CORS** — configured for `http://localhost:3000`
6. **Static Files**
7. **ApiKeyAuthMiddleware** — for `/api/v1/ats/*` routes; validates `X-API-Key` or `X-Bootstrap-Secret` header
8. **CandidateSessionMiddleware** — for `/api/v1/candidate/*` routes; validates candidate JWT from cookie or Bearer header
9. **JWT Authentication** (Auth0)
10. **Authorization**
11. **UserContextMiddleware** — resolves authenticated user's group, organizations, and admin status; caches for 30 seconds
12. **Controllers**

### 6.2 Error Response Format

```json
{
  "statusCode": 400,
  "message": "User-friendly error message",
  "exceptionType": "AgentValidationException",
  "isBusinessException": true,
  "isTechnicalException": false,
  "timestamp": "2026-02-16T12:00:00Z"
}
```

- Business exceptions → expose the `Reason` message to the user
- Technical exceptions → return generic "Something went wrong" message (never expose internals)

### 6.3 Controller & Endpoint Catalog

#### AdminController — `GET /api/v1/admin`
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/orphaned-entities` | JWT (group admin) | Get orphaned entity summary |

#### AgentController — `/api/v1/Agent`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/` | JWT | Create agent |
| GET | `/{id}` | JWT | Get agent by ID |
| GET | `/` | JWT | Search agents (paginated) |
| PUT | `/{id}` | JWT | Update agent |
| DELETE | `/{id}` | JWT | Delete agent (soft) |
| POST | `/{id}/voice/select` | JWT | Select voice for agent |
| POST | `/{id}/voice/test` | Public | Test/preview agent voice (streams audio) |
| POST | `/{id}/voice/stream` | Public | Stream agent voice (chunked audio) |

#### AtsController — `/api/v1/ats`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/groups` | API Key or Bootstrap Secret | Create/update group |
| GET | `/settings/webhook` | API Key | Get webhook URL |
| PUT | `/settings/webhook` | API Key | Update webhook URL |
| POST | `/jobs` | API Key | Create/update job |
| GET | `/jobs/{externalJobId}` | API Key | Get job by external ID |
| GET | `/jobs` | API Key | Search jobs |
| DELETE | `/jobs/{externalJobId}` | API Key | Delete job |
| POST | `/applicants` | API Key | Create/update applicant |
| GET | `/applicants/{externalApplicantId}` | API Key | Get applicant |
| GET | `/applicants` | API Key | Search applicants |
| GET | `/agents` | API Key | List agents for group |
| GET | `/configurations` | API Key | List interview configurations |
| POST | `/interviews` | API Key | Create interview |
| GET | `/interviews/{id}` | API Key | Get interview with invite status |
| POST | `/interviews/{id}/refresh-invite` | API Key | Refresh invite |
| GET | `/interviews/{id}/result` | API Key | Get interview result |
| GET | `/interviews/{id}/responses` | API Key | Get interview responses |

#### CandidateController — `/api/v1/candidate`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/sessions` | Public | Redeem invite code, create session |
| GET | `/interview` | Candidate Session | Get current interview details |
| POST | `/interview/start` | Candidate Session | Start the interview |
| POST | `/interview/responses` | Candidate Session | Submit a response |
| POST | `/interview/complete` | Candidate Session | Complete the interview |
| POST | `/interview/audio/upload` | Candidate Session | Upload audio (50MB limit) |
| POST | `/interview/audio/warmup` | Candidate Session | Pre-cache interview audio |

#### ConversationController — `/api/v1/Conversation`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/respond/audio` | JWT or Candidate | Stream audio AI response (chunked) |
| GET | `/status` | JWT or Candidate | Check voice service availability |

#### GroupController — `/api/v1/Group`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/` | JWT | Create group |
| GET | `/{id}` | JWT | Get group |
| GET | `/by-external-id/{externalGroupId}` | JWT | Get group by external ID |
| GET | `/` | JWT | Search groups |
| PUT | `/{id}` | JWT | Update group |
| DELETE | `/{id}` | JWT | Delete group |

#### ImageController — `/api/v1/Image`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/upload` | JWT | Upload image (10MB limit) |
| GET | `/images/{key}` | Public | Get image by key |
| GET | `/interview-audio/{key}` | JWT | Get interview audio |

#### InterviewController — `/api/v1/Interview`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/` | JWT | Create interview |
| GET | `/{id}` | JWT | Get interview detail |
| GET | `/by-token/{token}` | Public | Get interview by token |
| GET | `/` | JWT | Search interviews |
| POST | `/by-token/{token}/start` | Public | Start interview by token |
| POST | `/by-token/{token}/complete` | Public | Complete interview by token |
| POST | `/by-token/{token}/responses` | Public | Add response by token |
| GET | `/{id}/responses` | JWT | Get responses |
| POST | `/{id}/responses` | JWT | Add response by ID |
| POST | `/{id}/result` | JWT | Create/update result |
| GET | `/{id}/result` | JWT | Get result |
| DELETE | `/{id}` | JWT | Delete interview |
| POST | `/test` | JWT | Create test interview |
| POST | `/test/{id}/score` | JWT | Score test interview |
| POST | `/{id}/audio/warmup` | JWT | Warmup audio |
| POST | `/by-token/{token}/audio/warmup` | Public | Warmup audio by token |

#### InterviewConfigurationController — `/api/v1/InterviewConfiguration`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/` | JWT | Create configuration |
| GET | `/{id}` | JWT | Get configuration |
| GET | `/` | JWT | Search configurations |
| PUT | `/{id}` | JWT | Update configuration |
| DELETE | `/{id}` | JWT | Delete configuration |

#### InterviewGuideController — `/api/v1/InterviewGuide`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/` | JWT | Create guide |
| GET | `/{id}` | JWT | Get guide (optionally with questions) |
| GET | `/` | JWT | Search guides |
| PUT | `/{id}` | JWT | Update guide |
| DELETE | `/{id}` | JWT | Delete guide |
| GET | `/{id}/questions` | JWT | Get guide questions |
| POST | `/{id}/questions` | JWT | Add question |
| DELETE | `/{id}/questions/{questionId}` | JWT | Delete question |

#### InterviewQuestionController — `/api/v1/interview-questions`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/{id}/follow-ups/generate` | JWT | Generate AI follow-up suggestions |
| POST | `/{id}/follow-ups/approve` | JWT | Approve follow-ups |
| GET | `/{id}/follow-ups` | JWT | Get follow-up templates |
| PUT | `/{id}/follow-ups/{templateId}` | JWT | Update follow-up |
| DELETE | `/{id}/follow-ups/{templateId}` | JWT | Delete follow-up |

#### JobController — `/api/v1/Job`
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/` | JWT | Search jobs |

#### UserController — `/api/v1/User`
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/me-context` | JWT | Get current user's context |
| POST | `/` | Public | Create user |
| GET | `/{id}` | JWT | Get user |
| GET | `/by-auth0-sub/{auth0Sub}` | Public | Get user by Auth0 sub |
| GET | `/` | JWT | Search users |
| PUT | `/{id}` | JWT | Update user |
| DELETE | `/{id}` | JWT | Delete user |

#### VoiceController — `/api/v1/Voice`
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/elevenlabs` | JWT | List available ElevenLabs voices |
| GET | `/elevenlabs/stock` | JWT | List curated stock voices |
| POST | `/preview` | JWT | Preview a voice (streams audio) |

### 6.4 Resource Model Convention

For each entity, the API defines separate models:
- `{Entity}Resource` — response model (GET output, POST/PUT output)
- `Create{Entity}Resource` — create request body
- `Update{Entity}Resource` — update request body (fields are nullable for partial updates)
- `Search{Entity}Request` — query parameters (extends `PaginatedRequest` with `PageNumber`, `PageSize`, `SearchTerm`)

### 6.5 Mapper Convention

Static mapper classes with:
- `ToResource(domainModel)` → response model
- `ToResource(IEnumerable<domainModel>)` → collection
- `ToDomain(createResource, ...)` → domain model for creation
- `ToDomain(updateResource, existingDomain)` → domain model for update (preserves existing values for null fields)

### 6.6 Streaming Audio Responses

Audio endpoints (voice preview, conversation) return `audio/mpeg` with chunked transfer encoding. The API streams bytes directly from the ElevenLabs TTS API to the client.

### 6.7 Organization-Based Filtering

For search endpoints, the controller reads the `UserContext` from `HttpContext.Items`:
- **Superadmins / Group Admins**: See all organizations in their group
- **Regular users**: Filtered to only their `AccessibleOrganizationIds`
- The accessible org IDs are passed to DomainFacade search methods

---

## 7. Frontend (Web Layer)

### 7.1 App Router Structure

```
Orchestrator.Web/
├── app/
│   ├── (app)/                     # Authenticated admin layout
│   │   ├── layout.tsx             # Sidebar + header layout
│   │   ├── page.tsx               # Dashboard
│   │   ├── my-agents/             # Agent management
│   │   │   ├── page.tsx           # Agent list
│   │   │   ├── [id]/edit/         # Edit agent
│   │   │   └── [id]/train/        # Agent training
│   │   ├── create-agent/          # Create new agent
│   │   ├── interview-guides/      # Guide management
│   │   │   ├── page.tsx           # Guide list
│   │   │   ├── [id]/              # View/edit guide
│   │   │   └── new/               # Create guide
│   │   ├── interview-configurations/  # Configuration management
│   │   │   ├── page.tsx
│   │   │   ├── [id]/
│   │   │   └── new/
│   │   ├── interviews/            # Interview management
│   │   │   ├── page.tsx           # Interview list
│   │   │   ├── [id]/              # Interview details + results
│   │   │   └── test/              # Test interview interface
│   │   ├── jobs/                  # Job list (read-only, synced from ATS)
│   │   └── admin/
│   │       └── orphaned-entities/ # Admin tool
│   ├── (candidate)/               # Candidate interview layout
│   │   └── i/[code]/              # Candidate interview experience
│   ├── login/                     # Login page
│   ├── interview/[token]/         # Public interview access
│   └── api/auth/                  # Auth0 route handlers
├── components/
│   ├── ui/                        # Radix UI primitives (button, dialog, input, etc.)
│   └── ...                        # Feature components (header, sidebar, etc.)
├── lib/
│   ├── api-client.ts              # Client-side API client (browser fetch + auth)
│   ├── api-client-server.ts       # Server-side API helpers (server actions)
│   └── use-server-action.ts       # Hook for mutation operations
└── ...
```

### 7.2 API Communication Pattern

**Server Components / Server Actions**:
- Use `apiGet<T>()`, `apiPost<T>()`, `apiPut<T>()`, `apiDelete()` helpers
- Automatically inject Auth0 access token and `X-Group-Id` header
- 401 responses trigger redirect to login
- Errors throw structured `ApiClientError`

**Client Components**:
- Use an `ApiClient` class (or `apiClient` singleton)
- Automatically reads the group ID from a cookie (`orchestrator_group_id`)
- Injects Bearer token from Auth0 session
- 401 responses redirect to login

**Mutation Hook** (`useServerAction`):
- Wraps server actions with loading state, error handling, and toast notifications
- Usage: `const { execute, isLoading, error } = useServerAction(myServerAction, { successMessage: '...' })`

### 7.3 Key Frontend Features

1. **Dashboard**: Quick-access cards to agents, guides, configurations, interviews
2. **Agent Management**: Create/edit agents, configure voice (provider, voice ID, stability, similarity), set system prompt and guidelines, upload profile image
3. **Agent Training**: Configure agent behavior, test voice, preview responses
4. **Interview Guides**: Create question sets with ordering, scoring weights, guidance, opening/closing templates, follow-up configuration
5. **Interview Configurations**: Link an agent + guide together, name and describe the configuration
6. **Interview List**: Search/filter interviews, view status, access results
7. **Interview Detail**: View transcript, responses, AI-generated scores, per-question analysis
8. **Test Interview**: Conduct a test interview with an agent to preview the experience
9. **Candidate Interview Experience** (`/i/[code]`): Full voice interview UI for candidates — voice recording, playback, real-time AI conversation
10. **Job List**: Read-only view of jobs synced from the ATS
11. **Admin Tools**: Orphaned entity detection for data integrity

### 7.4 Multi-Tenant Context

- Group is selected and stored in a cookie (`orchestrator_group_id`)
- The `X-Group-Id` header is sent with every API request
- The API resolves the user's permissions within that group
- Frontend may show a group selector for users with access to multiple groups

### 7.5 Styling & UX

- **Tailwind CSS** for utility-first styling
- **Radix UI** primitives for accessible, unstyled components (dialog, dropdown, tabs, tooltip, etc.)
- **Framer Motion** for animations
- **Sonner** for toast notifications
- **Lucide** icons throughout
- Modern, clean design with responsive layout
- Sidebar navigation with collapsible sections

---

## 8. Authentication & Authorization

### 8.1 Three Auth Mechanisms

| Mechanism | Used By | How It Works |
|---|---|---|
| **Auth0 JWT** | Admin UI users | Standard OIDC JWT Bearer flow; token obtained via Auth0 SDK in frontend |
| **API Key** (`X-API-Key`) | External ATS servers | Header validated against `groups.api_key` column; resolves to group context |
| **Candidate Session JWT** | Interview candidates | Custom JWT issued when invite code is redeemed; stored in cookie (`candidate_session`) |

### 8.2 Auth0 Configuration

- **Domain**: Configured in `appsettings.json`
- **Audience**: `https://api.surrova.com`
- **Frontend SDK**: `@auth0/nextjs-auth0` with rolling sessions
- **Routes**: `/api/auth/login`, `/api/auth/logout`, `/api/auth/callback`

### 8.3 User Context Resolution

After Auth0 authentication, the `UserContextMiddleware`:
1. Extracts the Auth0 `sub` claim from the JWT
2. Reads the `X-Group-Id` header (external group identifier)
3. Resolves the internal Orchestrator group
4. Calls the external ATS gateway to determine the user's organization access
5. Builds a `UserContext` with: `Auth0Sub`, `UserName`, `GroupId`, `IsSuperadmin`, `IsGroupAdmin`, `AccessibleOrganizationIds`
6. Caches the result for 30 seconds

### 8.4 Candidate Session Flow

1. ATS creates an interview and invite via API key auth
2. Candidate opens `/i/{code}` in their browser
3. Frontend calls `POST /api/v1/candidate/sessions` with the invite code
4. Backend validates the invite, creates a `CandidateSession` record, issues a custom JWT
5. JWT is set as a cookie (`candidate_session`) and returned in the response
6. All subsequent candidate API calls use this JWT
7. `CandidateSessionMiddleware` validates the JWT and sets interview context

### 8.5 Bootstrap Secret

For initial group setup (before an API key exists), a `X-Bootstrap-Secret` header can be used with the `POST /api/v1/ats/groups` endpoint. This allows an ATS to register its first group.

---

## 9. External Service Integrations

### 9.1 OpenAI (GPT)

**Used For**:
- Generating interview follow-up suggestions
- Selecting the best follow-up question based on candidate response
- Scoring and analyzing interview responses
- Widget chatbot responses

**Integration Pattern**: `GatewayFacade.OpenAI.cs` → `OpenAIManager` → OpenAI Chat Completions API

### 9.2 ElevenLabs (Text-to-Speech)

**Used For**:
- Agent voice preview and testing
- Real-time voice conversation during interviews
- Pre-caching (warming up) interview audio
- Listing available voices

**Integration Pattern**: `GatewayFacade.ElevenLabs.cs` → `ElevenLabsManager` → ElevenLabs API

**Feature Flag**: Voice can be disabled (returns `ElevenLabsDisabledException` / HTTP 503)

**Streaming**: Audio is streamed directly from ElevenLabs through the API to the client (chunked transfer encoding).

### 9.3 External ATS (Applicant Tracking System)

**Used For**:
- Resolving user permissions and organization access
- Fetching organization lists
- Detecting orphaned entities (entities referencing organizations that no longer exist)

**Integration Pattern**: `GatewayFacade.Ats.cs` → `AtsGatewayManager` → External ATS API (configured per group via `ats_base_url`)

### 9.4 Azure Blob Storage

**Used For**:
- Storing uploaded images (agent profile photos, etc.)
- Storing interview audio recordings

**Integration Pattern**: `ImageManager` → Azure Blob Storage SDK

---

## 10. Testing Strategy

### 10.1 Principles

- **DomainFacade-only testing**: All acceptance tests interact exclusively through the `DomainFacade` public interface
- **No external mocking frameworks**: All test doubles are custom implementations (no Moq, NSubstitute, etc.)
- **Real database**: Tests run against a real PostgreSQL test database
- **Custom `ServiceLocatorForAcceptanceTesting`**: Subclass of `ServiceLocatorBase` that provides test-specific configuration (e.g., test database connection string, fake/stub gateway implementations)
- **`InternalsVisibleTo`**: The DomainLayer grants access to AcceptanceTests so custom stubs can be created for internal components

### 10.2 Test Organization

Tests are organized as partial classes of `DomainFacadeTests`, one file per feature:

```
Orchestrator.AcceptanceTests/
├── Domain/
│   ├── DomainFacadeTests.User.cs
│   ├── DomainFacadeTests.Agent.cs
│   ├── DomainFacadeTests.Group.cs
│   ├── DomainFacadeTests.Job.cs
│   ├── DomainFacadeTests.Applicant.cs
│   ├── DomainFacadeTests.Interview.cs
│   ├── DomainFacadeTests.InterviewGuide.cs
│   ├── DomainFacadeTests.InterviewConfiguration.cs
│   ├── DomainFacadeTests.Voice.cs
│   └── DomainFacadeTests.Image.cs
├── Infrastructure/
│   ├── ServiceLocatorForAcceptanceTesting.cs
│   └── Stubs/                     # Custom test doubles
└── TestUtilities/
    └── TestDataCleanup.cs         # Centralized SQL cleanup
```

### 10.3 Test Lifecycle

```csharp
[TestInitialize]
public async Task TestInitialize()
{
    TestDataCleanup.CleanupAllTestData();  // SQL-based cleanup
    _domainFacade = new DomainFacade(new ServiceLocatorForAcceptanceTesting());
    // Setup test group/user as needed
}

[TestCleanup]
public void TestCleanup()
{
    try { TestDataCleanup.CleanupAllTestData(); }
    catch (Exception ex) { Console.WriteLine($"Warning: {ex.Message}"); }
    finally { _domainFacade?.Dispose(); }
}
```

### 10.4 Test Data Cleanup

- **Pattern-based SQL deletion**: Test data is identified by conventions (e.g., emails ending in `@example.com`, group names starting with `TestGroup_`)
- **FK-aware ordering**: Deletes happen in reverse foreign key dependency order (children first)
- **Runs before AND after each test**: Ensures complete isolation even if a previous test crashed

### 10.5 Common Test Scenarios Per Entity

- `Create_ValidData_ReturnsCreatedEntity`
- `Create_InvalidData_ThrowsValidationException`
- `GetById_ExistingId_ReturnsEntity`
- `GetById_NonExistingId_ReturnsNull`
- `Search_WithResults_ReturnsPaginatedList`
- `Search_EmptyResults_ReturnsEmptyPaginatedList`
- `Update_ValidData_UpdatesSuccessfully`
- `Delete_ExistingId_SoftDeletes`
- `FullLifecycle_CreateReadUpdateSearchDelete`

---

## 11. Build, Run & CI/CD

### 11.1 Prerequisites

- .NET 9 SDK
- Node.js 18+
- PostgreSQL 17
- Java 17 (for Liquibase)
- Liquibase 4.32.x
- `just` command runner

### 11.2 Project Layout

```
/
├── apps/
│   ├── orchestrator/
│   │   ├── Orchestrator.Api/            # .NET 9 Web API
│   │   ├── Orchestrator.DomainLayer/    # Business logic (class library)
│   │   ├── Orchestrator.AcceptanceTests/ # MSTest project
│   │   ├── Orchestrator.Database/       # Liquibase migrations
│   │   │   └── liquibase/
│   │   │       ├── config/liquibase.properties
│   │   │       └── changelog/
│   │   │           ├── db.changelog-master.xml
│   │   │           └── YYYYMMDDHHMMSS-Description.xml ...
│   │   └── Orchestrator.Web/           # Next.js frontend
│   └── hireology-test-ats/             # Secondary application (same structure)
├── docs/
├── scripts/
├── justfile
└── .github/workflows/build.yml
```

### 11.3 Key Commands (justfile)

| Command | Description |
|---|---|
| `just build` | Build the API and Web projects |
| `just start` | Start API (port 5000) and Web (port 3000) |
| `just dev` | Development mode with real ElevenLabs |
| `just dev-fake` | Development mode with fake voice (no API key needed) |
| `just test` | Run acceptance tests |
| `just db-update` | Run Liquibase database migrations |

### 11.4 Configuration

**API (`appsettings.json` / `appsettings.Development.json`)**:
- Database connection string
- Auth0 domain and audience
- Azure Blob Storage connection string
- OpenAI API key
- ElevenLabs API key
- Bootstrap secret
- CORS origins

**Frontend (`.env.local`)**:
- `NEXT_PUBLIC_API_URL` (e.g., `http://localhost:5000`)
- Auth0 configuration (secret, base URL, issuer base URL, client ID, client secret, audience)

### 11.5 CI/CD Pipeline (GitHub Actions)

**Trigger**: Push or PR to `main`

**Steps**:
1. Spin up PostgreSQL 17 as a service container
2. Setup .NET 9 SDK
3. Setup Node.js 18
4. Setup Java 17
5. Install Liquibase 4.32 with PostgreSQL JDBC driver
6. Install web dependencies (`npm ci`)
7. Build all projects
8. Create test databases
9. Run Liquibase migrations
10. Run acceptance tests

### 11.6 API Kestrel Configuration

- Listens on `http://localhost:5000`
- Swagger available in development at `/swagger`
- CORS configured for `http://localhost:3000`

---

## 12. Secondary Application: Hireology Test ATS

The monorepo also contains a second application, `hireology-test-ats`, which serves as a test/reference ATS implementation.

### Structure

Follows the exact same architecture as the Orchestrator:
- `HireologyTestAts.Api` (.NET 9)
- `HireologyTestAts.Domain` (.NET 9)
- `HireologyTestAts.AcceptanceTests` (MSTest)
- `HireologyTestAts.Database` (Liquibase)
- `HireologyTestAts.Web` (Next.js 15, port 3001)

### Purpose

- Demonstrates how an external ATS integrates with the Orchestrator
- Provides a working ATS implementation for end-to-end testing
- Uses its own separate PostgreSQL database (`test_ats`)
- Communicates with the Orchestrator via API key authentication

### Key Differences from Orchestrator
- Separate database (`test_ats`)
- Frontend runs on port 3001
- Has its own groups, users, and entities
- Pushes data (jobs, applicants) to Orchestrator via API
- Retrieves interview results from Orchestrator

---

## Appendix A: Glossary

| Term | Definition |
|---|---|
| **Group** | Top-level tenant (represents an ATS customer account) |
| **Organization** | Sub-tenant within a group (external ATS concept, no local table) |
| **Agent** | An AI interviewer with a configurable voice and personality |
| **Interview Guide** | A reusable set of structured interview questions |
| **Interview Configuration** | Links an Agent + Interview Guide for a specific use case |
| **Interview Invite** | A short-code invitation sent to a candidate |
| **Candidate Session** | A JWT-authenticated session for a candidate conducting an interview |
| **Follow-Up Template** | An AI-suggested follow-up question that can be approved for use |
| **Stock Voice** | A curated voice option from the ElevenLabs library |
| **DomainFacade** | The single entry point to all business logic |
| **DataFacade** | The single entry point to all data access operations |
| **GatewayFacade** | The single entry point to all external service integrations |

## Appendix B: Entity Relationship Summary

```
Group (tenant)
├── Agent (AI interviewer)
├── Job (from ATS)
├── Applicant (from ATS)
├── Interview Guide
│   └── Interview Guide Question
│       └── Follow-Up Template
├── Interview Configuration → links Agent + Interview Guide
├── Interview → links Job + Applicant + Agent + Configuration
│   ├── Interview Response
│   ├── Interview Result
│   ├── Interview Audit Log
│   └── Interview Invite
│       └── Candidate Session
├── Webhook Config
│   └── Webhook Delivery
└── User (system-wide, cross-group via Auth0)
```
