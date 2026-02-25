# SDET Homework Task — Scriptube

## 🎯 Objective

Build a **C# / .NET test automation framework** for [Scriptube](https://scriptube.me) — a YouTube transcript extraction SaaS.

Demonstrate your ability to design a clean, maintainable, and scalable test framework covering API, UI, and Webhook testing.

---

## 📖 API Documentation

- **Swagger UI (OpenAPI spec):** [https://scriptube.me/docs](https://scriptube.me/docs)
- **Public API docs:** [https://scriptube.me/ui/api-docs](https://scriptube.me/ui/api-docs)
- **Authentication:** All API calls require `X-API-Key` header
- **Base URL:** `https://scriptube.me`

Read the Swagger spec — it defines all endpoints, request/response schemas, and authentication requirements.

---

## 🔐 Test Account

- **Email:** `cristi_test_sdet@gmail.com`
- **Password:** `cristi_test_sdet@gmail.com`
- **Plan:** Pro (500 credits preloaded)
- **API key:** Available in the dashboard after login → [https://scriptube.me/ui/api-keys](https://scriptube.me/ui/api-keys)

### Credit Costs Reference

| Processing Path | Cost |
|----------------|------|
| YouTube captions (manual or auto) | 1 credit |
| YouTube via proxy | 2 credits |
| Translation | 12 credits per 1,000 characters |
| ElevenLabs AI transcription | 10 credits per minute of audio |
| Cached transcript (YouTube source) | 1 credit |
| Cached transcript (ElevenLabs source) | ElevenLabs rate applies |

---

## 🧪 Test Data (Hardcoded Mock Videos)

All video IDs starting with `tst` are deterministic test data — no real YouTube API calls are made. Use these exclusively.

### ✅ Success Videos

| URL | What It Tests |
|-----|---------------|
| `https://www.youtube.com/watch?v=tstENMAN001` | English manual captions (cheapest path, 1 credit) |
| `https://www.youtube.com/watch?v=tstENAUT001` | English auto-generated captions (1 credit) |
| `https://www.youtube.com/watch?v=tstKOONL001` | Korean only → auto-translation to English |
| `https://www.youtube.com/watch?v=tstESAUT001` | Spanish only → auto-translation to English |
| `https://www.youtube.com/watch?v=tstMULTI001` | Multi-language video, picks English (1 credit) |
| `https://www.youtube.com/watch?v=tstYTTRN001` | French → YouTube auto-translate to English (free) |
| `https://www.youtube.com/watch?v=tstNOCAP001` | No captions → ElevenLabs AI fallback (paid plan required) |
| `https://www.youtube.com/watch?v=tstELABS001` | Force ElevenLabs transcription (paid plan required) |
| `https://www.youtube.com/watch?v=tstELTRN001` | ElevenLabs + translation (German → English) |
| `https://www.youtube.com/watch?v=tstCACHE001` | Cached YouTube transcript (cache hit) |
| `https://www.youtube.com/watch?v=tstCACEL001` | Cached ElevenLabs transcript |

### ❌ Error Videos

| URL | What It Tests |
|-----|---------------|
| `https://www.youtube.com/watch?v=tstPRIVT001` | Private video → error |
| `https://www.youtube.com/watch?v=tstDELET001` | Deleted video → error |
| `https://www.youtube.com/watch?v=tstAGERS001` | Age-restricted → error |
| `https://www.youtube.com/watch?v=tstLONG0001` | 120 min video, too long → error |
| `https://www.youtube.com/watch?v=tstRLIMT001` | Rate limit → retry → recovery |
| `https://www.youtube.com/watch?v=tstTIMEO001` | Connection timeout → error |
| `https://www.youtube.com/watch?v=tstINVLD001` | Malformed data → processing error |

### 📁 Test Playlists

| URL | Contents |
|-----|----------|
| `https://www.youtube.com/playlist?list=PLtstOK00001` | 3 success videos |
| `https://www.youtube.com/playlist?list=PLtstMIX0001` | Mixed: English + Korean + ElevenLabs |
| `https://www.youtube.com/playlist?list=PLtstALL0001` | 5 videos: success + errors mixed |
| `https://www.youtube.com/playlist?list=PLtstERR0001` | 3 error-only videos |

---

## 🔑 What to Cover

### 1. API Tests — Crucial Endpoints

| Endpoint | Method | What to Test |
|----------|--------|--------------|
| `/api/v1/transcripts` | POST | Submit batch: single video, multiple videos, playlist URL, with/without `translate_to_english`, with/without `use_byok` |
| `/api/v1/transcripts/{batch_id}` | GET | Poll batch status until completion |
| `/api/v1/transcripts/{batch_id}/export` | GET | Export results in JSON, TXT, SRT formats |
| `/api/v1/credits/balance` | GET | Check credit balance before/after processing |
| `/api/v1/usage` | GET | Usage statistics |
| `/api/v1/plans` | GET | Available plans listing |
| `/api/v1/user` | GET | User info and plan details |
| `/api/v1/credits/precheck` | POST | Pre-validate URLs + estimate cost before submitting |
| `/api/v1/credits/estimate` | POST | Cost estimation for video IDs |
| `/api/v1/credits/costs` | GET | Credit cost table |
| `/api/v1/credits/history` | GET | Credit transaction log |
| `/api/v1/transcripts/{batch_id}/cancel` | POST | Cancel a running batch |
| `/api/v1/transcripts/{batch_id}/retry-failed` | POST | Retry all failed items in a batch |
| `/api/v1/transcripts/{batch_id}/rerun` | POST | Rerun entire batch |
| `/api/v1/transcripts/{batch_id}` | DELETE | Delete a batch |

**Key business logic flows to test:**
- **Precheck → Submit → Poll → Export:** Full E2E flow — precheck URLs first, verify estimated cost, submit batch, poll until complete, export results
- **Credit deduction verification:** Check balance before and after batch processing — verify correct credits were charged per processing path
- **Cancel mid-processing:** Submit batch → immediately cancel → verify status changes
- **Retry failed items:** Submit batch with error video IDs (`tstPRIVT001`, `tstTIMEO001`) → verify failures → retry failed items
- **Free SEO tool:** `POST /tools/youtube-transcript` — public endpoint, no auth required, single video transcript (good smoke test)

**Negative cases to cover:**
- No API key → 401
- Invalid API key → 401
- Empty URL list → validation error
- Invalid URL (not YouTube) → validation error
- Non-existent batch ID → 404
- Accessing another user's batch → 404

### 2. UI E2E Tests (Playwright — Chromium only)

| Flow | What to Test |
|------|--------------|
| **Login** | Valid credentials → dashboard redirect; Invalid credentials → error message |
| **Signup** | New account creation; Duplicate email → error |
| **Submit batch** | Paste test video URLs in dashboard → submit → batch created |
| **Batch detail** | View progress, items, status, transcript preview |
| **Export** | Download transcript from batch detail page |
| **Credits page** | Verify balance display and credit pack options |
| **Pricing page** | Verify all plans displayed with correct pricing |

### 3. Webhook Tests — With & Without BYOK

| Scenario | What to Test |
|----------|--------------|
| **Register webhook** | POST /api/webhooks/register with valid HTTPS URL → 201 |
| **Trigger test event** | POST /api/webhooks/{id}/test → event delivered |
| **Batch complete → webhook fires** | Submit batch (`use_byok=false`) → wait for completion → verify webhook payload + HMAC signature |
| **Batch complete with BYOK** | Submit batch (`use_byok=true`) → verify webhook fires, verify different credit cost |
| **Payload validation** | Webhook payload matches expected schema (batch_id, status, items) |
| **HMAC-SHA256 verification** | Compute signature with webhook secret, compare to header |
| **SSRF protection** | Try registering `http://localhost/x`, `http://192.168.1.1/x`, `http://10.0.0.1/x` → should be blocked |
| **View delivery logs** | GET /api/webhooks/{id}/logs → delivery history with status codes |
| **Available events** | GET /api/webhooks/events/available → list of subscribable events |

---

## ⚙️ Technical Requirements (Mandatory)

### Framework & Language
- **C# / .NET 8+**
- **Test runner:** NUnit or xUnit
- **HTTP client:** RestSharp or HttpClient
- **UI automation:** Playwright for .NET (Chromium only)
- **Assertions:** FluentAssertions

### Reporting — GitHub Pages
- **Allure Report** (or equivalent rich HTML report) auto-published to **GitHub Pages** after each CI run
- Report must include:
  - Full test logs (HTTP request/response for every API call)
  - Screenshots on failure for UI tests
  - Step-by-step execution trace
  - Pass/fail/skip breakdown per area
- Live report URL linked in repo README

### CI Triggers
- **Manual:** `workflow_dispatch` with parameters (area, suite, threads)
- **On push:** to `main` branch — runs smoke suite
- **On pull request:** runs full regression

### Test Execution — Parameterized CI
- **GitHub Actions** as CI/CD (free tier)
- Pipeline must accept these parameters:
  - **`area`**: `api` | `ui` | `webhook` | `all`
  - **`suite`**: `smoke` | `regression`
  - **`threads`**: number of parallel threads (e.g. 1, 2, 4)
- Tests must be tagged/categorized: `[Category("API")]`, `[Category("UI")]`, `[Category("Webhook")]`, `[Category("Smoke")]`, `[Category("Regression")]`

### Pre-commit Checks
- **`dotnet format`** — code style enforcement
- **`dotnet build --warnaserror`** — no warnings allowed
- Import ordering and unused import detection
- Can use a git hook or CI step — must run before tests

### Parallel Execution
- Configurable thread count via CI parameter
- No shared state between tests — each test manages its own data
- Thread-safe HTTP clients and test context

### Request Logging
- All HTTP requests/responses captured and attached to report
- Logged: method, URL, headers (API key masked), request body, response status, response body
- Visible in Allure report as test steps

### Environment Configuration
- Support multiple environments via config (e.g. `appsettings.json`, `appsettings.prod.json`)
- Base URL, credentials, timeouts — all configurable, **zero hardcoded values**
- API keys stored in **GitHub Secrets** for CI, never committed to repo

### Secrets Management
- No credentials in source code — ever
- Use environment variables or `.env` files (gitignored) for local runs
- GitHub Secrets for CI pipeline

### Retry & Stability
- Configurable retry count for flaky tests (e.g. network timeouts)

### Architecture & Code Quality
- **Design patterns:** Page Object Model for UI, Service/Client pattern for API, Builder pattern for test data
- **Reusable methods:** shared HTTP client wrappers, common assertions, helper utilities
- **Generic approach:** base test classes, shared fixtures, configuration injection
- **No hardcoded values:** URLs, IDs, credentials, timeouts — everything from config
- **DRY:** no copy-paste between tests — extract common logic into helpers
- **Readable tests:** test names describe the scenario, arrange-act-assert structure
- **Separation of concerns:** test logic, API clients, models, utilities — in separate layers

### Bonus (Nice to Have — only a few tests needed to demonstrate)
- SpecFlow / Reqnroll BDD — write 2-3 scenarios to show the approach
- API contract validation against OpenAPI spec — 1-2 endpoints
- Performance smoke test — response time assertion on health endpoint

### Repository
- Clean .NET solution in a **GitHub repository**
- `README.md` with:
  - Setup instructions (local run)
  - How to run via CI (with parameter examples)
  - Link to latest GitHub Pages report
- All configs, test data references, and CI workflows committed

---

## 📊 Evaluation Criteria

| Area | Weight |
|------|--------|
| **Framework architecture** (clean structure, patterns, reusability) | 25% |
| **Test coverage** (positive + negative + edge cases) | 25% |
| **CI/CD pipeline** (parameterized, parallel, reporting) | 20% |
| **Code quality** (readability, naming, DRY, no hardcoded secrets) | 15% |
| **Reporting** (logs, screenshots, GitHub Pages) | 15% |

---

## ⏰ Deadline

**5 business days** from receiving this task.

---

## 💡 Tips

- Read the Swagger spec thoroughly before writing any code
- Use the test video IDs — they're deterministic and won't consume real credits
- The Pro plan account has webhooks, BYOK, ElevenLabs, and translation unlocked
- Design for maintainability — imagine this framework will be used by a team of 5

Good luck! 🚀
