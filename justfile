build:
	dotnet build apps/Platform.sln
	cd apps/web/Platform.Web && npm install && npm run build

test:
	dotnet test apps/api/Platform.AcceptanceTests/Platform.AcceptanceTests.csproj --verbosity normal

db-update:
	liquibase --defaultsFile=apps/api/Platform.Database/liquibase/config/liquibase.properties --changelog-file=apps/api/Platform.Database/changelog/db.changelog-master.xml update

db-connect:
	psql -h localhost -U postgres -d surrova

# Dev targets: real ElevenLabs = dev-api, dev, start. Fake voice (no key) = dev-api-fake, dev-fake, start-fake.
# Start ONLY the API server (useful for running in separate terminal)
dev-api:
	cd apps/api/Platform.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run

# Run API with fake ElevenLabs (no API key needed)
dev-api-fake:
	cd apps/api/Platform.Api && ASPNETCORE_ENVIRONMENT=Development Voice__UseFakeElevenLabs=true dotnet run

# Start ONLY the web server (useful for running in separate terminal)
dev-web:
	cd apps/web/Platform.Web && npm run dev

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
	cd apps/api/Platform.Api
	ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Platform.Web
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
	cd apps/api/Platform.Api
	ASPNETCORE_ENVIRONMENT=Development Voice__UseFakeElevenLabs=true dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Platform.Web
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
	cd apps/api/Platform.Api
	ASPNETCORE_ENVIRONMENT=Development dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Platform.Web
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
	cd apps/api/Platform.Api
	ASPNETCORE_ENVIRONMENT=Development Voice__UseFakeElevenLabs=true dotnet run 2>&1 | sed 's/^/[🔵 API] /' &
	API_PID=$!

	echo "🌐 Starting web server..."
	cd ../../../apps/web/Platform.Web
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
