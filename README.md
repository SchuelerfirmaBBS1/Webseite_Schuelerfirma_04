# Webseite_Schuelerfirma_04
Webseite für die Schülerfirma der BBS1 Lüneburg

Frontend: Vue/Tailwind

Backend: C# ASP.NET Core 

Datenbank: MySQL 8.4 über EF Core

Planung Backend:
Admin-Bereich
Termine ändern/einstellen
Kontaktformular einsehen
E-Mail Adresse ändern, an die die Kontaktformulare gehen

## Lokal starten: welcher Weg wofür?

Es gibt drei Arten, das Projekt auf dem eigenen Rechner zu starten:

| Ziel | Weg | Adresse im Browser | Anleitung |
|---|---|---|---|
| Backend entwickeln, API über die **Swagger UI** testen | `dotnet run` + MySQL aus der `compose.yaml` im Repo-Root | `https://localhost:7205/swagger` | „Backend starten“ und „API-Testoberfläche“ |
| **Frontend** entwickeln (Hot Reload) | `npm run dev` | `http://localhost:5173` | „Frontend starten“ |
| **Alles zusammen** so wie auf dem Server (Proxy, Frontend-Build, API, Datenbank) | Docker Compose mit `deploy/compose.yaml` | `https://localhost:8443` | „Deployment (Docker)“ → „Lokal ausprobieren“ |

**Die Swagger UI gibt es nur beim ersten Weg.** Im Docker-Setup läuft die API als Production, dort sind `/swagger` und `/openapi` abgeschaltet (der Proxy liefert unter `/swagger` die Vue-Seite aus).

## Frontend starten

Voraussetzung: Node.js in der Version aus `frontend/.nvmrc` (24).

```bash
cd frontend
npm install       # einmalig und nach Änderungen an package.json
npm run dev       # Dev-Server, Adresse steht in der Ausgabe (meist http://localhost:5173)
```

Vor einem Commit `npm run lint`, `npm test` und `npm run build` ausführen.

## Backend starten

Voraussetzung: .NET 10 SDK.

```bash
cd backend
dotnet run --project src/Schuelerfirma.Api
dotnet test
```

`dotnet run` startet die API unter `https://localhost:7205` (zusätzlich `http://localhost:5077`). Schreibende Anfragen funktionieren nur über HTTPS, weil die CSRF-Cookies `Secure` sind; einmalig `dotnet dev-certs https --trust` ausführen. Ein Dev-Server des Frontends muss `/api` deshalb an `https://localhost:7205` weiterleiten (in `frontend/vite.config.ts` noch nicht eingerichtet, das Frontend ruft die API bisher nicht auf).

### Datenbank einrichten

Das Backend speichert Benutzer und Sessions in MySQL, sobald der Connection-String `ConnectionStrings:Default` gesetzt ist. Ohne ihn läuft es in Development mit einem Speicher im Arbeitsspeicher: Nach jedem Neustart sind alle Daten weg. Außerhalb von Development startet es ohne Connection-String nicht.

1. **MySQL starten.** Im Repository liegt eine `compose.yaml` mit MySQL 8.4 (nur für die Entwicklung, die Passwörter darin sind keine Geheimnisse). Sie bindet Port 3306 nur an `127.0.0.1`, die Datenbank ist also nur vom eigenen Rechner aus erreichbar:
   ```bash
   docker compose up -d     # oder: podman compose up -d (braucht podman-compose)
   ```
2. **Connection-String hinterlegen** (passend zur `compose.yaml`):
   ```bash
   cd backend/src/Schuelerfirma.Api
   dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=schuelerfirma;User=schuelerfirma_app;Password=development-password"
   ```
3. **Tabellen anlegen.** Die App wendet Migrations nicht selbst an. Nach dem ersten Einrichten und nach jedem Pull mit neuen Migrations:
   ```bash
   dotnet tool install --global dotnet-ef   # einmalig
   cd backend
   dotnet ef database update --project src/Schuelerfirma.Api
   ```
   `dotnet ef` liest den Connection-String aus den user-secrets bzw. aus `ConnectionStrings__Default`. Ohne Migration bricht der Start ab, weil Tabellen und Rollen fehlen.

Auf dem Server kommt der Connection-String aus der Umgebungsvariable `ConnectionStrings__Default`. Neue Migrations werden **vor** dem Start der neuen Version angewendet (`dotnet ef database update` oder ein Migration-Bundle über `dotnet ef migrations bundle`); im Docker-Setup erledigt das der Dienst `migrate` (siehe „Deployment (Docker)“). Hintergründe zum Mapping: [documentation/Datenbank-Implementierung.md](documentation/Datenbank-Implementierung.md).

### Tests

`dotnet test` startet für die Repository-Tests gegen MySQL einen eigenen, kurzlebigen MySQL-Container (Testcontainers). Dafür muss Docker oder Podman laufen; die Datenbank aus der `compose.yaml` wird nicht angefasst. Die übrigen Integrationstests laufen immer im Arbeitsspeicher, auch wenn ein Connection-String gesetzt ist. Mit rootless Podman:

```bash
systemctl --user enable --now podman.socket
export DOCKER_HOST=unix://$XDG_RUNTIME_DIR/podman/podman.sock
export TESTCONTAINERS_RYUK_DISABLED=true
```

### Ersten Admin einrichten

Beim Start legt das Backend einen Administrator an, falls noch keiner existiert. Die Zugangsdaten gehören **nicht** ins Repository, sondern in `dotnet user-secrets` (lokal) bzw. in Umgebungsvariablen (Server):

```bash
cd backend/src/Schuelerfirma.Api
dotnet user-secrets set "Bootstrap:AdminUsername" "admin"
dotnet user-secrets set "Bootstrap:AdminEmail" "admin@example.org"
dotnet user-secrets set "Bootstrap:AdminPassword" "<mindestens 12 Zeichen>"
```

Auf dem Server heißen die Variablen `Bootstrap__AdminUsername`, `Bootstrap__AdminEmail` und `Bootstrap__AdminPassword`. Der Benutzername besteht aus 3–32 Zeichen (`a-z`, `0-9`, `.`, `_`, `-`). Das Passwort muss die Passwortrichtlinie erfüllen (mindestens 12 Zeichen mit Groß- und Kleinbuchstabe, Ziffer und Sonderzeichen, kein häufiges Passwort), sonst startet das Backend nicht. **Achtung:** Ältere Startpasswörter in euren user-secrets ohne Ziffer oder Sonderzeichen müssen angepasst werden. Es muss beim ersten Login geändert werden; danach `Bootstrap:AdminPassword` aus der Konfiguration entfernen. Ohne Datenbank (siehe „Datenbank einrichten“) liegen alle Daten nur im Arbeitsspeicher, und der Admin wird bei jedem Start neu angelegt.

### API-Testoberfläche (nur Development)

Zum Ausprobieren der API ohne Frontend und ohne `curl` gibt es in Development eine Testoberfläche (Swagger UI):

- Oberfläche: `https://localhost:7205/swagger`
- API-Beschreibung (OpenAPI): `https://localhost:7205/openapi/v1.json`

Die Oberfläche zeigt alle Endpunkte mit Beschreibung (aus den XML-Kommentaren) und möglichen Antworten, gruppiert nach `auth`, `admin` und `public`. Alle Dateien kommen aus dem NuGet-Paket, es wird nichts von fremden Servern geladen. In allen anderen Umgebungen gibt es beide Pfade nicht (Antwort wie bei jedem unbekannten Pfad).

Ablauf:

1. Backend mit dem `https`-Profil starten (Standard bei `dotnet run`). Über `http://localhost:5077` funktioniert die Anmeldung nicht, weil die Cookies `Secure` sind. Das Entwicklungszertifikat einmalig mit `dotnet dev-certs https --trust` vertrauen.
2. `GET /api/auth/csrf` ausführen. Danach schickt die Oberfläche das CSRF-Token bei jedem POST, PUT, PATCH und DELETE automatisch im Header `X-XSRF-TOKEN` mit (so wie später das Frontend).
3. `POST /api/auth/login` mit den Zugangsdaten (siehe „Ersten Admin einrichten“). Das Session-Cookie schickt der Browser von selbst mit.
4. Endpunkte testen. `GET /api/auth/me` zeigt den angemeldeten Benutzer und seine Berechtigungen. Solange das temporäre Passwort nicht geändert ist, antworten alle Verwaltungsendpunkte mit `403`.
5. `POST /api/auth/logout` beendet die Session.

Antwortet ein schreibender Endpunkt mit `400` „Sicherheitstoken fehlt oder ist ungültig“, Schritt 2 wiederholen. Nach fünf Login-Versuchen pro Minute antwortet die API mit `429`.

Ausführliche Anleitung mit Testfällen und Fehlersuche: [documentation/API-Testoberflaeche.md](documentation/API-Testoberflaeche.md).

### Konfiguration

