# Database Schema Summary

Generated: 2025-12-05 15:59:35 CST

> **Regenerate**: `just update-db-context`

---

## Tables

| Table | Columns |
|-------|---------|
| [chat_topics](#chat_topics) | 4 |
| [chats](#chats) | 12 |
| [databasechangelog](#databasechangelog) | 14 |
| [databasechangeloglock](#databasechangeloglock) | 4 |
| [messages](#messages) | 11 |
| [personas](#personas) | 13 |
| [users](#users) | 14 |

---

## chat_topics

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | PRIMARY KEY |
| chat_id | uuid | NO |  | FOREIGN KEY |
| topic_id | uuid | NO |  |  |
| added_at | timestamp without time zone | NO | now() |  |

**Foreign Keys:**
- chat_id → chats.id

## chats

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | PRIMARY KEY |
| persona_id | uuid | NO |  |  |
| user_id | uuid | NO |  |  |
| title | character varying(500) | YES |  |  |
| last_message_at | timestamp without time zone | NO |  |  |
| created_at | timestamp without time zone | NO | now() |  |
| updated_at | timestamp without time zone | YES |  |  |
| created_by | character varying(100) | YES |  |  |
| updated_by | character varying(100) | YES |  |  |
| is_deleted | boolean | NO | false |  |
| deleted_at | timestamp without time zone | YES |  |  |
| deleted_by | character varying(100) | YES |  |  |

## databasechangelog

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| id | character varying(255) | NO |  |  |
| author | character varying(255) | NO |  |  |
| filename | character varying(255) | NO |  |  |
| dateexecuted | timestamp without time zone | NO |  |  |
| orderexecuted | integer | NO |  |  |
| exectype | character varying(10) | NO |  |  |
| md5sum | character varying(35) | YES |  |  |
| description | character varying(255) | YES |  |  |
| comments | character varying(255) | YES |  |  |
| tag | character varying(255) | YES |  |  |
| liquibase | character varying(20) | YES |  |  |
| contexts | character varying(255) | YES |  |  |
| labels | character varying(255) | YES |  |  |
| deployment_id | character varying(10) | YES |  |  |

## databasechangeloglock

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| id | integer | NO |  | PRIMARY KEY |
| locked | boolean | NO |  |  |
| lockgranted | timestamp without time zone | YES |  |  |
| lockedby | character varying(255) | YES |  |  |

## messages

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | PRIMARY KEY |
| chat_id | uuid | NO |  |  |
| role | character varying(50) | NO |  |  |
| content | character varying(10000) | NO |  |  |
| created_at | timestamp without time zone | NO | now() |  |
| updated_at | timestamp without time zone | YES |  |  |
| created_by | character varying(100) | YES |  |  |
| updated_by | character varying(100) | YES |  |  |
| is_deleted | boolean | NO | false |  |
| deleted_at | timestamp without time zone | YES |  |  |
| deleted_by | character varying(100) | YES |  |  |

## personas

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | PRIMARY KEY |
| first_name | character varying(100) | YES |  |  |
| last_name | character varying(100) | YES |  |  |
| display_name | character varying(100) | NO |  |  |
| profile_image_url | character varying(500) | YES |  |  |
| created_at | timestamp without time zone | NO | now() |  |
| updated_at | timestamp without time zone | YES |  |  |
| created_by | character varying(100) | YES |  |  |
| updated_by | character varying(100) | YES |  |  |
| is_deleted | boolean | NO | false |  |
| deleted_at | timestamp without time zone | YES |  |  |
| deleted_by | character varying(100) | YES |  |  |
| training_file_path | character varying(1000) | YES |  |  |

## users

| Column | Type | Nullable | Default | Constraints |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | PRIMARY KEY |
| first_name | character varying(100) | YES |  |  |
| last_name | character varying(100) | YES |  |  |
| email | character varying(255) | NO |  | UNIQUE |
| phone | character varying(20) | YES |  |  |
| created_at | timestamp without time zone | NO | now() |  |
| updated_at | timestamp without time zone | YES |  |  |
| created_by | character varying(100) | YES |  |  |
| updated_by | character varying(100) | YES |  |  |
| is_deleted | boolean | NO | false |  |
| deleted_at | timestamp without time zone | YES |  |  |
| deleted_by | character varying(100) | YES |  |  |
| auth0_sub | character varying(255) | YES |  | UNIQUE |
| profile_image_url | character varying(500) | YES |  |  |

