# TokenGuard

Lightweight AI API proxy and budget enforcer. Sit TokenGuard in front of OpenAI, Gemini, and Anthropic to track costs per key/project and hard-stop requests when a daily budget is exceeded.

## Architecture

```
cost-control/
├── src/
│   ├── TokenGuard.Core/          # Domain entities & interfaces
│   ├── TokenGuard.Infrastructure/ # Redis + SQLite/EF Core implementations
│   └── TokenGuard.Api/           # ASP.NET Core 9 proxy + management API
├── frontend/                     # React 18 + Vite + Tailwind dashboard
├── docker-compose.yml
└── .env.example
```

## Quick Start

### With Docker Compose

```bash
cp .env.example .env
# Edit .env with your upstream API keys and a JWT secret
docker compose up --build
```

- API: http://localhost:5000
- Dashboard: http://localhost:3000

### Local Development

**Prerequisites:** .NET 9 SDK, Redis (or `docker run -p 6379:6379 redis:7-alpine`), Node 20

```bash
# Start API
cd src/TokenGuard.Api
dotnet run

# Start dashboard (separate terminal)
cd frontend
npm install
npm run dev
```

## Usage

### 1. Get a management token

```bash
curl -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"adminSecret":"CHANGE_ME_32_CHARS_MIN_SECRET_KEY_1"}'
```

### 2. Create an API key

```bash
TOKEN=<token from above>
curl -X POST http://localhost:5000/api/keys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"My Key","projectName":"my-project","dailyLimitUsd":5.00}'
```

Save the returned `key` value — it is only shown once.

### 3. Proxy a request

```bash
TG_KEY=<key from above>
curl -X POST http://localhost:5000/v1/proxy/openai/v1/chat/completions \
  -H "X-TokenGuard-Key: $TG_KEY" \
  -H "Content-Type: application/json" \
  -d '{"model":"gpt-4o","messages":[{"role":"user","content":"Hello"}]}'
```

TokenGuard forwards to OpenAI (or fails over to Gemini/Anthropic), records usage, and enforces the budget.

## Management API

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/token` | Get JWT (pass `adminSecret` = JWT secret) |
| POST | `/api/keys` | Create key |
| GET | `/api/keys` | List keys with today's spend |
| DELETE | `/api/keys/{id}` | Deactivate key |
| GET | `/api/keys/{id}/usage?days=7` | Usage history |
| GET | `/api/dashboard` | Aggregate stats |

## Configuration

All settings live in `appsettings.json` and can be overridden with environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__Redis` | `localhost:6379` | Redis connection |
| `ConnectionStrings__Sqlite` | `tokenguard.db` | SQLite DB path |
| `Jwt__Secret` | (change me) | ≥32 char signing secret |
| `OPENAI_API_KEY` | — | Upstream OpenAI key |
| `GEMINI_API_KEY` | — | Upstream Gemini key |
| `ANTHROPIC_API_KEY` | — | Upstream Anthropic key |

## How It Works

1. Client sends request to `/v1/proxy/{provider}/...` with `X-TokenGuard-Key` header.
2. ProxyMiddleware looks up the key, checks Redis budget counter.
3. If over limit → 429 with `{"error":"budget_exceeded","spent":X,"limit":Y}`.
4. Otherwise, forwards to upstream provider with its actual API key.
5. On success, parses token usage from response, estimates cost, increments Redis counter, persists `UsageRecord` in SQLite.
6. If upstream returns 5xx or times out, automatically tries the next provider.
