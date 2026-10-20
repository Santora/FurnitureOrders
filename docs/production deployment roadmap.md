# Plán pro uvedení do produkčního provozu (Production Readiness Roadmap)

Tento dokument shrnuje technické, bezpečnostní a infrastrukturní kroky nezbytné pro překlopení vývojové verze aplikace **Zakázkový systém pro výrobu nábytku** do plného produkčního provozu.

---

## 1. Bezpečnost a Správa přístupů (Security & Auth)

- [ ] **Autentizace a autorizace (Identity & Access Management):**
  - Implementovat ověřování uživatelů (např. pomocí ASP.NET Core Identity, Microsoft Entra ID / Azure AD nebo Auth0).
  - Zavést řízení přístupových práv (RBAC – Role-Based Access Control) napříč API endpointy i Blazor UI (např. *Administrátor*, *Obchodník*, *Technolog*, *Skladník*).
- [ ] **Správa tajných klíčů a konfiguračních dat:**
  - Odstranit z konfigurací vývojové API klíče a připojovací řetězce (`appsettings.json`).
  - Integrovat bezpečné úložiště tajných klíčů (např. Azure Key Vault, HashiCorp Vault nebo proměnné prostředí na serveru).
- [ ] **Zabezpečení API a komunikace:**
  - Vynutit HTTPS komunikaci s platnými SSL/TLS certifikáty (včetně HTTP Strict Transport Security - HSTS).
  - Zpřísnit pravidla CORS (Cross-Origin Resource Sharing) pouze pro schválené domény.
  - Nastavit **Rate Limiting** (omezení počtu požadavků) na API endpointy, zejména pro extrakci textu pomocí AI, k zamezení DoS útokům a vyčerpání kvót.

---

## 2. Databáze a Správa dat (Database Operations)

- [ ] **Oddělení migrací od běhu aplikace:**
  - Vypnout automatické spouštění `DatabaseInitializer` / EF Core migrací při startu webového API (prevence kolizí při běhu více instancí).
  - Převést spouštění migrací do CI/CD pipeline nebo dedikovaného inicializačního kroku (např. init container).
- [ ] **Vypnutí testovacích dat:**
  - Zajistit, aby `DemoDataSeeder` nebyl spouštěn v produkčním prostředí (řízeno přes `ASPNETCORE_ENVIRONMENT=Production`).
- [ ] **Strategie zálohování a obnovy:**
  - Nastavit automatické plné i rozdílové zálohy MS SQL Serveru.
  - Otestovat a zdokumentovat postup obnovy databáze (Disaster Recovery Plan).
- [ ] **Indexace a optimalizace:**
  - Provést revizi databázových indexů na cizích klíčích, kódech položek a stavových polích zakázek pro zajištění rychlých dotazů při větším objemu dat.

---

## 3. DevOps, Kontejnery a CI/CD

- [ ] **Kontejnerizace (Docker):**
  - Připravit produkční `Dockerfile` pro API i Blazor WebAssembly aplikaci s vícefázovým sestavením (multi-stage build).
- [ ] **CI/CD Pipeline:**
  - Nastavit automatizovanou pipeline (např. GitHub Actions, Azure DevOps) obsahující:
    1. Sestavení aplikace.
    2. Spuštění unit a integračních testů z `FurnitureOrders.Tests`.
    3. Analýzu kódu a bezpečnostních zranitelností (Static Analysis / Dependency Scan).
    4. Automatický deployment na produkční / staging prostředí.
- [ ] **Reverzní proxy:**
  - Nasadit produkční reverzní proxy (Nginx, IIS, YARP nebo Azure App Service ingress) pro správu SSL/TLS certifikátů, kompresi static obsahu a load balancing.

---

## 4. Monitorování, Logování a Odolnost (Observability & Resilience)

- [ ] **Centrální logování:**
  - Nahradit konzolový výstup strukturovaným logováním (např. pomocí Serilog) se směrováním do centrálního nástroje (Elasticsearch/Kibana, Seq, Azure Application Insights).
- [ ] **Sledování stavu (Health Checks):**
  - Implementovat ASP.NET Core Health Checks na `/health` (kontrola dostupnosti databáze, externích AI služeb a volného místa).
- [ ] **Odolnost externích integrací:**
  - Ošetřit komunikaci s AI službou (Anthropic API) pomocí vzorů Retry a Circuit Breaker (např. knihovna Polly), aby dočasný výpadek AI neblokoval hlavní funkce systému.

---

## 5. Zabezpečení kvality a UX (Testing & QA)

- [ ] **E2E a zátěžové testy:**
  - Doplnit základní sadu End-to-End testů (např. Playwright / Selenium) pro ověření klíčových průchodů systémem (vytvoření zakázky, změna stavu).
  - Otestovat chování při souběžném přístupu více uživatelů (Concurrency testing).
- [ ] **Uživatelská dokumentace:**
  - Připravit stručný provozní manuál pro uživatele a administrátory systému.