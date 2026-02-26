# Plan de implementare detaliat — Scriptube Test Automation Framework

[In Proces]
## 1) Scop și rezultat urmărit

Acest plan definește implementarea unui framework de automatizare în **C# / .NET 8+** pentru Scriptube, cu acoperire pe:
- API tests
- UI E2E tests (Playwright, Chromium)
- Webhook tests (inclusiv validare semnătură HMAC și scenarii BYOK)

Rezultatul final: un repo gata de rulare locală și în GitHub Actions, cu raportare Allure publicată pe GitHub Pages, configurare pe medii și zero secrete hardcodate.

---

[Creat]
## 2) Principii arhitecturale

- **Separare clară a responsabilităților**: teste, clienți API, modele, utilitare, config.
- **Reutilizare / DRY**: fixture-uri comune, helper-e comune, builder-e de request.
- **Scalabilitate**: adăugare facilă de endpoint-uri/scenarii fără copy-paste.
- **Stabilitate**: retry controlat, polling robust, izolare între teste.
- **Observabilitate**: log complet request/response + atașare în Allure.

---

[Creat]
## 3) Structura soluției propuse

> Se păstrează soluția existentă și se extinde în proiecte/foldere clare.

```text
Scriptube.Tests.sln
 ├─ src/
 │   ├─ Scriptube.Core/
 │   │   ├─ Configuration/
 │   │   ├─ Http/
 │   │   ├─ Logging/
 │   │   ├─ Retry/
 │   │   ├─ Security/
 │   │   └─ Utilities/
 │   ├─ Scriptube.Api/
 │   │   ├─ Clients/
 │   │   ├─ Contracts/ (DTO request/response)
 │   │   ├─ Builders/
 │   │   └─ Assertions/
 │   ├─ Scriptube.Ui/
 │   │   ├─ Pages/
 │   │   ├─ Components/
 │   │   ├─ Flows/
 │   │   └─ Assertions/
 │   └─ Scriptube.Webhooks/
 │       ├─ Clients/
 │       ├─ Models/
 │       ├─ Signature/
 │       └─ Assertions/
 ├─ tests/
 │   ├─ Scriptube.Tests.Api/
 │   ├─ Scriptube.Tests.Ui/
 │   ├─ Scriptube.Tests.Webhooks/
 │   └─ Scriptube.Tests.Shared/
 ├─ config/
 │   ├─ appsettings.json
 │   ├─ appsettings.prod.json
 │   └─ appsettings.local.template.json
 ├─ .github/workflows/
 │   ├─ ci-tests.yml
 │   └─ publish-allure.yml (opțional separat)
 ├─ scripts/
 │   ├─ precommit.ps1
 │   └─ run-tests.ps1
 ├─ .allure/
 ├─ README.md
 └─ IMPLEMENTATION_PLAN.md
```

---

[In Proces]
## 4) Stack tehnic și pachete NuGet

- **Test runner**: NUnit (alegere recomandată pentru categorii `[Category]`)
- **Assertions**: FluentAssertions
- **HTTP**: HttpClient + delegating handlers custom
- **UI**: Microsoft.Playwright + Microsoft.Playwright.NUnit
- **Reporting**: Allure.NUnit
- **Config**: Microsoft.Extensions.Configuration + Json + EnvironmentVariables
- **Serialization**: System.Text.Json
- **Resilience**: Polly (retry/timeouts)

Pachete minime:
- `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`
- `FluentAssertions`
- `Microsoft.Playwright`, `Microsoft.Playwright.NUnit`
- `Allure.NUnit`
- `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Configuration.Json`, `Microsoft.Extensions.Configuration.EnvironmentVariables`
- `Polly`

---

[Creat]
## 5) Configurare medii și secrete

### Fișiere config
- `config/appsettings.json` (default)
- `config/appsettings.prod.json` (override CI/prod)
- `config/appsettings.local.template.json` (template local, fără secrete)

### Chei de configurare (exemplu)
- `Scriptube:BaseUrl`
- `Scriptube:UiBaseUrl`
- `Scriptube:ApiTimeoutSeconds`
- `Scriptube:PollingIntervalMs`
- `Scriptube:PollingTimeoutSeconds`
- `Scriptube:RetryCount`
- `Scriptube:Webhook:ListenerBaseUrl`
- `Scriptube:Credentials:Email`
- `Scriptube:Credentials:Password`

### Secrete (doar env vars / GitHub Secrets)
- `SCRIPTUBE_API_KEY`
- `SCRIPTUBE_EMAIL`
- `SCRIPTUBE_PASSWORD`
- eventual `SCRIPTUBE_BYOK_KEY` (dacă e necesar pentru scenarii)

**Regulă**: niciun secret în cod/fișiere versionate.

---

[In Proces]
## 6) Design intern framework

[Creat]
## 6.1 Layer Core
- `ConfigurationProvider`: încarcă config JSON + env vars.
- `ApiContext`: furnizează `HttpClient`, timeouts, header auth (`X-API-Key`).
- `MaskedLoggingHandler`: loghează request/response și maschează API key.
- `RetryPolicyFactory`: creează politici Polly configurabile.
- `AllureAttachmentHelper`: atașează request/response, payload-uri, screenshot-uri.

