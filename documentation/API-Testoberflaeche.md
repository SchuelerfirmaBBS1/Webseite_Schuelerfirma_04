# API-Testoberfläche (Swagger UI) – Anleitung

Mit der Testoberfläche probiert ihr das Backend direkt im Browser aus: alle Endpunkte ansehen, Anfragen abschicken, Antworten und Statuscodes prüfen. Dafür braucht ihr weder das Frontend noch `curl`.

Die Oberfläche gibt es **nur in Development**. Auf dem Server (Production) existiert sie nicht.

---

## 1. Einmalig vorbereiten

### .NET 10 SDK

```bash
dotnet --version   # muss mit 10. beginnen
```

### Entwicklungszertifikat vertrauen

Die Cookies für Login und CSRF-Schutz sind `Secure`, deshalb läuft die Oberfläche nur über HTTPS:

```bash
dotnet dev-certs https --trust
```

Unter Linux reicht das oft nicht für den Browser. Zeigt der Browser beim ersten Aufruf eine Zertifikatswarnung, akzeptiert sie einmal („Erweitert“ → „Risiko akzeptieren und fortfahren“). Für lokales Testen ist das in Ordnung.

### Ersten Admin anlegen

Ohne Admin kann sich niemand anmelden. Die Zugangsdaten kommen in die user-secrets, **nicht** ins Repository:

```bash
cd backend/src/Schuelerfirma.Api
dotnet user-secrets set "Bootstrap:AdminUsername" "admin"
dotnet user-secrets set "Bootstrap:AdminEmail" "admin@example.org"
dotnet user-secrets set "Bootstrap:AdminPassword" "<euer Startpasswort>"
```

Regeln für das Passwort (gelten auch beim Passwortwechsel):

- mindestens 12 Zeichen, höchstens 128
- mindestens ein Großbuchstabe, ein Kleinbuchstabe, eine Ziffer und ein Sonderzeichen. Als Sonderzeichen zählt alles, was weder Buchstabe noch Ziffer noch Leerzeichen ist (z. B. `! ? # - _ . @ €`). Umlaute zählen als Buchstaben.
- nicht auf der Liste häufiger Passwörter
- enthält nicht den Benutzernamen
- keine einfache Folge (`123456789012`, `abcdefghijkl`) und nicht nur ein Zeichen wiederholt

Gut ist zum Beispiel `Rhabarber steht am Messestand #2026`: lang, leicht zu merken und mit allen vier Zeichenarten.

Benutzernamen bestehen aus 3–32 Zeichen aus `a-z`, `0-9`, `.`, `_` und `-`.

---

## 2. Starten

```bash
cd backend
dotnet run --project src/Schuelerfirma.Api
```

Dann im Browser öffnen: **https://localhost:7205/swagger**

> Immer über `https://localhost:7205` arbeiten. `dotnet run` nutzt dafür automatisch das `https`-Profil; mit dem Profil `http` (`--launch-profile http`) funktioniert der Login nicht, weil die Cookies HTTPS verlangen.

Beenden mit `Strg+C`.

---

## 3. Aufbau der Oberfläche

- Ganz oben steht eine Kurzanleitung.
- Darunter folgen die Endpunkte, gruppiert nach Bereich:

  | Gruppe | Pfad | Zugriff |
  |---|---|---|
  | `auth` | `/api/auth/...` | Anmeldung, Abmeldung, eigener Benutzer, CSRF-Token. **Hier anfangen.** |
  | `admin` | `/api/admin/...` | nur mit Login **und** passender Berechtigung der Rolle |
  | `public` | `/api/public/...` | ohne Login (für Besucher der Webseite: Produkte, Kategorien, Bilder) |

- Ein Klick auf einen Endpunkt klappt ihn auf. Dort stehen Beschreibung, Parameter, Beispiel-Body und alle möglichen Antworten mit Statuscode.
- „Try it out“ ist schon aktiv: Body bzw. Parameter ausfüllen und **Execute** klicken.
- Bilder hochladen (`POST /api/admin/products/{productId}/images`): Bei `files` mit **Add item** eine oder mehrere Dateien auswählen. Erlaubt sind JPEG, PNG und WebP; das gespeicherte Bild lässt sich danach über die `url` aus der Antwort im Browser öffnen.
- Unter „Responses“ erscheinen danach die echte Antwort (Code, Body, Header) und der passende `curl`-Befehl.
- Ganz unten unter „Schemas“ stehen alle Request- und Response-Typen.

