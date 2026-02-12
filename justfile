build:
	dotnet build apps/Orchestrator.sln
	cd apps/web/Orchestrator.Web && npm install && npm run build

# Build Hireology Test ATS (API + Web only; no DB step)
build-test-ats:
	dotnet build apps/hireology-test-ats/HireologyTestAts.sln
	cd apps/hireology-test-ats/HireologyTestAts.Web && npm install && npm run build

test:
	dotnet test apps/api/Orchestrator.AcceptanceTests/Orchestrator.AcceptanceTests.csproj --verbosity normal

db-update:
	liquibase --defaultsFile=apps/api/Orchestrator.Database/liquibase/config/liquibase.properties --changelog-file=apps/api/Orchestrator.Database/changelog/db.changelog-master.xml update

db-connect:
	psql -h localhost -U postgres -d orchestrator

# Seed Orchestrator DB with org that has api_key 'hireology-ats-key' (for Hireology Test ATS). Idempotent.
seed-ats-api-key:
	psql -h localhost -U postgres -d orchestrator -f scripts/seed-ats-api-key.sql

# Hireology Test ATS database (separate DB: test_ats). Create once: createdb test_ats
db-test-ats-update:
	liquibase --defaultsFile=apps/hireology-test-ats/HireologyTestAts.Database/liquibase/config/liquibase.properties --changelog-file=apps/hireology-test-ats/HireologyTestAts.Database/changelog/db.changelog-master.xml update

# Dev targets: real ElevenLabs = dev-api, dev, start. Fake voice (no key) = dev-api-fake, dev-fake, start-fake.
# Start ONLY the API server (useful for running in separate terminal)
dev-api:
	cd apps/api/Orchestrator.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run

# Run API with fake ElevenLabs (no API key needed)
dev-api-fake:
	cd apps/api/Orchestrator.Api && ASPNETCORE_ENVIRONMENT=Development Voice__UseFakeElevenLabs=true dotnet run

# Start ONLY the web server (useful for running in separate terminal)
dev-web:
	cd apps/web/Orchestrator.Web && npm run dev

# Start both API and web applications in development mode (no build)
dev:
	#!/usr/bin/env bash
	set -euo pipefail

	# Kill any existing servers to avoid "address in use" errors
	echo "🧹 Cleaning up old processes..."
	lsof -ti:5000 | xargs kill -9 2>/dev/null || true
	lsof -ti:3000 | xargs kill -9 2>/dev/null || true
	sleep 1

	echo "🚀 Starting API server..."
	cd apps/api/Orchestrator.Api
	ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Orchestrator.Web
	npm run dev 2>&1 | sed 's/^/[🟢 WEB] /' &
	WEB_PID=$!

	echo "⏳ Waiting for servers to start..."
	sleep 5

	echo "🌍 Opening browsers..."
	open "http://localhost:5000/swagger"
	open "http://localhost:3000"

	echo "✅ Both applications are running!"
	echo "📋 API: http://localhost:5000/swagger"
	echo "🌐 Web: http://localhost:3000"
	echo "🛑 Press Ctrl+C to stop both servers"

	# Wait for user to stop
	trap "kill $API_PID $WEB_PID 2>/dev/null || true; echo '🛑 Stopped all servers'" EXIT
	wait

# Same as dev but API runs with fake ElevenLabs (no API key needed)
dev-fake:
	#!/usr/bin/env bash
	set -euo pipefail

	echo "🧹 Cleaning up old processes..."
	lsof -ti:5000 | xargs kill -9 2>/dev/null || true
	lsof -ti:3000 | xargs kill -9 2>/dev/null || true
	sleep 1

	echo "🚀 Starting API server (fake voice)..."
	cd apps/api/Orchestrator.Api
	ASPNETCORE_ENVIRONMENT=Development Voice__UseFakeElevenLabs=true dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Orchestrator.Web
	npm run dev 2>&1 | sed 's/^/[🟢 WEB] /' &
	WEB_PID=$!

	echo "⏳ Waiting for servers to start..."
	sleep 5

	echo "🌍 Opening browsers..."
	open "http://localhost:5000/swagger"
	open "http://localhost:3000"

	echo "✅ Both applications are running! (API in fake voice mode)"
	echo "📋 API: http://localhost:5000/swagger"
	echo "🌐 Web: http://localhost:3000"
	echo "🛑 Press Ctrl+C to stop both servers"

	trap "kill $API_PID $WEB_PID 2>/dev/null || true; echo '🛑 Stopped all servers'" EXIT
	wait