[Creat]
## 6.2 Layer API
- Câte un client per agregat:
  - `TranscriptsClient`
  - `CreditsClient`
  - `UserClient`
  - `PlansClient`
  - `UsageClient`
  - `SeoToolsClient`
  - `WebhooksClient`
- `RequestBuilder` pentru payload-uri transcript/precheck/estimate.
- `BatchPollingService` pentru `GET /transcripts/{batch_id}` până la stare finală.

[Urmeaza]
## 6.3 Layer UI (POM)
- Page Objects:
  - `LoginPage`
  - `SignupPage`
  - `DashboardPage`
  - `BatchDetailsPage`
  - `CreditsPage`
  - `PricingPage`
- `UiFlow` wrappers pentru scenarii complete (ex. login + submit batch).
- Screenshot automat la fail în teardown.

[In Proces]
## 6.4 Layer Webhooks
- `WebhookRegistrationClient`
- `WebhookDeliveryVerifier`
- `HmacSignatureVerifier` (HMAC-SHA256)
- `WebhookTestListener` (mock receiver HTTP local/extern controlat)

---

[In Proces]
## 7) Acoperire teste — plan detaliat

[Creat]
## 7.1 API — Smoke suite (rapidă)
1. `POST /tools/youtube-transcript` fără auth (happy path)
2. `GET /api/v1/user` cu API key valid
3. `GET /api/v1/credits/balance` cu API key valid
4. Flux minim E2E:
   - precheck (`tstENMAN001`)
   - submit batch
   - poll până la `completed`
   - export JSON

Tagging: `[Category("API")][Category("Smoke")]`

[Creat]
## 7.2 API — Regression suite (completă)

### Endpoints obligatorii
- `POST /api/v1/transcripts`
  - single URL, multiple URLs, playlist URL
  - `translate_to_english=true/false`
  - `use_byok=true/false`
- `GET /api/v1/transcripts/{batch_id}`
- `GET /api/v1/transcripts/{batch_id}/export` (JSON/TXT/SRT)
- `GET /api/v1/credits/balance`
- `GET /api/v1/usage`
- `GET /api/v1/plans`
- `GET /api/v1/user`
- `POST /api/v1/credits/precheck`
- `POST /api/v1/credits/estimate`
- `GET /api/v1/credits/costs`
- `GET /api/v1/credits/history`
- `POST /api/v1/transcripts/{batch_id}/cancel`
- `POST /api/v1/transcripts/{batch_id}/retry-failed`
- `POST /api/v1/transcripts/{batch_id}/rerun`
- `DELETE /api/v1/transcripts/{batch_id}`

### Fluxuri business critice
1. **Precheck → Submit → Poll → Export**
2. **Credit deduction verification** (before/after)
3. **Cancel mid-processing**
4. **Retry failed items** (`tstPRIVT001`, `tstTIMEO001`)
5. **Rerun batch** + validare rezultate

### Negative obligatorii
- no API key → 401
- invalid API key → 401
- empty URL list → validation error
- invalid URL non-YouTube → validation error
- batch id inexistent → 404
- batch alt utilizator → 404

Tagging: `[Category("API")][Category("Regression")]`

[Urmeaza]
## 7.3 UI E2E — Chromium only

Scenarii:
1. Login valid → redirect dashboard
2. Login invalid → mesaj eroare
3. Signup nou cont (cu email unic generat)
4. Signup duplicate email → eroare
5. Submit batch din dashboard
6. Batch detail: progres, itemi, status, preview transcript
7. Export transcript din batch detail
8. Credits page: balance + options
9. Pricing page: plans + prețuri vizibile

Tagging: `[Category("UI")]` + `Smoke/Regression` după criticitate.

[In Proces]
## 7.4 Webhooks

Scenarii:
1. Register webhook valid HTTPS → 201
2. Trigger test event și verificare livrare
3. Batch complete (use_byok=false) → webhook payload + HMAC valid
4. Batch complete (use_byok=true) → webhook + verificare cost diferențiat
5. Payload schema validation (`batch_id`, `status`, `items`)
6. SSRF protection (`localhost`, `192.168.x.x`, `10.x.x.x`) blocate
7. Delivery logs (`/logs`) conțin status codes
8. Available events endpoint listă validă

Tagging: `[Category("Webhook")][Category("Regression")]`, plus smoke pentru scenariile rapide.

---

[Creat]
## 8) Date de test și management date

- Se folosesc exclusiv URL-urile `tst*` din task.
- Se definește un `TestDataCatalog` central cu:
  - success videos
  - error videos
  - playlists
- Fiecare test își creează propriile batch-uri; nu reutilizează stare globală.
- Cleanup explicit unde există endpoint de delete.

---

[In Proces]
## 9) Logging și raportare (Allure)

## 9.1 Ce se atașează per test
- Request: method, URL, headers (API key masked), body
- Response: status, headers, body
- Polling timeline pentru batch-uri (status transitions)
- UI screenshot la fail
- Playwright trace/video opțional pentru regression failures