| Schlüssel | Standard | Bedeutung |
|---|---|---|
| `ConnectionStrings:Default` | – | Verbindung zur MySQL-Datenbank (siehe „Datenbank einrichten“); nur über user-secrets bzw. `ConnectionStrings__Default`, nie in `appsettings.json`. **Außerhalb von Development Pflicht.** |
| `Auth:Password:MinLength` | 12 | Mindestlänge neuer Passwörter (12–128) |
| `Auth:Password:RequireUppercase` / `RequireLowercase` / `RequireDigit` / `RequireSpecialCharacter` | `true` | Neue Passwörter brauchen mindestens einen Groß- und einen Kleinbuchstaben, eine Ziffer und ein Sonderzeichen (alles außer Buchstabe, Ziffer und Leerzeichen; Umlaute zählen als Buchstaben) |
| `Auth:Lockout:MaxFailedAttempts` | 5 | Fehlversuche bis zur Kontosperre |
| `Auth:Lockout:Duration` | `00:15:00` | Dauer der Kontosperre |
| `Auth:Session:IdleTimeout` | `00:30:00` | Session endet nach so langer Inaktivität |
| `Auth:Session:MaxLifetime` | `08:00:00` | Session endet spätestens so lange nach dem Login |
| `Auth:Session:CleanupInterval` | `00:10:00` | Abstand, in dem abgelaufene Sessions gelöscht werden |
| `DataProtection:KeysPath` | – | Ordner für die Schlüssel des Session-Cookies; **außerhalb von Development Pflicht**. Der Ordner muss für das Backend beschreibbar und für alle anderen unlesbar sein (die Schlüssel liegen unverschlüsselt dort); ohne ihn wären nach jedem Neustart alle abgemeldet. |
| `ForwardedHeaders:KnownProxies` | leer | IP-Adressen der Reverse Proxys, denen `X-Forwarded-*` vertraut wird |
| `RateLimiting:Auth:PermitLimit` / `:Window` | 5 / `00:01:00` | Login-Versuche pro Client-IP und Zeitfenster |
| `RateLimiting:Contact:PermitLimit` / `:Window` | 10 / `00:10:00` | Absendungen öffentlicher Formulare (Kontaktformular) pro Client-IP und Zeitfenster; bewusst großzügig, weil sich auf der Messe viele Besucher eine IP teilen |
| `Authorization:RolePermissions` | siehe unten | Welche Rolle welche Berechtigungen hat |
| `Images:MaxFileSizeBytes` | 10485760 (10 MiB) | Größte Bilddatei beim Upload und nach der Verarbeitung (höchstens 16777215, Größe der Spalte) |
| `Images:MaxRequestSizeBytes` | 31457280 (30 MiB) | Größte Upload-Anfrage (alle Dateien zusammen); mindestens `MaxFileSizeBytes`. Der Proxy (`MAX_REQUEST_BODY` in `deploy/.env`) muss etwas mehr erlauben. |
| `Images:MaxFilesPerRequest` | 10 | Höchstzahl Dateien pro Upload |
| `Images:MaxDimension` | 2000 | Längste Kante eines gespeicherten Bilds in Pixeln; größere Bilder werden verkleinert |
| `Images:MaxSourcePixels` | 40000000 | Höchstzahl Pixel (Breite × Höhe) eines hochgeladenen Bilds; schützt vor „Dekompressionsbomben“ |

### Login-API

- `GET /api/auth/csrf` → `204`, setzt die CSRF-Cookies (siehe unten); ohne Anmeldung erreichbar.
- `POST /api/auth/login` mit `{ "username": "...", "password": "..." }` → `204` und Cookie `__Host-sf.session`, sonst `401`. Alle Fehlerfälle (unbekannter Benutzer, falsches Passwort, gesperrtes oder deaktiviertes Konto) liefern dieselbe Antwort. Höchstens 5 Versuche pro Minute und IP, danach `429` mit `Retry-After`.
- `POST /api/auth/logout` → `204`, beendet die Session auf dem Server.
- `GET /api/auth/me` → angemeldeter Benutzer (`userId`, `username`, `email`, `role`, `permissions`, `mustChangePassword`), ohne Login `401`. `permissions` sind die Berechtigungen, die der Benutzer gerade nutzen kann (alphabetisch, `*` bereits aufgelöst; leer, solange `mustChangePassword` gesetzt ist); das Frontend blendet damit Menüpunkte und Buttons aus, die API prüft trotzdem selbst.
- `POST /api/auth/change-password` mit `{ "currentPassword": "...", "newPassword": "..." }` → `204`. Danach sind **alle** Sessions des Benutzers beendet, auch die aktuelle; er meldet sich mit dem neuen Passwort neu an. Fehler: `400` mit der Meldung am Feld (`errors.CurrentPassword` bzw. `errors.NewPassword`, z. B. Passwortrichtlinie verletzt oder neues gleich altem Passwort). Ein falsches aktuelles Passwort zählt zur Kontosperre; löst es die Sperre aus, kommt `401` („Konto gesperrt“) und alle Sessions enden. Teilt sich das Limit von 5 Versuchen pro Minute mit dem Login.

### Temporäre Passwörter