# Start only Hireology Test ATS (API 5001 + Web 3001). Run just dev in another terminal if you need Orchestrator.
dev-test-ats:
	#!/usr/bin/env bash
	set -euo pipefail

	echo "🧹 Cleaning up old processes on 5001, 3001..."
	lsof -ti:5001 | xargs kill -9 2>/dev/null || true
	lsof -ti:3001 | xargs kill -9 2>/dev/null || true
	sleep 1

	echo "🚀 Starting Hireology Test ATS API (5001)..."
	(cd apps/hireology-test-ats/HireologyTestAts.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🟠 API] /') &
	API_PID=$!

	echo "🌐 Starting Hireology Test ATS Web (3001)..."
	(cd apps/hireology-test-ats/HireologyTestAts.Web && npm install && npm run dev 2>&1 | sed 's/^/[🟠 WEB] /') &
	WEB_PID=$!

	echo "⏳ Waiting for servers to start..."
	sleep 5

	echo "✅ Hireology Test ATS is running!"
	echo "📋 Hireology Test ATS API: http://localhost:5001/swagger"
	echo "🌐 Hireology Test ATS Web: http://localhost:3001"
	echo "🛑 Press Ctrl+C to stop both"

	trap "kill $API_PID $WEB_PID 2>/dev/null || true; echo '🛑 Stopped Hireology Test ATS'" EXIT
	wait

# Start all four: Orchestrator (5000, 3000) + Hireology Test ATS (5001, 3001). One Ctrl+C stops all.
dev-all:
	#!/usr/bin/env bash
	set -euo pipefail

	echo "🧹 Cleaning up old processes..."
	lsof -ti:5000 | xargs kill -9 2>/dev/null || true
	lsof -ti:3000 | xargs kill -9 2>/dev/null || true
	lsof -ti:5001 | xargs kill -9 2>/dev/null || true
	lsof -ti:3001 | xargs kill -9 2>/dev/null || true
	sleep 1

	echo "🚀 Starting Orchestrator API (5000)..."
	(cd apps/api/Orchestrator.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🔵 API] /') &
	ORCH_API_PID=$!

	echo "🌐 Starting Orchestrator Web (3000)..."
	(cd apps/web/Orchestrator.Web && npm run dev 2>&1 | sed 's/^/[🟢 WEB] /') &
	ORCH_WEB_PID=$!

	echo "🚀 Starting Hireology Test ATS API (5001)..."
	(cd apps/hireology-test-ats/HireologyTestAts.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🟠 API] /') &
	TEST_ATS_API_PID=$!

	echo "🌐 Starting Hireology Test ATS Web (3001)..."
	(cd apps/hireology-test-ats/HireologyTestAts.Web && npm install && npm run dev 2>&1 | sed 's/^/[🟠 WEB] /') &
	TEST_ATS_WEB_PID=$!

	echo "⏳ Waiting for servers to start..."
	sleep 6

	echo "🌍 Opening browsers..."
	open "http://localhost:5000/swagger"
	open "http://localhost:3000"
	open "http://localhost:5001/swagger"
	open "http://localhost:3001"

	echo "✅ All four applications are running!"
	echo "📋 Orchestrator API: http://localhost:5000/swagger | Web: http://localhost:3000"
	echo "📋 Hireology Test ATS API: http://localhost:5001/swagger | Web: http://localhost:3001"
	echo "🛑 Press Ctrl+C to stop all"

	trap "kill $ORCH_API_PID $ORCH_WEB_PID $TEST_ATS_API_PID $TEST_ATS_WEB_PID 2>/dev/null || true; echo '🛑 Stopped all servers'" EXIT
	wait