## 9.2 Publicare în GitHub Pages
- Workflow generează rezultate Allure (`allure-results`).
- Build report HTML (`allure-report`).
- Deploy pe branch `gh-pages`.
- README include link „Latest Allure Report”.

---

[Urmeaza]
## 10) CI/CD — GitHub Actions

## 10.1 Trigger-e
- `push` pe `main` → smoke
- `pull_request` → regression full
- `workflow_dispatch` cu input:
  - `area`: `api|ui|webhook|all`
  - `suite`: `smoke|regression`
  - `threads`: `1|2|4`

## 10.2 Strategie rulare
- Mapare categorii la `dotnet test --filter`:
  - area api: `Category=API`
  - area ui: `Category=UI`
  - area webhook: `Category=Webhook`
  - suite smoke/regression combinat cu `&`.
- Paralelizare:
  - `dotnet test -m:{threads}`
  - eventual `NUnit.NumberOfTestWorkers={threads}`

## 10.3 Gate-uri obligatorii
1. `dotnet restore`
2. `dotnet format --verify-no-changes`
3. `dotnet build --warnaserror`
4. `dotnet test` (filtrat conform parametri)
5. publish artifacts (logs, screenshots, allure-results)
6. deploy report

---

[Creat]
## 11) Stabilitate și retry

- Retry strict configurabil doar pe erori tranziente (timeouts, 5xx, network hiccups).
- Fără retry pe erori de business (4xx validați explicit).
- Polling cu timeout total + interval configurabil.
- Testele flaky marcate și analizate; evităm retry excesiv care ascunde bug-uri reale.

---

[Creat]
## 12) Securitate & conformitate

- Masking API key în toate log-urile.
- Fără output de credentials în console.
- `.gitignore` include `.env`, output-uri locale, playwright traces locale.
- Hook/CI verifică că nu apar secrete în commit.

---

[In Proces]
## 13) Roadmap de implementare (5 zile lucrătoare)

[Creat]
## Ziua 1 — Fundament framework
- Curățare proiect inițial + structură foldere/proiecte.
- Adăugare pachete NuGet, config loading, secret loading.
- Implementare HttpClient wrapper + logging + masking.
- Setup Allure și fixture de bază pentru teste.

[Creat]
## Ziua 2 — API core + smoke
- Implementare clienți API principali.
- Implementare builders + polling service.
- Implementare smoke suite API + primele negative.
- Validare locală + stabilizare.

[In Proces]
## Ziua 3 — API regression + Webhooks
- [Creat] Completare endpoint-uri API restante.
- [Creat] Fluxuri cancel/retry/rerun/delete.
- Implementare webhook registration/test/logs.
- Implementare verificare HMAC + SSRF negative.

[Urmeaza]
## Ziua 4 — UI E2E + hardening
- Implementare Page Objects și flow-uri UI din task.
- Screenshot la fail, trace opțional.
- Tagging complet Smoke/Regression/Area.
- Reducere flaky și optimizare waits.

[Urmeaza]
## Ziua 5 — CI/CD + documentație finală
- Workflow complet parametrizat (area/suite/threads).
- Publicare Allure în GitHub Pages.
- Pre-commit checks și scripturi locale.
- README final: setup local, rulare CI, link raport live.

---

[Urmeaza]
## 14) Definiție de Done (DoD)

Un item este „Done” când:
- Testele trec local și în CI conform suitei țintă.
- Categoriile sunt aplicate corect (`API/UI/Webhook`, `Smoke/Regression`).
- Request/response logs apar în Allure.
- UI failures au screenshot atașat.
- Nu există hardcodări de secrete/date sensibile.
- `dotnet format` și `dotnet build --warnaserror` trec.
- README și workflow-urile sunt actualizate.

---

[Creat]
## 15) Riscuri și mitigare

1. **Flaky UI din cauza sincronizării**
   - Mitigare: explicit waits pe stări UI, selectors stabili, retry limitat.

2. **Durată mare regression în CI**
   - Mitigare: separare smoke/regression + paralelizare configurabilă.

3. **Dependență de mediu webhook receiver**
   - Mitigare: listener controlat + fallback pe endpoint test event.

4. **Consum credite / inconsistențe de cost**
   - Mitigare: folosire exclusivă date `tst*`, verificări delta cu toleranță documentată.

---

[Urmeaza]
## 16) Extensii bonus (după MVP)

- 2-3 scenarii Reqnroll/SpecFlow pentru demonstrat BDD.
- Contract testing OpenAPI pe 1-2 endpoint-uri.
- Performance smoke simplu pe endpoint rapid (assert prag timp răspuns).

---

[Creat]
## 17) Prioritizare MVP

Dacă timpul devine limitat, ordinea minimă de livrare:
1. API smoke + flux E2E critic + negative cheie
2. CI parametrizat + Allure Pages
3. Webhook scenarii critice (register + batch complete + HMAC)
4. UI smoke (login + submit + batch detail + export)
5. Regression complet și bonus-uri
