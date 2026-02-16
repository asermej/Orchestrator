-- Seed a group with API key 'hireology-ats-key' for Hireology Test ATS.
-- Name must be "Default Group" so Orchestrator's Jobs page (GET /Job) lists jobs for this group.
-- Idempotent: safe to run multiple times (no-op if key already exists).
INSERT INTO groups (id, name, api_key, webhook_url, is_active, created_at, updated_at, is_deleted)
VALUES (gen_random_uuid(), 'Default Group', 'hireology-ats-key', NULL, true, NOW(), NOW(), false)
ON CONFLICT (api_key) DO NOTHING;

-- Ensure existing group with this key has the expected name (so Jobs page finds it)
UPDATE groups SET name = 'Default Group', updated_at = NOW() WHERE api_key = 'hireology-ats-key';
