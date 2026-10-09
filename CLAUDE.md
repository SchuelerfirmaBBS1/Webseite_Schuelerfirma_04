# CLAUDE.md

Leitfaden für Claude-Agents, die in diesem Repository arbeiten.

## Projekt

Webseite der Schülerfirma der BBS1 Lüneburg. Öffentliche Seite plus Admin-Bereich. Geplante Funktionen im Backend (siehe `README.md`):

- Admin-Bereich mit Login
- Termine anlegen/ändern
- Eingegangene Kontaktformulare einsehen
- E-Mail-Adresse ändern, an die Kontaktformulare weitergeleitet werden

Das Datenmodell ist in `documentation/Models.drawio` beschrieben (draw.io-XML). Vor Änderungen an Entities dort nachsehen und das Diagramm bei Modelländerungen mit aktualisieren.

## Tech-Stack und Struktur

| Bereich   | Technik                                                                   | Ordner           |
| --------- | ------------------------------------------------------------------------- | ---------------- |
| Frontend  | Vue 3 (`<script setup lang="ts">`), TypeScript, Vite, Tailwind CSS 4, Pinia, Vue Router | `frontend/`      |
| Backend   | C# / ASP.NET Core (Web API), Entity Framework Core                        | `backend/` (noch nicht angelegt) |
| Datenbank | MySQL (über EF Core, Provider z. B. `Pomelo.EntityFrameworkCore.MySql`)  | –                |
| Doku      | draw.io-Diagramme                                                         | `documentation/` |

Das Backend existiert noch nicht. Wenn es angelegt wird: unter `backend/` mit einer `.sln`, einem Web-API-Projekt und einem separaten Testprojekt (xUnit). Danach diese Datei um die konkreten Pfade und Befehle ergänzen.

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

### Backend (sobald vorhanden, `cd backend`)

```bash
dotnet build
dotnet test
dotnet run --project <WebApi-Projekt>
dotnet ef migrations add <Name> --project <Projekt>
dotnet ef database update --project <Projekt>
```

## Konventionen

### Frontend

- Komponenten als Single-File-Components mit `<script setup lang="ts">`, Composition API.
- Styling mit Tailwind-Utility-Klassen; eigenes CSS nur, wenn Tailwind nicht reicht. Tailwind 4 wird über `@import "tailwindcss";` in `src/assets/main.css` und das Vite-Plugin eingebunden (keine `tailwind.config.js`).
- Import-Alias `@` → `frontend/src`.
- Globaler Zustand in Pinia-Stores (`src/stores/`), Routen in `src/router/index.ts`.
- Prettier (`npm run format`) ist optional und wird in der CI nicht geprüft. Keine reinen Formatierungsänderungen an Dateien vornehmen, die man sonst nicht anfasst. Vor dem Commit `npm run lint`, `npm test` und `npm run build` ausführen.

### Backend

- ASP.NET Core Web API mit Controllern (oder Minimal APIs – konsistent bei einer Variante bleiben).
- EF Core mit MySQL; Schemaänderungen ausschließlich über Migrations, nie manuell an der Datenbank.
- Connection-Strings und Secrets (DB-Passwort, SMTP-Zugangsdaten) nie committen – lokal über `dotnet user-secrets` bzw. Umgebungsvariablen, in `appsettings.json` nur Platzhalter.
- XML-Doc-Comments für öffentliche Klassen und Methoden.
- Admin-Endpunkte müssen authentifiziert sein; Eingaben aus dem Kontaktformular validieren.

### Sprache

- Kommunikation mit dem User, Projektdoku (README, CLAUDE.md) und UI-Texte der Webseite: **Deutsch**.
- Code (Bezeichner), Commit-Messages und PR-Titel/-Beschreibungen: **Englisch**.

## Git-Workflow

- `main` ist der stabile Stand, `dev` der Integrationsbranch.
- Neue Features entstehen auf `feature/<name>`-Branches, abgezweigt von `dev`, und gehen per Pull Request zurück nach `dev`.
- Commit-Messages nach Conventional Commits, z. B. `feat(contact-form): add submission endpoint`.
- Für neue Features den Skill **`feature-workflow`** (`.claude/skills/feature-workflow/SKILL.md`) verwenden: Sync mit `dev` → Feature-Branch → Rückfragen → Plan freigeben lassen → kleine Commits → Tests → Doku → PR gegen `dev`.
- Keine destruktiven Git-Befehle (`reset --hard`, `clean`, Force-Push) ohne Rückfrage.

## CI und Releases

- `.github/workflows/ci.yml` läuft bei Pushes und PRs auf `main`/`dev`. Die CI prüft nur, ob alles technisch funktioniert, nicht den Code-Stil. Frontend: oxlint und eslint (ohne `--fix`), Type-Check, `npm test`, Build. Backend: `dotnet build` + `dotnet test`, sobald unter `backend/` ein Projekt liegt. Vor dem Push dieselben Checks lokal ausführen.
- `.github/workflows/release.yml` nutzt release-please mit getrennter Config pro Branch:
  - `dev` → `release-please-config.dev.json` / `.release-please-manifest.dev.json`, Minor-Bump (v1.1.0 → v1.2.0), kein CHANGELOG, nur GitHub-Release-Notes.
  - `main` → `release-please-config.json` / `.release-please-manifest.json`, Major-Bump (v1.2.0 → v2.0.0), schreibt `CHANGELOG.md`.
  - Nach einem Release auf `main` merged der Workflow `main` automatisch zurück in `dev` und setzt die dev-Version auf die neue Major-Version.
- Ein Release-PR entsteht nur für Conventional Commits mit sichtbarem Typ (`feat`, `fix`, `perf`, `revert`, `refactor`, `docs`, `test`, `build`, `ci`, `style`). `chore`-Commits lösen keinen Release aus.
- Manifeste, `CHANGELOG.md` und Release-PRs (`release-please--branches--*`) nicht von Hand bearbeiten.
