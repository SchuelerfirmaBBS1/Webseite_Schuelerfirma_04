# CLAUDE.md

Leitfaden für Claude-Agents, die in diesem Repository arbeiten.

## Projekt

Webseite der Schülerfirma der BBS1 Lüneburg. Öffentliche Seite plus Admin-Bereich. Geplante Funktionen im Backend (siehe `README.md`):

- Admin-Bereich mit Login
- Termine anlegen/ändern
- Eingegangene Kontaktformulare einsehen
- E-Mail-Adresse ändern, an die Kontaktformulare weitergeleitet werden

Das Datenmodell ist in den Mermaid-Diagrammen `documentation/UMLClassDiagram.mmd` (Klassendiagramm) und `documentation/UMLEntityRelationshipDiagramm.mmd` (ER-Diagramm) beschrieben. Vor Änderungen an Entities dort nachsehen und bei Modelländerungen beide Diagramme mit aktualisieren. Was für die Datenbank-Anbindung (EF Core/MySQL) gilt, steht in `documentation/Datenbank-Implementierung.md`.

## Tech-Stack und Struktur

| Bereich   | Technik                                                                   | Ordner           |
| --------- | ------------------------------------------------------------------------- | ---------------- |
| Frontend  | Vue 3 (`<script setup lang="ts">`), TypeScript, Vite, Tailwind CSS 4, Pinia, Vue Router | `frontend/`      |
| Backend   | C# / ASP.NET Core (Web API), Entity Framework Core                        | `backend/`       |
| Datenbank | MySQL (über EF Core, Provider z. B. `Pomelo.EntityFrameworkCore.MySql`)  | –                |
| Doku      | Mermaid-Diagramme (`.mmd`)                                                | `documentation/` |

Das Backend liegt unter `backend/` (.NET 10, Solution `Schuelerfirma.slnx`):

- `backend/src/Schuelerfirma.Api/` – Web-API mit Controllern. Ordner: `Domain/` (Entities), `Application/` (Services, Interfaces), `Infrastructure/` (technische Umsetzung, z. B. `Http/` für Security-Header und Proxy-Handling), `Controllers/`, `Contracts/` (Request-/Response-DTOs).
- `backend/tests/Schuelerfirma.Api.Tests/` – xUnit; Integrationstests laufen über `WebApplicationFactory<Program>` (`Integration/`).

## Befehle

### Frontend (`cd frontend`)

Node-Version: siehe `frontend/.nvmrc` (24).

```bash
npm install          # Abhängigkeiten installieren
npm run dev          # Dev-Server (Vite)
npm run build        # Type-Check (vue-tsc) + Production-Build
npm run type-check   # nur Type-Check
npm test             # Unit-Tests (Vitest, einmaliger Lauf)
npm run test:watch   # Vitest im Watch-Modus
npm run lint         # oxlint + eslint (mit --fix)
npm run format       # prettier auf src/
```

Tests liegen in `__tests__/`-Ordnern neben dem getesteten Code (`*.spec.ts`), z. B. `src/stores/__tests__/counter.spec.ts`. Vitest läuft mit `jsdom`, Komponenten werden mit `@vue/test-utils` gemountet.

### Backend (`cd backend`)

```bash
dotnet build
dotnet test
dotnet run --project src/Schuelerfirma.Api
dotnet ef migrations add <Name> --project src/Schuelerfirma.Api   # erst mit EF Core (#22)
dotnet ef database update --project src/Schuelerfirma.Api           # erst mit EF Core (#22)
```

## Konventionen

### Frontend

- Komponenten als Single-File-Components mit `<script setup lang="ts">`, Composition API.
- Styling mit Tailwind-Utility-Klassen; eigenes CSS nur, wenn Tailwind nicht reicht. Tailwind 4 wird über `@import "tailwindcss";` in `src/assets/main.css` und das Vite-Plugin eingebunden (keine `tailwind.config.js`).
- Import-Alias `@` → `frontend/src`.
- Globaler Zustand in Pinia-Stores (`src/stores/`), Routen in `src/router/index.ts`.
- Prettier (`npm run format`) ist optional und wird in der CI nicht geprüft. Keine reinen Formatierungsänderungen an Dateien vornehmen, die man sonst nicht anfasst. Vor dem Commit `npm run lint`, `npm test` und `npm run build` ausführen.

### Backend