# Build then start all four (Orchestrator + Hireology Test ATS)
start-all:
	#!/usr/bin/env bash
	set -euo pipefail

	echo "🔨 Building Orchestrator and Hireology Test ATS..."
	just build
	just build-test-ats

	echo "🧹 Cleaning up old processes..."
	lsof -ti:5000 | xargs kill -9 2>/dev/null || true
	lsof -ti:3000 | xargs kill -9 2>/dev/null || true
	lsof -ti:5001 | xargs kill -9 2>/dev/null || true
	lsof -ti:3001 | xargs kill -9 2>/dev/null || true
	sleep 1

	echo "🚀 Starting Orchestrator API (5000)..."
	(cd apps/api/Orchestrator.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🔵 API] /') &
	ORCH_API_PID=$!

	echo "🌐 Starting Orchestrator Web (3000)..."
	(cd apps/web/Orchestrator.Web && npm run dev 2>&1 | sed 's/^/[🟢 WEB] /') &
	ORCH_WEB_PID=$!

	echo "🚀 Starting Hireology Test ATS API (5001)..."
	(cd apps/hireology-test-ats/HireologyTestAts.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🟠 API] /') &
	TEST_ATS_API_PID=$!

	echo "🌐 Starting Hireology Test ATS Web (3001)..."
	(cd apps/hireology-test-ats/HireologyTestAts.Web && npm run dev 2>&1 | sed 's/^/[🟠 WEB] /') &
	TEST_ATS_WEB_PID=$!

	echo "⏳ Waiting for servers to start..."
	sleep 6

	echo "🌍 Opening browsers..."
	open "http://localhost:5000/swagger"
	open "http://localhost:3000"
	open "http://localhost:5001/swagger"
	open "http://localhost:3001"

	echo "✅ All four applications are running!"
	echo "📋 Orchestrator API: http://localhost:5000/swagger | Web: http://localhost:3000"
	echo "📋 Hireology Test ATS API: http://localhost:5001/swagger | Web: http://localhost:3001"
	echo "🛑 Press Ctrl+C to stop all"

	trap "kill $ORCH_API_PID $ORCH_WEB_PID $TEST_ATS_API_PID $TEST_ATS_WEB_PID 2>/dev/null || true; echo '🛑 Stopped all servers'" EXIT
	wait

# Build and start both API and web applications
start:
	#!/usr/bin/env bash
	set -euo pipefail

	# Kill any existing servers to avoid "address in use" errors
	echo "🧹 Cleaning up old processes..."
	lsof -ti:5000 | xargs kill -9 2>/dev/null || true
	lsof -ti:3000 | xargs kill -9 2>/dev/null || true
	sleep 1

	echo "🔨 Building applications..."
	just build

	echo "🚀 Starting API server..."
	cd apps/api/Orchestrator.Api
	ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Orchestrator.Web
	npm run dev 2>&1 | sed 's/^/[🟢 WEB] /' &
	WEB_PID=$!

	echo "⏳ Waiting for servers to start..."
	sleep 5

	echo "🌍 Opening browsers..."
	open "http://localhost:5000/swagger"
	open "http://localhost:3000"

	echo "✅ Both applications are running!"
	echo "📋 API: http://localhost:5000/swagger"
	echo "🌐 Web: http://localhost:3000"
	echo "🛑 Press Ctrl+C to stop both servers"

	# Wait for user to stop
	trap "kill $API_PID $WEB_PID 2>/dev/null || true; echo '🛑 Stopped all servers'" EXIT
	wait

# Same as start but API runs with fake ElevenLabs (no API key needed)
start-fake:
	#!/usr/bin/env bash
	set -euo pipefail

	echo "🧹 Cleaning up old processes..."
	lsof -ti:5000 | xargs kill -9 2>/dev/null || true
	lsof -ti:3000 | xargs kill -9 2>/dev/null || true
	sleep 1

	echo "🔨 Building applications..."
	just build

	echo "🚀 Starting API server (fake voice)..."
	cd apps/api/Orchestrator.Api
	ASPNETCORE_ENVIRONMENT=Development Voice__UseFakeElevenLabs=true dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Orchestrator.Web
	npm run dev 2>&1 | sed 's/^/[🟢 WEB] /' &
	WEB_PID=$!

	echo "⏳ Waiting for servers to start..."
	sleep 5

	echo "🌍 Opening browsers..."
	open "http://localhost:5000/swagger"
	open "http://localhost:3000"

	echo "✅ Both applications are running! (API in fake voice mode)"
	echo "📋 API: http://localhost:5000/swagger"
	echo "🌐 Web: http://localhost:3000"
	echo "🛑 Press Ctrl+C to stop both servers"

	trap "kill $API_PID $WEB_PID 2>/dev/null || true; echo '🛑 Stopped all servers'" EXIT
	wait