Ihr müsst euch um nichts weiter kümmern. Das Session-Cookie schickt der Browser von selbst mit, und das CSRF-Token setzt die Oberfläche bei POST, PUT, PATCH und DELETE automatisch in den Header `X-XSRF-TOKEN`. Einen „Authorize“-Button braucht es deshalb nicht.

---

## 4. Grundablauf: anmelden und loslegen

1. **`GET /api/auth/csrf`** → `204`
   Holt das CSRF-Token. Ohne diesen Schritt antwortet jede schreibende Anfrage mit `400`.

2. **`POST /api/auth/login`** → `204`
   ```json
   { "username": "admin", "password": "<euer Startpasswort>" }
   ```

3. **`GET /api/auth/me`** → `200`
   Beim ersten Login steht dort `"mustChangePassword": true` und `"permissions": []`. Mit einem temporären Passwort darf man nichts außer Passwort ändern, `me` und Logout. Alle `admin`-Endpunkte antworten bis dahin mit `403`.

4. **`POST /api/auth/change-password`** → `204`
   ```json
   { "currentPassword": "<euer Startpasswort>", "newPassword": "<neues Passwort>" }
   ```
   Danach seid ihr **überall abgemeldet**, auch in diesem Browser. Das ist gewollt.

5. Noch einmal **`POST /api/auth/login`**, diesmal mit dem neuen Passwort → `204`.

6. **`GET /api/auth/me`** → jetzt mit allen Berechtigungen der Rolle.

7. Endpunkte testen (siehe Abschnitt 5).

8. **`POST /api/auth/logout`** → `204`. Danach liefert `GET /api/auth/me` `401`.

> **Wichtig:** Ohne Datenbank (kein `ConnectionStrings:Default`, siehe README „Datenbank einrichten“) liegen alle Daten nur im Arbeitsspeicher. Nach jedem Neustart des Backends sind alle Benutzer und Sessions weg, und der Admin wird mit dem Startpasswort neu angelegt. Ihr beginnt dann wieder bei Schritt 1.

---

## 5. Testfälle

Eine Auswahl dessen, was ihr prüfen könnt. In Klammern steht die erwartete Antwort.

### Anmeldung (`auth`)

| Test | So geht's | Erwartet |
|---|---|---|
| Login ohne CSRF-Token | direkt nach dem Start, ohne `GET /api/auth/csrf`, einloggen | `400` „Sicherheitstoken fehlt oder ist ungültig“ |
| Falsches Passwort | Login mit falschem Passwort | `401` |
| Unbekannter Benutzer | Login mit erfundenem Namen | `401`, **genau dieselbe Antwort** wie beim falschen Passwort |
| Nicht angemeldet | nach Logout `GET /api/auth/me` | `401` |
| Schwaches neues Passwort | `change-password` mit `newPassword` = `passwort1234` | `400`, alle Begründungen unter `errors.NewPassword` (hier: Großbuchstabe und Sonderzeichen fehlen) |
| Ohne Sonderzeichen | `change-password` mit `newPassword` = `Rhabarber steht am Messestand 2026` | `400`: Ein Leerzeichen zählt nicht als Sonderzeichen |
| Neues = altes Passwort | `change-password` mit zweimal demselben Passwort | `400` |
| Zu viele Versuche | mehr als 5 Logins innerhalb einer Minute | `429`, siehe Header `Retry-After` |

### Benutzerverwaltung (`admin`, als Admin mit geändertem Passwort)

| Test | So geht's | Erwartet |
|---|---|---|
| Alle Benutzer | `GET /api/admin/users` | `200` mit Liste |
| Rollen | `GET /api/admin/roles` | `200`: `Admin` (Id 1) und `User` (Id 2) mit ihren Berechtigungen |
| Benutzer anlegen | `POST /api/admin/users` mit `{ "username": "member", "email": "member@example.org", "roleId": 2 }` | `201` mit `temporaryPassword` (wird nur dieses eine Mal angezeigt, notieren!) |
| Name schon vergeben | denselben Benutzer noch einmal anlegen | `409` |
| Ungültige Eingabe | `username` = `A!` | `400`, Meldung unter `errors` |
| Unbekannte Id | `GET /api/admin/users/999` | `404` |
| Benutzer ändern | `PUT /api/admin/users/{id}` mit `{ "email": "neu@example.org", "roleId": 2, "isActive": true }` | `200` |
| Benutzer deaktivieren | `PUT` mit `"isActive": false` | `200`; der Benutzer kann sich nicht mehr anmelden, laufende Sessions enden |
| Sich selbst deaktivieren | `PUT` auf die eigene Id mit `"isActive": false` | `409` |
| Passwort zurücksetzen | `POST /api/admin/users/{id}/reset-password` | `200` mit neuem temporären Passwort; Sperre aufgehoben, Sessions beendet |
| Sessions beenden | `POST /api/admin/users/{id}/revoke-sessions` | `200` mit Anzahl beendeter Sessions |