- ASP.NET Core Web API mit Controllern (keine Minimal APIs).
- Frontend und API laufen unter derselben Domain (Reverse Proxy); deshalb gibt es kein CORS.
- Datenzugriff nur über die Repository-Interfaces in `Application/Abstractions/` (nie direkt über einen `DbContext` in Services/Controllern). Bis die EF-Core-Implementierung (#22) existiert, laufen in Development die In-Memory-Repositories aus `Infrastructure/InMemory/`; außerhalb von Development startet die App ohne registrierte Repositories nicht. Neue Repositories bekommen abstrakte Contract-Tests in `tests/.../Persistence/`, die für jede Implementierung abgeleitet werden.
- Passwörter nur über `IPasswordHasher<User>` hashen (210.000 PBKDF2-Iterationen, siehe `PasswordSecurityExtensions`) und neue Passwörter immer mit `PasswordPolicy` prüfen; temporäre Passwörter kommen aus `ITemporaryPasswordGenerator`. Die Richtlinie ist rein statisch (keine externen Dienste); die Liste häufiger Passwörter liegt in `Resources/common-passwords.txt` (Quelle: `Resources/common-passwords.README.md`). Mindestlänge über `Auth:Password:MinLength` (12–128).
- Login: serverseitige Sessions (`SessionTicketStore` auf Basis von `IUserSessionRepository`), Cookie `__Host-sf.session` (HttpOnly, Secure, SameSite=Strict). Die Fallback-Policy verlangt für jeden Endpunkt eine Anmeldung; öffentliche Endpunkte werden ausdrücklich mit `[AllowAnonymous]` markiert. Login-Fehler sind immer identisch (`401`), nie zwischen unbekanntem Benutzer, falschem Passwort, Sperre oder Deaktivierung unterscheiden. Eigene Optionsklassen nicht wie Typen aus `Microsoft.AspNetCore.*` benennen (`SessionOptions`, `LockoutOptions` und `PasswordOptions` kollidieren), daher `Login…Options`/`PasswordPolicyOptions`.
- CSRF: Ein globaler Filter (`CsrfValidationFilter`) prüft bei jedem POST/PUT/PATCH/DELETE das Token aus dem Header `X-XSRF-TOKEN`, auch bei anonymen Endpunkten. Nicht abschalten; `[IgnoreAntiforgeryToken]` nur mit Begründung. Ändert sich die Identität im Request (Login, Logout), neues Token mit `IAntiforgery.IssueCsrfToken(HttpContext, neuerPrincipal)` ausstellen.
- Rate Limiting: Öffentliche schreibende Endpunkte (z. B. Kontaktformular) und alles, was Passwörter prüft, bekommen `[EnableRateLimiting(...)]` mit einer Policy aus `RateLimitPolicies` (`auth`, `contact`). Gezählt wird pro Client-IP (IPv6 pro /64); neue Policies in `RateLimitingExtensions` mit Standardwerten und Konfigurationsabschnitt anlegen.
- Konfiguration: `Auth:*` (Passwort, Sperre, Session), `RateLimiting:*` (Limits), `Authorization:RolePermissions` (Rollenrechte), `Bootstrap:*` (erster Admin, nur über user-secrets/Umgebungsvariablen), `DataProtection:KeysPath` (Pflicht außerhalb von Development). Details im README.
- Zeitpunkte sind UTC (`DateTime`); Repositories lesen keine Uhr, der Aufrufer übergibt die Zeit.
- Fehlerantworten sind ProblemDetails ohne Stacktraces. Zeitabhängige Logik nutzt den injizierten `TimeProvider`, nicht `DateTime.UtcNow`.
- EF Core mit MySQL; Schemaänderungen ausschließlich über Migrations, nie manuell an der Datenbank.
- Connection-Strings und Secrets (DB-Passwort, SMTP-Zugangsdaten) nie committen – lokal über `dotnet user-secrets` bzw. Umgebungsvariablen, in `appsettings.json` nur Platzhalter.
- XML-Doc-Comments für öffentliche Klassen und Methoden.
- API-Bereiche: Öffentliche Endpunkte erben von `PublicApiController` (`/api/public/...`, anonym), Verwaltungsendpunkte von `AdminApiController` (`/api/admin/...`, nicht cachebar). Jeder Verwaltungsendpunkt bekommt `[HasPermission(Permissions.…)]`, nie `[AllowAnonymous]`; öffentliche schreibende Endpunkte brauchen `[EnableRateLimiting]`. Andere Pfade außer `/api/auth` gibt es nicht. `ApiAreaRulesTests` prüft das für alle Endpunkte.
- Öffentliche Endpunkte bekommen eigene Response-DTOs, nie dieselben wie der Verwaltungsbereich – so wird ein internes Feld nicht versehentlich öffentlich.
- Berechtigungen: Namen stehen im Katalog `Application/Auth/Permissions.cs`, die Zuordnung zu Rollen in `appsettings.json` unter `Authorization:RolePermissions` (nie im Code). Eine neue Berechtigung in beiden eintragen. Mehrere `[HasPermission]` an einem Endpunkt müssen alle erfüllt sein. Solange `User.MustChangePassword` gesetzt ist (Claim `sf:must_change_password`), gewährt `[HasPermission]` nichts; deshalb nie Funktionen für angemeldete Benutzer ohne `[HasPermission]` bauen, außer den Endpunkten unter `/api/auth`.
- Passwortwechsel beendet alle Sessions des Benutzers inklusive der aktuellen; der Benutzer meldet sich neu an. Grund: Der Cookie-Handler kann innerhalb eines angemeldeten Requests keinen neuen Session-Schlüssel ausstellen (er erneuert bei `SignInAsync` den alten).
- Benutzerverwaltung: Fachlogik und Schutzregeln liegen in `UserAdministrationService` (`Application/Users/`), der Controller übersetzt nur in HTTP. Benutzer nie löschen, nur deaktivieren; jede Admin-Aktion mit handelndem Benutzer loggen, temporäre Passwörter nie. Wer `users.manage` hat, ergibt sich aus der Konfiguration, nie aus dem Rollennamen.
- Eingaben aus dem Kontaktformular validieren.

### Sprache

- Kommunikation mit dem User, Projektdoku (README, CLAUDE.md) und UI-Texte der Webseite: **Deutsch**.
- Code (Bezeichner), Commit-Messages und PR-Titel/-Beschreibungen: **Englisch**.

## Git-Workflow

- `main` ist der stabile Stand, `dev` der Integrationsbranch.
- Neue Features entstehen auf `feature/<name>`-Branches, abgezweigt von `dev`, und gehen per Pull Request zurück nach `dev`.
- Commit-Messages nach Conventional Commits, z. B. `feat(contact-form): add submission endpoint`.
- Für neue Features den Skill **`feature-workflow`** (`.claude/skills/feature-workflow/SKILL.md`) verwenden: Sync mit `dev` → Feature-Branch → Rückfragen → Plan freigeben lassen → Umsetzung → Tests → Doku → Vorschlag einer logischen Commit-Reihenfolge. Der Agent committet, pusht und eröffnet **keinen** PR; das macht der User selbst.
- Keine destruktiven Git-Befehle (`reset --hard`, `clean`, Force-Push) ohne Rückfrage.

## CI und Releases

- `.github/workflows/ci.yml` läuft bei Pushes und PRs auf `main`/`dev`. Die CI prüft nur, ob alles technisch funktioniert, nicht den Code-Stil. Frontend: oxlint und eslint (ohne `--fix`), Type-Check, `npm test`, Build. Backend: `dotnet build` + `dotnet test`, sobald unter `backend/` ein Projekt liegt. Vor dem Push dieselben Checks lokal ausführen.
- `.github/workflows/release.yml` nutzt release-please mit getrennter Config pro Branch:
  - `dev` → `release-please-config.dev.json` / `.release-please-manifest.dev.json`, Minor-Bump (v1.1.0 → v1.2.0), kein CHANGELOG, nur GitHub-Release-Notes.
  - `main` → `release-please-config.json` / `.release-please-manifest.json`, Major-Bump (v1.2.0 → v2.0.0), schreibt `CHANGELOG.md`.
  - Nach einem Release auf `main` merged der Workflow `main` automatisch zurück in `dev` und setzt die dev-Version auf die neue Major-Version.
- Ein Release-PR entsteht nur für Conventional Commits mit sichtbarem Typ (`feat`, `fix`, `perf`, `revert`, `refactor`, `docs`, `test`, `build`, `ci`, `style`). `chore`-Commits lösen keinen Release aus.
- Manifeste, `CHANGELOG.md` und Release-PRs (`release-please--branches--*`) nicht von Hand bearbeiten.
