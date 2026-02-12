-- Seed an organization with API key 'hireology-ats-key' for Hireology Test ATS.
-- Name must be "Default Organization" so Orchestrator's Jobs page (GET /Job) lists jobs for this org.
-- Idempotent: safe to run multiple times (no-op if key already exists).
INSERT INTO organizations (id, name, api_key, webhook_url, is_active, created_at, updated_at, is_deleted)
VALUES (gen_random_uuid(), 'Default Organization', 'hireology-ats-key', NULL, true, NOW(), NOW(), false)
ON CONFLICT (api_key) DO NOTHING;

-- Ensure existing org with this key has the expected name (so Jobs page finds it)
UPDATE organizations SET name = 'Default Organization', updated_at = NOW() WHERE api_key = 'hireology-ats-key';
