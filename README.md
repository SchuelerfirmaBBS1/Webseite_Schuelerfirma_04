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

### Login-API

- `POST /api/auth/login` mit `{ "username": "...", "password": "..." }` → `204` und Cookie `__Host-sf.session`, sonst `401`. Alle Fehlerfälle (unbekannter Benutzer, falsches Passwort, gesperrtes oder deaktiviertes Konto) liefern dieselbe Antwort.
- `POST /api/auth/logout` → `204`, beendet die Session auf dem Server.
- `GET /api/auth/me` → angemeldeter Benutzer (`userId`, `username`, `email`, `role`), ohne Login `401`.
- Alle Endpunkte außer den ausdrücklich mit `[AllowAnonymous]` markierten verlangen eine Anmeldung; das gilt auch für unbekannte Pfade (`401` statt `404`).