Der erste Admin (und später jeder Benutzer mit einem vom Admin vergebenen Passwort, #36) muss das Passwort nach der ersten Anmeldung ändern. Solange `mustChangePassword` in `/api/auth/me` `true` ist, funktionieren nur `/api/auth/me`, `/api/auth/change-password` und `/api/auth/logout`; alle Verwaltungsendpunkte antworten mit `403`. Ablauf im Frontend: nach dem Login `/me` abfragen → bei `mustChangePassword` direkt das Formular „Passwort ändern“ zeigen → nach `204` „Passwort geändert, bitte neu anmelden“ und zurück zum Login.
- Alle Endpunkte außer den ausdrücklich mit `[AllowAnonymous]` markierten verlangen eine Anmeldung; das gilt auch für unbekannte Pfade (`401` statt `404`).

### API-Bereiche und Rollenrechte

Die API ist in drei Bereiche geteilt:

| Pfad | Für | Zugriff |
|---|---|---|
| `/api/public/...` | Besucher der Webseite (Termine, Bilder, Produkte, Kontaktformular) | ohne Anmeldung |
| `/api/admin/...` | Verwaltung (Admin-Panel) | Anmeldung und passende Berechtigung, sonst `401` bzw. `403` |
| `/api/auth/...` | Login, Logout, `/me`, CSRF-Token | siehe oben |

`/api/admin` steht für den Bereich, nicht für die Rolle Admin: Auch die Rolle User arbeitet dort, was sie darf, entscheiden die Berechtigungen. Welche Rolle welche Berechtigungen hat, steht in `appsettings.json`:

```json
"Authorization": {
  "RolePermissions": {
    "Admin": [ "*" ],
    "User": [ "events.create", "events.update", "images.upload", "products.read", "products.toggle-price-visibility" ]
  }
}
```

- `*` steht für alle Berechtigungen, auch künftige. Eine Rolle ohne Eintrag hat keine Rechte.
- Verfügbare Berechtigungen: `events.create`, `events.update`, `events.delete`, `images.upload`, `images.update`, `images.delete`, `products.read`, `products.manage`, `products.toggle-price-visibility`, `contact-requests.read`, `contact-requests.update`, `contact-requests.delete`, `settings.manage`, `users.manage`.
- Ein unbekannter Name (Tippfehler) verhindert den Start. Änderungen an `appsettings.json` gelten ohne Neustart; ist die geänderte Datei fehlerhaft, bleibt die bisherige Zuordnung aktiv und im Log steht ein Fehler.
- Über Umgebungsvariablen wird jeder Listeneintrag einzeln überschrieben (`Authorization__RolePermissions__User__0=events.create`); Einträge mit höherem Index aus `appsettings.json` bleiben dabei bestehen.

### Benutzerverwaltung (Admin-Panel)

Alle Endpunkte verlangen die Berechtigung `users.manage` (Standard: nur Admin). Benutzer werden nie gelöscht, nur deaktiviert. Antworten mit Passwörtern werden nicht zwischengespeichert.

- `GET /api/admin/users`, `GET /api/admin/users/{id}` → Benutzer mit `userId`, `username`, `email`, `roleId`, `role`, `isActive`, `mustChangePassword`, `lockedUntil` (Ende einer Sperre nach Fehlversuchen, sonst `null`), `createdAt`, `lastLoginAt`. Ohne Passwort-Hash.
- `POST /api/admin/users` mit `{ "username": "...", "email": "...", "roleId": 2 }` → `201` mit `{ "user": {...}, "temporaryPassword": "..." }`. Das temporäre Passwort wird nur dieses eine Mal angezeigt; der Benutzer muss es bei der ersten Anmeldung ändern. Benutzername: 3–32 Zeichen aus `a-z 0-9 . _ -`, nicht änderbar.
- `PUT /api/admin/users/{id}` mit `{ "email": "...", "roleId": 2, "isActive": true }` → `200` mit dem Benutzer. Rollenwechsel und Deaktivierung beenden sofort alle Sessions des Benutzers.
- `POST /api/admin/users/{id}/reset-password` → `200` mit `{ "temporaryPassword": "...", "endedSessions": 1 }`; hebt eine Sperre auf und beendet alle Sessions.
- `POST /api/admin/users/{id}/revoke-sessions` → `200` mit `{ "endedSessions": 2 }`, z. B. wenn jemand am Messe-Laptop angemeldet geblieben ist.
- `GET /api/admin/roles` → Rollen mit `roleId`, `name` und `permissions` (für Auswahllisten).
- Fehler: `400` bei ungültigen Eingaben (Meldung am Feld), `404` bei unbekannter ID, `409` mit deutscher Meldung in `detail` bei vergebenem Benutzernamen oder vergebener E-Mail und bei den Schutzregeln: Niemand kann das eigene Konto deaktivieren, die eigene Rolle ändern oder das eigene Passwort hier zurücksetzen (dafür gibt es `/api/auth/change-password`), und der letzte aktive Benutzer mit `users.manage` bleibt erhalten.

### Produkte und Kategorien

Besucher sehen die Produkte ohne Anmeldung (Produktseite #23). Das Team pflegt Produkte und Kategorien im Verwaltungsbereich (#11). Produktbilder liegen in der Datenbank (Tabelle `ProductImageContents`, siehe `documentation/Datenbank-Implementierung.md` und „Bilder“ unten).

**Öffentlich** (anonym):

- `GET /api/public/products` (optional `?categoryId=`) und `GET /api/public/products/{id}` → `id`, `categoryId`, `categoryName`, `name`, `description`, `price`, `images` (`url`, `description`, `isPrimary`, sortiert). Liste nach Name sortiert.
- `GET /api/public/categories` → `id`, `name`, nur sichtbare Kategorien, nach Name sortiert.
- `GET /api/public/products/{id}/images/{imageId}/file` → die Bilddatei (`url` in `images`), mit `Cache-Control: public, max-age=31536000, immutable`. Nur für sichtbare Produkte; sonst, bei unbekannter ID oder bei einem Bild eines anderen Produkts `404`.
- Regeln, alle serverseitig in der Repository-Abfrage: Es erscheinen nur aktive Produkte aus sichtbaren Kategorien. Ausgeblendete und unbekannte Produkte antworten gleich mit `404`. Bei ausgeblendetem Preis steht in der Antwort `"price": null`; der echte Preis verlässt den Server nicht. Interne Felder wie `isActive` oder `isPriceVisible` sind nicht enthalten.

**Verwaltung:**

| Endpunkt | Berechtigung |
|---|---|
| `GET /api/admin/products` (optional `?categoryId=`), `GET /api/admin/products/{id}`, `GET /api/admin/categories` | `products.read` |
| `POST /api/admin/products`, `PUT /api/admin/products/{id}`, `DELETE /api/admin/products/{id}` | `products.manage` |
| `POST /api/admin/categories`, `PUT /api/admin/categories/{id}`, `DELETE /api/admin/categories/{id}` | `products.manage` |
| `PUT /api/admin/products/{id}/price-visibility` mit `{ "isPriceVisible": false }` → `204` | `products.toggle-price-visibility` |
| `GET /api/admin/products/{id}/images/{imageId}/file` (Bild, auch von inaktiven Produkten; nicht cachebar) | `products.read` |
| `POST /api/admin/products/{id}/images` (Upload, siehe „Bilder“) → `201`, `PUT /api/admin/products/{id}/images/{imageId}` → `200`, `DELETE /api/admin/products/{id}/images/{imageId}` → `204` | `products.manage` |

- Produkt anlegen: `{ "categoryId": 1, "name": "...", "description": "...", "price": 2.5, "isPriceVisible": true, "isActive": true }`. Ohne die beiden Schalter ist ein neues Produkt aktiv und zeigt seinen Preis. `PUT` ersetzt alle Felder; dort sind beide Schalter Pflicht.
- Kategorie anlegen oder ändern: `{ "name": "...", "isVisible": true }`. Eine ausgeblendete Kategorie blendet auch alle ihre Produkte aus.
- Die Rolle User hat standardmäßig `products.read` und `products.toggle-price-visibility`: Sie darf also nur den Preis ein- und ausblenden, sonst nichts ändern.
- Validierung: Name Pflicht (Produkt max. 200, Kategorie max. 100 Zeichen), Beschreibung max. 4000 Zeichen, Preis optional, ≥ 0 und höchstens zwei Nachkommastellen, Kategorie muss existieren. Texte sind reiner Text und werden unverändert gespeichert; das Frontend gibt sie als Text aus (nie `v-html`).
- Fehler: `400` bei ungültigen Eingaben (Meldung am Feld, z. B. `CategoryId` bei unbekannter Kategorie), `404` bei unbekannter ID, `409` bei doppeltem Kategorienamen (Groß-/Kleinschreibung zählt nicht) und beim Löschen einer Kategorie, die noch Produkte enthält.
- Bild ändern: `{ "description": "...", "sortOrder": 0, "isPrimary": true }` (alle Felder werden ersetzt; Beschreibung max. 500 Zeichen, reiner Text; Reihenfolge ≥ 0). Wird ein Bild Hauptbild, verlieren die anderen Bilder des Produkts das Kennzeichen; es gibt höchstens eins.
- Löschen eines Produkts löscht auch seine Bilder samt Bilddaten. Jede Änderung wird mit dem handelnden Benutzer geloggt.

### Bilder

Bilder werden hochgeladen, geprüft, umgewandelt und **in der Datenbank** gespeichert, nie im Dateisystem. Umgesetzt ist das für Produktbilder; Terminbilder (#14) nutzen dieselbe Verarbeitung.

- **Upload:** `multipart/form-data` mit einer oder mehreren Dateien im Feld `files` (in der Swagger UI direkt auswählbar). Neue Bilder kommen ans Ende der Reihenfolge; hat das Produkt noch kein Hauptbild, wird das erste neue Bild Hauptbild. Antwort `201` mit den neuen Bildern (`productImageId`, `fileName`, `description`, `contentType`, `length`, `sortOrder`, `isPrimary`, `url`).
- **Prüfung:** Erlaubt sind nur JPEG, PNG und WebP, erkannt am **Dateiinhalt**, nicht an Endung oder `Content-Type`. SVG wird nie angenommen: Die Bilder kommen von derselben Domain wie der Verwaltungsbereich, ein Skript im SVG liefe mit der Session des Betrachters. Ist eine Datei ungültig, wird keine gespeichert (`400`, Meldung am Feld `files` mit dem Dateinamen).
- **Umwandlung:** Das Bild wird nach der EXIF-Ausrichtung aufrecht gedreht, auf höchstens `Images:MaxDimension` Pixel verkleinert und im gleichen Format neu kodiert. Gespeichert wird nur das Ergebnis, ohne Metadaten: keine EXIF-Daten (GPS-Position aus Handyfotos!), kein XMP, keine Textfelder, kein Farbprofil (Farben werden nach sRGB umgerechnet).
- **Grenzen:** siehe `Images:*` unter „Konfiguration“. Zu große Dateien oder Anfragen → `413`; zu viele Pixel, zu viele Dateien oder beschädigte Dateien → `400`.
- **Dateiname:** Der Name vom Client wird nur als Text für die Anzeige gespeichert (ohne Pfadanteile und Steuerzeichen, max. 255 Zeichen) und nie als Pfad verwendet.
- **Bibliothek:** [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT-Lizenz) mit `SkiaSharp.NativeAssets.Linux.NoDependencies`; die native Bibliothek kommt mit dem Paket, das Docker-Image braucht keine zusätzlichen Pakete. ImageSharp wurde bewusst nicht genommen: Es bricht Release-Builds ohne Lizenzschlüssel von Six Labors ab.

### CSRF-Schutz: Hinweise fürs Frontend

Jede schreibende Anfrage (POST, PUT, PATCH, DELETE) braucht ein CSRF-Token – auch der Login und öffentliche Formulare wie das Kontaktformular. Ohne gültiges Token antwortet die API mit `400` und dem Titel „Sicherheitstoken fehlt oder ist ungültig“.

1. Beim Start der Seite und **vor jedem Login** `GET /api/auth/csrf` aufrufen. Das setzt das Cookie `XSRF-TOKEN` (für JavaScript lesbar) und ein zweites, nicht lesbares Cookie.
2. Bei jeder schreibenden Anfrage den aktuellen Wert des Cookies `XSRF-TOKEN` im Header `X-XSRF-TOKEN` mitsenden. Den Wert jedes Mal frisch aus dem Cookie lesen, nicht zwischenspeichern. axios macht das mit den Standardeinstellungen automatisch, bei `fetch` von Hand.
3. Login und Logout stellen selbst ein neues Token aus, denn das Token gehört zum angemeldeten Benutzer.
4. Ist jemand angemeldet (`GET /api/auth/me` liefert `200`), muss er sich erst abmelden, bevor sich jemand anderes anmeldet – das Token einer laufenden Session wird beim Login abgelehnt. Das Login-Formular zeigt in dem Fall „Angemeldet als … – Abmelden“.
5. Kommt `400` mit dem obigen Titel (z. B. weil die Session inzwischen abgelaufen ist), einmal `GET /api/auth/csrf` aufrufen und die Anfrage wiederholen.

Bei `429` („Zu viele Anfragen“) steht im Header `Retry-After`, nach wie vielen Sekunden es wieder geht.


## Deployment (Docker)

Auf dem Server laufen Reverse Proxy, Backend und Datenbank als Container. Die Dateien liegen in `deploy/`; die Images entstehen aus `backend/Dockerfile` und `frontend/Dockerfile`. **Von außen ist nur der Proxy auf Port 80 und 443 erreichbar.**

| Dienst | Aufgabe | Netz | Nach außen |
|---|---|---|---|
| `proxy` | Caddy: TLS-Zertifikate (Let's Encrypt), HTTP→HTTPS, `/api/*` an die API, alles andere aus dem Vue-Build | `edge` (feste IP) | 80, 443 (tcp + udp) |
| `api` | ASP.NET Core auf Port 8080, als Nicht-Root-Benutzer, Dateisystem schreibgeschützt | `edge`, `backend` | – |
| `migrate` | wendet beim Start die EF-Core-Migrations an und beendet sich; die API startet erst danach | `backend` | – |
| `db` | MySQL 8.4 | `backend` (`internal`: weder erreichbar noch mit Verbindung nach außen) | – |

### Voraussetzungen

- **VPS oder Root-Server** mit Docker Engine und Compose-Plugin (`docker compose version` ≥ 2). Bei einem **Managed Server** vorher klären, ob Docker erlaubt ist und wer Firewall und Betriebssystem-Updates pflegt; ohne Docker braucht es ein anderes Betriebskonzept (z. B. `dotnet publish` als systemd-Dienst hinter nginx).
- DNS-Eintrag (A/AAAA) der Domain zeigt auf den Server, Port 80 und 443 sind erreichbar (sonst bekommt Caddy kein Zertifikat).
- Falls der Anbieter eine Firewall vor dem Server anbietet (Cloud-Firewall, Security Groups): nur 22, 80 und 443 freigeben. Sie wirkt auch dann, wenn Docker die Firewall auf dem Server umgeht (siehe unten).

### Einrichten und starten

```bash
git clone <repo> && cd <repo>
cp deploy/.env.example deploy/.env && chmod 600 deploy/.env
nano deploy/.env                        # Domain, Datenbank-Passwörter, erster Admin
scripts/check-exposed-ports.sh          # muss "OK" melden
docker compose -f deploy/compose.yaml up -d --build
docker compose -f deploy/compose.yaml ps        # migrate: "Exited (0)", alle anderen "Up"
```

Die Secrets stehen nur in `deploy/.env` (in `.gitignore`); `deploy/.env.example` zeigt alle Variablen mit Platzhaltern. Datenbank-Passwörter mit `openssl rand -hex 24` erzeugen (kein `;`, das würde den Connection-String zerschneiden). Werte mit `$` in einfache Anführungszeichen setzen. Fehlt ein Pflichtwert, bricht `docker compose` mit einer Meldung ab.

Der erste Admin kommt aus `BOOTSTRAP_ADMIN_USERNAME`, `BOOTSTRAP_ADMIN_EMAIL` und `BOOTSTRAP_ADMIN_PASSWORD` (Regeln wie unter „Ersten Admin einrichten“). Nach dem ersten Login und dem Passwortwechsel `BOOTSTRAP_ADMIN_PASSWORD` in `deploy/.env` leeren und `docker compose -f deploy/compose.yaml up -d` ausführen.

Weitere Befehle (alle mit `-f deploy/compose.yaml`, oder vorher `cd deploy`):

```bash
docker compose logs -f api        # Logs
docker compose stop               # anhalten
docker compose start              # wieder starten
docker compose restart proxy      # nach Änderungen am Caddyfile (Caddy hat keine Admin-API)
docker compose exec api id        # zeigt uid=1654(app), nicht root
```

**Achtung:** `docker compose down -v` löscht die Volumes und damit Datenbank (samt Bildern), Session-Schlüssel und Zertifikate. Ohne `-v` bleiben sie erhalten.

### Update

```bash
git pull
scripts/check-exposed-ports.sh
docker compose -f deploy/compose.yaml up -d --build
```

`up` startet `migrate` jedes Mal neu; neue Migrations werden angewendet, bevor die neue API-Version startet. Schlägt eine Migration fehl, startet die API nicht (`docker compose logs migrate`). Alte Images danach mit `docker image prune` aufräumen.

### Daten und Sicherung

| Volume | Inhalt | Sichern |
|---|---|---|
| `schuelerfirma_db-data` | Datenbank, auch alle hochgeladenen Bilder | ja, per Dump (siehe unten) |
| `schuelerfirma_api-keys` | Schlüssel der Session-Cookies (unverschlüsselt, nur für den App-Benutzer lesbar) | ja; ohne sie sind nach einer Wiederherstellung alle abgemeldet |
| `schuelerfirma_caddy-data` | TLS-Zertifikate | optional; Caddy holt neue, Let's Encrypt begrenzt aber die Anzahl pro Woche |

```bash
cd deploy
docker compose exec -T db sh -c 'exec mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" --single-transaction --hex-blob "$MYSQL_DATABASE"' > backup-$(date +%F).sql
docker run --rm -v schuelerfirma_api-keys:/data:ro -v "$PWD":/backup docker.io/library/alpine tar czf /backup/keys-$(date +%F).tgz -C /data .
```

Die Sicherungen enthalten personenbezogene Daten und die Session-Schlüssel: nicht auf dem Server liegen lassen, verschlüsselt ablegen.

### Checkliste „Ports nach außen“

Docker setzt für veröffentlichte Ports eigene iptables-Regeln und **umgeht damit `ufw` und `firewalld`**: Ein Port, der dort gesperrt aussieht, kann trotzdem aus dem Internet erreichbar sein. Deshalb gilt in `deploy/compose.yaml`:

- [ ] Nur `proxy` hat `ports:` (80 und 443). `api` und `db` haben kein `ports:`, höchstens `expose:`.
- [ ] Ports zur Fehlersuche nur mit Loopback-Präfix (`"127.0.0.1:8080:8080"`), nie ohne, und danach wieder entfernen.
- [ ] Kein `network_mode: host`.
- [ ] `scripts/check-exposed-ports.sh` meldet „OK“ (läuft auch in der CI; braucht `jq` und Docker Compose ≥ 2, nicht `podman-compose`).
- [ ] Auf dem Server: `sudo ss -tlnp` zeigt nach außen nur `:80` und `:443` von Docker (plus SSH).
- [ ] Von einem **anderen** Rechner: `nmap -Pn -p- <server>` findet nur 22, 80 und 443; `curl http://<server>:8080` und `curl <server>:3306` schlagen fehl.

### Echte Client-IP hinter dem Proxy

Die API zählt das Rate Limiting pro Client-IP und übernimmt `X-Forwarded-For` nur vom Proxy: Der hat im Netz `edge` die feste Adresse `PROXY_IP`, die compose auch als `ForwardedHeaders:KnownProxies` an die API übergibt. Caddy verwirft `X-Forwarded-For`-Werte, die ein Client selbst mitschickt. Kollidiert das Subnetz mit einem vorhandenen Netz, `EDGE_SUBNET`, `EDGE_DYNAMIC_RANGE` und `PROXY_IP` in `deploy/.env` gemeinsam ändern (`PROXY_IP` im Subnetz, aber außerhalb des dynamischen Bereichs).

- **IPv6:** Das Netz `edge` ist reines IPv4. Kommt ein Besucher über IPv6, setzt Docker die Verbindung über seinen `docker-proxy` um, und Caddy sieht für alle IPv6-Besucher dieselbe Gateway-Adresse – sie würden sich ein Rate Limit teilen. Einfachste Abhilfe: für die Domain nur einen A-Eintrag (IPv4) anlegen, keinen AAAA-Eintrag. Soll die Seite über IPv6 erreichbar sein, IPv6 für das Netz `edge` einrichten (Docker-Doku „IPv6 networking“) und danach prüfen: Login-Fehlversuche von zwei verschiedenen IPv6-Anschlüssen dürfen sich nicht gegenseitig ins Limit (`429`) bringen; `docker compose logs api` nennt bei `429` die erkannte Client-IP.
- **Rootless Podman** (lokaler Test): Hier erscheinen alle Anfragen vom eigenen Rechner mit der IP des Proxys, weil Podman die Ports aus dem Container heraus weiterleitet. Das ist ein Effekt der Testumgebung, nicht des Setups.

### Lokal ausprobieren

So läuft das komplette Setup aus `deploy/` auf dem eigenen Rechner, z. B. um Frontend-Build, Proxy und API zusammen zu testen. Eine Swagger UI gibt es hier nicht (siehe „Lokal starten: welcher Weg wofür?“).

**1. Compose v2 bereitstellen** (einmalig)

- **Docker:** Docker Engine mit Compose-Plugin; die Befehle unten lauten dann `docker compose …`.
- **Podman** (z. B. Fedora): `podman compose` ruft meist `podman-compose` auf. Das setzt nicht alle Funktionen gleich um, die das Setup braucht (`!override`, feste Proxy-IP, Warten auf `migrate`), und wird vom Prüfskript nicht unterstützt. Stattdessen das `docker-compose`-Programm (Compose v2) über den Podman-Socket nutzen:
  ```bash
  sudo dnf install docker-compose                  # Fedora; sonst Compose v2 aus der Docker-Doku
  systemctl --user enable --now podman.socket
  export DOCKER_HOST=unix://$XDG_RUNTIME_DIR/podman/podman.sock   # in jedem neuen Terminal oder in ~/.bashrc / ~/.zshrc
  ```
  Die Befehle unten lauten dann `docker-compose …` statt `docker compose …`.

**2. `.env` anlegen**

```bash
cp deploy/.env.example deploy/.env && chmod 600 deploy/.env
```

In `deploy/.env` anpassen:

- `SITE_ADDRESS=localhost`
- `MYSQL_PASSWORD` und `MYSQL_ROOT_PASSWORD`: je ein Wert aus `openssl rand -hex 24`
- `BOOTSTRAP_ADMIN_PASSWORD`: Startpasswort des ersten Admins (mindestens 12 Zeichen mit Groß- und Kleinbuchstabe, Ziffer und Sonderzeichen; Werte mit `$` in einfache Anführungszeichen)

**3. Ports umbiegen**

Rootless Docker/Podman darf 80 und 443 nicht binden. Deshalb die Datei `deploy/compose.override.local.yaml` anlegen (`deploy/*.local.yaml` ist in `.gitignore`):

```yaml
services:
  proxy:
    ports: !override
      - "127.0.0.1:8081:80"
      - "127.0.0.1:8443:443"
```

**4. Bauen und starten**

```bash
docker compose -f deploy/compose.yaml -f deploy/compose.override.local.yaml up -d --build
docker compose -f deploy/compose.yaml ps     # migrate: "Exited (0)", alle anderen "Up"
```

Der erste Lauf dauert einige Minuten (Images bauen, MySQL initialisieren). Nach Änderungen am Code denselben `up`-Befehl erneut ausführen; er baut nur, was sich geändert hat.

**5. Im Browser öffnen:** `https://localhost:8443`

Caddy nutzt für `localhost` eine eigene Zertifizierungsstelle, deshalb warnt der Browser vor dem Zertifikat; die Warnung lokal einfach bestätigen (bei `curl` die Option `-k`, z. B. `curl -k https://localhost:8443/`). Die API ist unter derselben Adresse erreichbar (`https://localhost:8443/api/...`). Angemeldet wird sich mit `BOOTSTRAP_ADMIN_USERNAME` und `BOOTSTRAP_ADMIN_PASSWORD` aus der `.env`; das Passwort muss beim ersten Login geändert werden.

**6. Anhalten und aufräumen**

```bash
docker compose -f deploy/compose.yaml logs -f api   # Logs ansehen, falls etwas nicht startet
docker compose -f deploy/compose.yaml stop          # anhalten, Daten bleiben erhalten
docker compose -f deploy/compose.yaml down          # Container entfernen, Daten bleiben erhalten
docker compose -f deploy/compose.yaml down -v       # alles inklusive Datenbank löschen (frischer Start)
```

Unter Fedora/RHEL (SELinux) braucht das eingebundene `Caddyfile` die Mount-Option `z`; sie steht bereits in `deploy/compose.yaml`. Mit Podman wurde das Setup über das `docker-compose`-Programm (Compose v2) und den Podman-Socket getestet.