### Berechtigungen (mit zweitem Benutzer)

So prüft ihr, dass ein Benutzer ohne Berechtigung abgewiesen wird:

1. Als Admin den Benutzer `member` mit `roleId` 2 (Rolle `User`) anlegen und das temporäre Passwort notieren.
2. `POST /api/auth/logout`.
3. Als `member` mit dem temporären Passwort einloggen.
4. `GET /api/admin/users` → `403` (temporäres Passwort noch nicht geändert).
5. `change-password` ausführen und mit dem neuen Passwort wieder einloggen.
6. `GET /api/auth/me` → Berechtigungen der Rolle `User`, aber **ohne** `users.manage`.
7. `GET /api/admin/users` → `403` (der Rolle fehlt die Berechtigung).

Tipp: Zum Testen mit zwei Benutzern gleichzeitig einen zweiten Browser oder ein privates Fenster nehmen. Dann hat jedes Fenster seine eigenen Cookies.

### Kontosperre

Nach 5 falschen Passwörtern ist ein Konto 15 Minuten gesperrt. Auch das richtige Passwort liefert dann `401`. Weil das Rate Limit (5 Versuche pro Minute) vorher greift, startet das Backend für diesen Test mit höherem Limit (siehe unten). Dann:

1. 5-mal als `member` mit falschem Passwort einloggen → jeweils `401`.
2. Mit dem richtigen Passwort einloggen → trotzdem `401`.
3. Als Admin `GET /api/admin/users` → bei `member` steht `lockedUntil`.
4. Als Admin `POST /api/admin/users/{id}/reset-password` → Sperre aufgehoben, neues temporäres Passwort.

---

## 6. Rate Limit zum Testen hochsetzen

Login und Passwortwechsel sind auf 5 Versuche pro Minute und IP begrenzt. Für längere Testrunden das Backend so starten:

```bash
cd backend
RateLimiting__Auth__PermitLimit=50 dotnet run --project src/Schuelerfirma.Api
```

---

## 7. Fehlersuche

| Problem | Ursache und Lösung |
|---|---|
| `400` „Sicherheitstoken fehlt oder ist ungültig“ | `GET /api/auth/csrf` fehlt oder liegt vor einem Neustart des Backends. Ausführen und die Anfrage wiederholen. |
| Plötzlich `401`, obwohl eben noch angemeldet | Das Backend wurde neu gestartet (Daten nur im Arbeitsspeicher), die Session ist abgelaufen (30 Minuten ohne Aktivität), oder das Passwort wurde geändert. Neu anmelden. |
| `403` als Admin | Das temporäre Passwort ist noch nicht geändert (`GET /api/auth/me` zeigt `"mustChangePassword": true`). |
| `429` | Zu viele Versuche. Eine Minute warten oder das Limit hochsetzen (Abschnitt 6). |
| Login als Admin klappt nie | Die user-secrets für `Bootstrap:*` fehlen. Beim Start steht dazu eine Warnung im Terminal. |
| Backend startet nicht, Meldung zu `Bootstrap:AdminPassword` | Das Startpasswort verletzt die Passwortregeln (Abschnitt 1). Anderes Passwort setzen. |
| Browser warnt vor dem Zertifikat | `dotnet dev-certs https --trust` ausführen oder die Warnung einmal akzeptieren. |
| `/swagger` liefert `404` oder `401` | Das Backend läuft nicht in Development. Mit `dotnet run` aus `backend/` starten, nicht mit `ASPNETCORE_ENVIRONMENT=Production`. |

---

## 8. Für Entwickler: neue Endpunkte in der Oberfläche

Neue Controller-Endpunkte erscheinen automatisch. Damit sie gut beschrieben sind:

- XML-Kommentar mit `<summary>` und einem `<response code="...">` pro möglicher Antwort.
- `[ProducesResponseType]` für die eigenen Antworten, Fehler als `ProblemDetails` bzw. `ValidationProblemDetails`.
- 401, 403, 429 und die CSRF-400 nicht selbst eintragen. Die ergänzt `ApiAreaOperationTransformer` aus `[AllowAnonymous]`, `[HasPermission]`, `[EnableRateLimiting]` und der HTTP-Methode.
