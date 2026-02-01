# Rules System Overhaul - Complete

## Overview

The rules system has been successfully reorganized into a lean, stage-based architecture where context switches at workflow checkpoints.

## New Structure

### `.cursor/rules/` - Minimal Stage-Based Rules (7 files)

**Always-On Orchestrator**:
- `00-always-on.mdc` - Minimal orchestrator (< 20 lines), explains workflow concept

**Stage-Specific Rules** (5-10 bullets each, auto-apply via globs):
- `stage-db.mdc` → `apps/api/Platform.Database/**/*`
- `stage-domain-data.mdc` → `Platform.Domain/Managers/Models/**/*`, `Platform.Domain/Managers/DataLayer/**/*`
- `stage-domain-logic.mdc` → `Platform.Domain/Managers/**/*` (excluding DataLayer)
- `stage-domain-facade.mdc` → `Platform.Domain/DomainFacade*.cs`
- `stage-tests.mdc` → `apps/api/Platform.AcceptanceTests/**/*`
- `stage-api.mdc` → `apps/api/Platform.Api/**/*`
- `stage-frontend.mdc` → `apps/web/**/*`

**Preserved Rules**:
- `cursor_rules.mdc` - Rule formatting guide
- `self_improve.mdc` - Rule improvement process

### `.cursor/commands/` - Multi-Stage Commands (3 files)

Commands with explicit STOP points between stages:
- `backend-feature.md` - Database → Domain (Data/Logic/Facade) → Tests → API
- `frontend-feature.md` - Structure → Server Actions → Client → Testing
- `full-stack-feature.md` - Complete backend + frontend workflow

### `docs/` - Detailed Documentation

