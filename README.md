# Webseite_Schuelerfirma_04
Webseite für die Schülerfirma der BBS1 Lüneburg

Frontend: Vue/Tailwind

Backend: C# ASP.NET Core 

Datenbank: EF Core

Planung Backend:
Admin-Bereich
Termine ändern/einstellen
Kontaktformular einsehen
E-Mail Adresse ändern, an die die Kontaktformulare gehen

## Backend starten

Voraussetzung: .NET 10 SDK.

```bash
cd backend
dotnet run --project src/Schuelerfirma.Api
dotnet test
```

`dotnet run` startet die API unter `https://localhost:7205` (zusätzlich `http://localhost:5077`). Schreibende Anfragen funktionieren nur über HTTPS, weil die CSRF-Cookies `Secure` sind; einmalig `dotnet dev-certs https --trust` ausführen. Ein Dev-Server des Frontends leitet `/api` deshalb an `https://localhost:7205` weiter.

### Ersten Admin einrichten

Beim Start legt das Backend einen Administrator an, falls noch keiner existiert. Die Zugangsdaten gehören **nicht** ins Repository, sondern in `dotnet user-secrets` (lokal) bzw. in Umgebungsvariablen (Server):

```bash
cd backend/src/Schuelerfirma.Api
dotnet user-secrets set "Bootstrap:AdminUsername" "admin"
dotnet user-secrets set "Bootstrap:AdminEmail" "admin@example.org"
dotnet user-secrets set "Bootstrap:AdminPassword" "<mindestens 12 Zeichen>"
```

Auf dem Server heißen die Variablen `Bootstrap__AdminUsername`, `Bootstrap__AdminEmail` und `Bootstrap__AdminPassword`. Der Benutzername besteht aus 3–32 Zeichen (`a-z`, `0-9`, `.`, `_`, `-`). Das Passwort muss die Passwortrichtlinie erfüllen, sonst startet das Backend nicht. Es muss beim ersten Login geändert werden; danach `Bootstrap:AdminPassword` aus der Konfiguration entfernen. Solange keine Datenbank angebunden ist (#22), liegen alle Daten nur im Arbeitsspeicher und der Admin wird bei jedem Start neu angelegt.

### Konfiguration

| Schlüssel | Standard | Bedeutung |
|---|---|---|
| `Auth:Password:MinLength` | 12 | Mindestlänge neuer Passwörter (12–128) |
| `Auth:Lockout:MaxFailedAttempts` | 5 | Fehlversuche bis zur Kontosperre |
| `Auth:Lockout:Duration` | `00:15:00` | Dauer der Kontosperre |
| `Auth:Session:IdleTimeout` | `00:30:00` | Session endet nach so langer Inaktivität |
| `Auth:Session:MaxLifetime` | `08:00:00` | Session endet spätestens so lange nach dem Login |
| `Auth:Session:CleanupInterval` | `00:10:00` | Abstand, in dem abgelaufene Sessions gelöscht werden |
| `DataProtection:KeysPath` | – | Ordner für die Schlüssel des Session-Cookies; **außerhalb von Development Pflicht**. Der Ordner muss für das Backend beschreibbar und für alle anderen unlesbar sein (die Schlüssel liegen unverschlüsselt dort); ohne ihn wären nach jedem Neustart alle abgemeldet. |
| `ForwardedHeaders:KnownProxies` | leer | IP-Adressen der Reverse Proxys, denen `X-Forwarded-*` vertraut wird |
| `RateLimiting:Auth:PermitLimit` / `:Window` | 5 / `00:01:00` | Login-Versuche pro Client-IP und Zeitfenster |
| `RateLimiting:Contact:PermitLimit` / `:Window` | 10 / `00:10:00` | Absendungen öffentlicher Formulare (Kontaktformular) pro Client-IP und Zeitfenster; bewusst großzügig, weil sich auf der Messe viele Besucher eine IP teilen |

### Login-API

- `GET /api/auth/csrf` → `204`, setzt die CSRF-Cookies (siehe unten); ohne Anmeldung erreichbar.
- `POST /api/auth/login` mit `{ "username": "...", "password": "..." }` → `204` und Cookie `__Host-sf.session`, sonst `401`. Alle Fehlerfälle (unbekannter Benutzer, falsches Passwort, gesperrtes oder deaktiviertes Konto) liefern dieselbe Antwort. Höchstens 5 Versuche pro Minute und IP, danach `429` mit `Retry-After`.
- `POST /api/auth/logout` → `204`, beendet die Session auf dem Server.
- `GET /api/auth/me` → angemeldeter Benutzer (`userId`, `username`, `email`, `role`), ohne Login `401`.
- Alle Endpunkte außer den ausdrücklich mit `[AllowAnonymous]` markierten verlangen eine Anmeldung; das gilt auch für unbekannte Pfade (`401` statt `404`).

### CSRF-Schutz: Hinweise fürs Frontend

Jede schreibende Anfrage (POST, PUT, PATCH, DELETE) braucht ein CSRF-Token – auch der Login und öffentliche Formulare wie das Kontaktformular. Ohne gültiges Token antwortet die API mit `400` und dem Titel „Sicherheitstoken fehlt oder ist ungültig“.

1. Beim Start der Seite und **vor jedem Login** `GET /api/auth/csrf` aufrufen. Das setzt das Cookie `XSRF-TOKEN` (für JavaScript lesbar) und ein zweites, nicht lesbares Cookie.
2. Bei jeder schreibenden Anfrage den aktuellen Wert des Cookies `XSRF-TOKEN` im Header `X-XSRF-TOKEN` mitsenden. Den Wert jedes Mal frisch aus dem Cookie lesen, nicht zwischenspeichern. axios macht das mit den Standardeinstellungen automatisch, bei `fetch` von Hand.
3. Login und Logout stellen selbst ein neues Token aus, denn das Token gehört zum angemeldeten Benutzer.
4. Ist jemand angemeldet (`GET /api/auth/me` liefert `200`), muss er sich erst abmelden, bevor sich jemand anderes anmeldet – das Token einer laufenden Session wird beim Login abgelehnt. Das Login-Formular zeigt in dem Fall „Angemeldet als … – Abmelden“.
5. Kommt `400` mit dem obigen Titel (z. B. weil die Session inzwischen abgelaufen ist), einmal `GET /api/auth/csrf` aufrufen und die Anfrage wiederholen.

Bei `429` („Zu viele Anfragen“) steht im Header `Retry-After`, nach wie vielen Sekunden es wieder geht.