**backend/** - Backend workflow docs:
- `database-workflow.md` - Liquibase migrations, schema design
- `domain-data-workflow.md` - Domain models, data managers, DataFacade
- `domain-logic-workflow.md` - Validators, domain managers, exceptions
- `domain-facade-workflow.md` - DomainFacade pattern, Result<T>
- `testing-workflow.md` - Acceptance testing, cleanup patterns
- `api-workflow.md` - Controllers, resources, mappers

**frontend/** - Frontend workflow docs:
- `frontend-workflow.md` - Next.js, server actions, useServerAction hook

**reference-architecture/** - Template documentation (to be created):
- Annotated examples of each pattern from `.hbs` templates
- Explanation of why patterns work this way
- Common mistakes to avoid
- Links to related patterns

**Root**:
- `architecture-overview.md` - High-level architecture reference

### `.cursor/archive/old-rules/` - Archived Files

Moved for reference:
- `00-rules-index.mdc`
- `api-endpoint-workflow.mdc`
- `architecture-overview.mdc`
- `dev_workflow.mdc`

## Key Improvements

### Context Management

**Before**: Large always-applied rules loaded on every request
**After**: Minimal rules + stage-specific rules that auto-apply via globs

### Workflow Clarity

**Before**: Monolithic workflow in single rule file
**After**: Multi-stage commands with explicit checkpoints and summaries

### Documentation Organization

**Before**: Mixed rules and detailed docs
**After**: 
- Rules: 5-10 bullets pointing to docs/templates
- Docs: Comprehensive workflow guides in docs/ folder
- Templates: Canonical reference architecture in `.hbs` files

### Template Strategy

**Philosophy**: Templates are the golden standard, not existing code

**Three-Tier System**:
1. `.hbs` templates → Source of truth for code structure
2. `docs/reference-architecture/*.md` → Annotated explanations
3. `stage-*.mdc` rules → Quick reference pointing to tiers 1 & 2

## How It Works

### Stage-Based Workflow

1. **User starts command**: `/backend-feature`, `/frontend-feature`, or `/full-stack-feature`
2. **AI works through stages**: Follows command instructions for current stage
3. **Checkpoint reached**: AI summarizes and asks "Ready for next stage?"
4. **Context switches**: 
   - New stage's rule auto-applies (via globs)
   - New stage's docs can be pinned
   - Previous stage context dropped (only summary carried forward)
5. **Repeat** until feature complete

### Example: Backend Feature Flow

```
Stage 1: Database
├─ stage-db.mdc auto-applies (working in Platform.Database/)
├─ docs/backend/database-workflow.md available
├─ Create migration
└─ CHECKPOINT: "Ready for Domain Layer?"

Stage 2A: Domain Data Layer
├─ stage-domain-data.mdc auto-applies (working in Models/ and DataLayer/)
├─ docs/backend/domain-data-workflow.md available
├─ Create models, data managers, DataFacade
└─ SUB-CHECKPOINT: "Ready for business logic?"

Stage 2B: Domain Logic Layer
├─ stage-domain-logic.mdc auto-applies (working in Managers/)
├─ docs/backend/domain-logic-workflow.md available
├─ Create validators, managers, exceptions
└─ SUB-CHECKPOINT: "Ready for facade?"

Stage 2C: Domain Facade Layer
├─ stage-domain-facade.mdc auto-applies (working in DomainFacade*.cs)
├─ docs/backend/domain-facade-workflow.md available
├─ Register manager, create facade extension
└─ CHECKPOINT: "Ready for Tests?"

Stage 3: Acceptance Tests
├─ stage-tests.mdc auto-applies (working in Platform.AcceptanceTests/)
├─ docs/backend/testing-workflow.md available
├─ Create tests through DomainFacade only
└─ CHECKPOINT: "Ready for API?"

Stage 4: API Layer
├─ stage-api.mdc auto-applies (working in Platform.Api/)
├─ docs/backend/api-workflow.md available
├─ Create resources, mappers, controllers
└─ COMPLETE: Full feature summary
```

## Benefits

### For AI Development

1. **Reduced Token Usage**: Only loads relevant context per stage
2. **Better Focus**: Stage rules provide exact guidance for current work
3. **Clear Checkpoints**: Natural places to pause and switch context
4. **Template Adherence**: Always refers to canonical `.hbs` templates, not potentially-flawed existing code

### For Senior Developers

1. **Reference Architecture**: Templates serve as the "correct way"
2. **Consistency**: All features built using same proven patterns
3. **Maintainability**: Changes to patterns only need template updates
4. **Onboarding**: Junior devs learn from templates, not mixed-quality code

### For Project Quality

1. **Pattern Consistency**: Every feature follows exact same structure
2. **Quality Control**: Templates embody best practices
3. **Refactoring Safety**: Implementation details hidden behind facades
4. **Testing Reliability**: Tests through DomainFacade remain valid during refactors

## Success Metrics

✅ Always-on rule < 20 lines
✅ Each stage rule 5-10 bullets
✅ Commands have explicit checkpoints  
✅ Detailed docs moved to docs/ folder
✅ Old rules archived
✅ Pattern applied to backend (with domain layer sub-stages)
✅ Pattern applied to frontend
✅ Context switches naturally via globs + checkpoints

## Reference Architecture Documentation (In Progress)

Created initial reference architecture documentation in `docs/reference-architecture/`:

### ✅ Completed
- **README.md** - Complete overview and guide to using reference docs
- **domain-model.md** - Full documentation with line-by-line explanations, examples, common mistakes
- **data-manager.md** - Comprehensive SQL patterns, Dapper usage, anti-patterns
- **STATUS.md** - Tracks completion status and prioritization

### 🔄 Remaining (High Priority)
- `domain-manager.md` - Business logic orchestration
- `validator.md` - Business rule validation
- `facades.md` - DataFacade and DomainFacade patterns
- `controller.md` - HTTP endpoint handlers
- `resource-model.md` - Request/response DTOs
- `mapper.md` - Layer-to-layer conversion
- `testing.md` - Acceptance test patterns
- `exceptions.md` - Custom exception types
- Common patterns (pagination, error handling, etc.)

Each completed doc includes:
- Complete example from template ✅
- Line-by-line explanation of WHY ✅
- Common mistakes to avoid ✅
- When to use / not use ✅
- Links to related patterns ✅

**Status**: Foundation complete with 3 key patterns documented. Remaining patterns can be completed incrementally as they're used and questions arise. See `docs/reference-architecture/STATUS.md` for details.

### Usage

To use the new system:

1. **Start a feature**: Type `/backend-feature` or `/frontend-feature`
2. **Follow checkpoints**: Answer questions and confirm before each stage
3. **Let context switch**: Stage rules auto-load via globs
4. **Reference docs**: Pin workflow docs when needed
5. **Trust templates**: AI generates from `.hbs` files, not existing code

## Files Created/Modified

### Created
- `.cursor/rules/00-always-on.mdc`
- `.cursor/rules/stage-db.mdc`
- `.cursor/rules/stage-domain-data.mdc`
- `.cursor/rules/stage-domain-logic.mdc`
- `.cursor/rules/stage-domain-facade.mdc`
- `.cursor/rules/stage-tests.mdc`
- `.cursor/rules/stage-api.mdc`
- `.cursor/rules/stage-frontend.mdc`
- `.cursor/commands/backend-feature.md`
- `.cursor/commands/frontend-feature.md`
- `.cursor/commands/full-stack-feature.md`
- `docs/backend/database-workflow.md`
- `docs/backend/domain-data-workflow.md`
- `docs/backend/domain-logic-workflow.md`
- `docs/backend/domain-facade-workflow.md`
- `docs/backend/testing-workflow.md`
- `docs/backend/api-workflow.md`
- `docs/frontend/frontend-workflow.md`
- `docs/architecture-overview.md` (moved from rules)

### Archived
- `.cursor/archive/old-rules/00-rules-index.mdc`
- `.cursor/archive/old-rules/api-endpoint-workflow.mdc`
- `.cursor/archive/old-rules/architecture-overview.mdc`
- `.cursor/archive/old-rules/dev_workflow.mdc`

### Preserved
- `.cursor/rules/cursor_rules.mdc`
- `.cursor/rules/self_improve.mdc`

---

**Implementation Complete**: All planned components have been created and the new rules system is ready for use.

