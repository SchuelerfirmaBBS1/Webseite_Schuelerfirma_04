# Datenbank-Anbindung (EF Core / MySQL) – Leitfaden für die Implementierung (#22)

Dieses Dokument beschreibt, was für die Datenbank-Anbindung gebaut werden muss, damit sie ohne Änderungen am restlichen Backend funktioniert. Es fasst die vorhandenen Modelle, die Repository-Interfaces und das erwartete Verhalten zusammen.

Maßgeblich ist immer der Code. Die XML-Kommentare in den Interfaces (`Application/Abstractions/`) und die Contract-Tests (`tests/.../Persistence/`) sind die eigentliche Spezifikation. Dieses Dokument erklärt sie und ergänzt die Punkte, an denen MySQL sich anders verhält als die In-Memory-Variante.

---

## 1. Ausgangslage

- Das Backend (ASP.NET Core, .NET 10) greift **nur über Repository-Interfaces** auf Daten zu. Kein Service und kein Controller kennt einen `DbContext`.
- Für diese Interfaces gibt es bisher nur **In-Memory-Implementierungen** (`Infrastructure/InMemory/`). Sie laufen nur in Development, die Daten sind nach jedem Neustart weg.
- Außerhalb von Development startet die App absichtlich nicht, solange keine Repositories registriert sind (`Infrastructure/PersistenceStartupCheck.cs`).
- **Aufgabe:** eine zweite Implementierung derselben Interfaces mit EF Core und MySQL bauen, inklusive Migrations, Registrierung und Tests. Am restlichen Code ändert sich dabei nichts.

```
Controller ──► Service (UserAdministrationService, AuthService, …)
                   │
                   ▼
        IUserRepository / IRoleRepository / IUserSessionRepository   ◄── Application/Abstractions
                   │
       ┌───────────┴────────────┐
       ▼                        ▼
 InMemory…Repository      Ef…Repository  (neu)                     ◄── Infrastructure/
 (Development/Tests)      SchuelerfirmaDbContext (neu) ──► MySQL
```

---

## 2. Überblick: Was gebaut werden muss

| # | Was | Wo (Vorschlag) |
|---|-----|----------------|
| 1 | NuGet-Pakete: `Microsoft.EntityFrameworkCore`, MySQL-Provider, `Microsoft.EntityFrameworkCore.Design` | `Schuelerfirma.Api.csproj` |
| 2 | `SchuelerfirmaDbContext` mit `DbSet<User>`, `DbSet<Role>`, `DbSet<UserSession>` | `Infrastructure/Persistence/` |
| 3 | Mapping per Fluent API (`IEntityTypeConfiguration<T>`), ein Typ pro Entity | `Infrastructure/Persistence/Configurations/` |
| 4 | `EfRoleRepository`, `EfUserRepository`, `EfUserSessionRepository` | `Infrastructure/Persistence/` |
| 5 | Übersetzung von MySQL-Fehlern in die erwarteten Exceptions | `Infrastructure/Persistence/` (Hilfsklasse) |
| 6 | Registrierung `AddMySqlPersistence(...)` und Umschaltung in `Program.cs` | `Infrastructure/Persistence/`, `Program.cs` |
| 7 | `IDesignTimeDbContextFactory` für `dotnet ef` | `Infrastructure/Persistence/` |
| 8 | Erste Migration `InitialCreate` (inkl. Seed der Rollen) | `Infrastructure/Persistence/Migrations/` |
| 9 | Contract-Tests gegen echtes MySQL (Testcontainers) | `tests/Schuelerfirma.Api.Tests/Persistence/` |
| 10 | Doku aktualisieren (README, CLAUDE.md, Hinweise auf „#22“) | – |

Die Namen sind Vorschläge. Wichtig ist nur, dass alles unter `Infrastructure/` liegt und die Domain-Klassen frei von EF-Attributen bleiben (Mapping nur per Fluent API).

---

## 3. Die Modelle (Entities)

Die Entities liegen in `backend/src/Schuelerfirma.Api/Domain/`. Längen-Grenzen stehen als Konstanten in den Klassen. Bitte diese Konstanten im Mapping verwenden statt Zahlen zu kopieren.

Das Datenmodell beschreiben die Mermaid-Diagramme `documentation/UMLClassDiagram.mmd` und `documentation/UMLEntityRelationshipDiagramm.mmd`. Bei Modelländerungen beide Diagramme mit aktualisieren.

### 3.1 `Role` (`Domain/Role.cs`)

| Property | C#-Typ | MySQL (Vorschlag) | Regeln |
|---|---|---|---|
| `RoleId` | `int` | `int`, PK | Werte kommen aus dem Seed (1 = Admin, 2 = User) |
| `Name` | `string` | `varchar(50)` (`Role.NameMaxLength`) | **unique**, Vergleich ohne Groß-/Kleinschreibung |
| `Users` | `ICollection<User>` | – (Navigation) | wird vom Repository nicht geladen |

- Rollen sind **Seed-Daten** und werden zur Laufzeit nicht bearbeitet. Seed über `HasData` aus `DefaultRoles.CreateSeed()`, damit In-Memory und Datenbank dieselben IDs und Namen haben:
  ```csharp
  builder.HasData(DefaultRoles.CreateSeed().Select(r => new { r.RoleId, r.Name }));
  ```
  (Die Projektion auf anonyme Objekte vermeidet Probleme mit der `Users`-Navigation im Seed.)
- Was eine Rolle darf, steht **nicht** in der Datenbank, sondern in `appsettings.json` unter `Authorization:RolePermissions`. Dafür braucht es keine Tabelle.

### 3.2 `User` (`Domain/User.cs`)

| Property | C#-Typ | MySQL (Vorschlag) | Regeln |
|---|---|---|---|
| `UserId` | `int` | `int AUTO_INCREMENT`, PK | wird beim Einfügen vergeben |
| `RoleId` | `int` | `int`, FK → `Role.RoleId` | **kein** Cascade Delete (`Restrict`) |
| `Role` | `Role?` | – (Navigation) | wird vom Repository **immer** mitgeladen |
| `Username` | `string` | `varchar(32)` (`UsernameMaxLength`) | **unique**, case-insensitive, ändert sich nie |
| `Email` | `string` | `varchar(254)` (`EmailMaxLength`) | **unique**, case-insensitive |
| `PasswordHash` | `string` | `varchar(256)` (`PasswordHashMaxLength`) | Base64 aus `PasswordHasher<User>` |
| `IsActive` | `bool` | `tinyint(1)` | Benutzer werden nie gelöscht, nur deaktiviert |
| `FailedLoginCount` | `int` | `int` | nur über die atomaren Methoden ändern |
| `LockoutEnd` | `DateTime?` | `datetime(6) NULL` | UTC |
| `MustChangePassword` | `bool` | `tinyint(1)` | |
| `CreatedAt` | `DateTime` | `datetime(6)` | UTC |
| `LastLoginAt` | `DateTime?` | `datetime(6) NULL` | UTC |

> **Achtung bei `bool`-Spalten:** kein `HasDefaultValue(true)` setzen. EF Core lässt sonst beim Einfügen den Wert `false` weg (er gilt als „nicht gesetzt“), und die Datenbank setzt `true`. Ein deaktiviert angelegter Benutzer wäre dann aktiv.

### 3.3 `UserSession` (`Domain/UserSession.cs`)

| Property | C#-Typ | MySQL (Vorschlag) | Regeln |
|---|---|---|---|
| `SessionKeyHash` | `string` | `char(64)`, Zeichensatz `ascii`, PK | SHA-256 als Hex in Kleinbuchstaben (`SessionKeyHashLength`) |
| `UserId` | `int` | `int`, FK → `User.UserId`, **Index** | Cascade Delete ist unkritisch (Benutzer werden nie gelöscht) |
| `User` | `User?` | – (Navigation) | wird vom Repository **nicht** geladen; `User` hat keine Gegen-Collection |
| `Ticket` | `byte[]` | `longblob` (Standard bei Pomelo) | serialisiertes ASP.NET-Core-Authentifizierungsticket |
| `CreatedAt` | `DateTime` | `datetime(6)` | UTC |
| `ExpiresAt` | `DateTime` | `datetime(6)`, **Index** | UTC; für das Aufräumen abgelaufener Sessions |

In der Datenbank steht nur der **Hash** des Session-Schlüssels, nie der Schlüssel selbst. Hashen ist Sache des Aufrufers (`SessionTicketStore`), das Repository speichert nur, was es bekommt.

### 3.4 Beziehungen

```
Role 1 ───< * User 1 ───< * UserSession
     (Restrict)      (Cascade)
```

---

## 4. Interfaces und die zu implementierenden Klassen

Alle Interfaces liegen in `backend/src/Schuelerfirma.Api/Application/Abstractions/`. Zu jedem Interface gibt es eine In-Memory-Implementierung als Referenz (`Infrastructure/InMemory/`) und Contract-Tests (`tests/.../Persistence/*ContractTests.cs`).

| Interface | In-Memory (Referenz) | Neu zu bauen | Contract-Tests |
|---|---|---|---|
| `IRoleRepository` | `InMemoryRoleRepository` | `EfRoleRepository` | `RoleRepositoryContractTests` |
| `IUserRepository` | `InMemoryUserRepository` | `EfUserRepository` | `UserRepositoryContractTests` |
| `IUserSessionRepository` | `InMemoryUserSessionRepository` | `EfUserSessionRepository` | `UserSessionRepositoryContractTests` |
| `DuplicateEntryException` | – | wird geworfen, nicht implementiert | – |

### 4.1 `IRoleRepository` (nur lesend)

| Methode | Erwartetes Verhalten | EF-Hinweis |
|---|---|---|
| `GetAllAsync()` | alle Rollen, sortiert nach `RoleId` | `AsNoTracking().OrderBy(r => r.RoleId)` |
| `GetByIdAsync(roleId)` | Rolle oder `null` | |
| `GetByNameAsync(name)` | Rolle oder `null`, **ohne Beachtung der Groß-/Kleinschreibung** | `r.Name == name` mit `_ci`-Kollation; kein `ToLower()` |

### 4.2 `IUserRepository`

Zurückgegebene Benutzer sind **losgelöste Kopien** (`AsNoTracking`). Eine Änderung daran wirkt erst nach `UpdateAsync`. `User.Role` ist **immer** geladen (`Include(u => u.Role)`); `UserPrincipal` wirft sonst eine Exception.

| Methode | Erwartetes Verhalten | EF-Hinweis |
|---|---|---|
| `GetByIdAsync(userId)` | Benutzer inkl. Rolle oder `null` | `AsNoTracking().Include(u => u.Role)` |
| `GetByUsernameAsync(username)` | case-insensitive | Gleichheit über `_ci`-Kollation |
| `GetAllAsync()` | alle Benutzer inkl. Rolle, sortiert nach `Username` (case-insensitive) | `OrderBy(u => u.Username)` sortiert dank Kollation richtig |
| `ExistsByUsernameAsync` / `ExistsByEmailAsync` | case-insensitive | `AnyAsync` |
| `CountActiveInRolesAsync(roleIds)` | Anzahl aktiver Benutzer in diesen Rollen; leere Liste → `0` | `Where(u => u.IsActive && roleIds.Contains(u.RoleId)).CountAsync()` |
| `AddAsync(user)` | speichert und **setzt `user.UserId`** auf dem übergebenen Objekt | siehe unten |
| `UpdateAsync(user)` | speichert **nur** `Email`, `RoleId`, `PasswordHash`, `IsActive`, `MustChangePassword` | `ExecuteUpdateAsync` mit genau diesen 5 Feldern |
| `IncrementFailedLoginCountAsync(userId)` | erhöht **atomar** um 1, gibt den neuen Wert zurück; unbekannter Benutzer → `0` | siehe 5.2 |
| `LockOutAsync(userId, lockoutEnd)` | setzt `LockoutEnd`, Zähler auf `0`; unbekannter Benutzer → nichts | ein `ExecuteUpdateAsync` |
| `RecordSuccessfulLoginAsync(userId, loginAt)` | Zähler `0`, `LockoutEnd = null`, `LastLoginAt = loginAt` | ein `ExecuteUpdateAsync` |
| `ClearLockoutAsync(userId)` | Zähler `0`, `LockoutEnd = null`, `LastLoginAt` bleibt | ein `ExecuteUpdateAsync` |

Fehlerfälle:

| Methode | Situation | Exception |
|---|---|---|
| `AddAsync` | Benutzername oder E-Mail schon vergeben | `DuplicateEntryException("Username")` bzw. `("Email")` |
| `AddAsync` / `UpdateAsync` | Rolle existiert nicht | `InvalidOperationException` |
| `UpdateAsync` | Benutzer existiert nicht | `KeyNotFoundException` |
| `UpdateAsync` | E-Mail gehört einem anderen Benutzer | `DuplicateEntryException("Email")` |

Warum `UpdateAsync` nur fünf Felder schreibt: Der Benutzername ändert sich nie. Die Sperr-Felder ändern nur die atomaren Methoden. Würde `UpdateAsync` das ganze Objekt speichern, könnte eine veraltete Kopie einen parallel hochgezählten Fehlversuchszähler überschreiben und so die Sperre nach zu vielen Fehlversuchen aushebeln (Test `Update_WithStaleCopy_DoesNotOverwriteTheFailedLoginCount`).

Bei `AddAsync` den Benutzer am besten in ein neues Entity-Objekt **ohne** `Role`-Navigation kopieren, speichern und dann `user.UserId = entity.UserId` setzen. Sonst versucht EF unter Umständen, eine mitgegebene Rolle neu einzufügen.

**Warum `DuplicateEntryException`?** Die Services prüfen vorher mit `ExistsBy…`, ob der Wert frei ist. Zwei gleichzeitige Requests können diese Prüfung aber beide bestehen. Erst der Unique-Index der Datenbank entscheidet zuverlässig. `UserAdministrationService` und `AdminBootstrapper` fangen die Exception ab und machen daraus eine saubere Fehlermeldung. `Field` muss deshalb exakt `nameof(User.Username)` bzw. `nameof(User.Email)` sein.

### 4.3 `IUserSessionRepository`

Zurückgegebene Sessions sind losgelöste Kopien, `UserSession.User` wird **nicht** geladen.

| Methode | Erwartetes Verhalten | EF-Hinweis |
|---|---|---|
| `AddAsync(session)` | speichert neue Session; doppelter Hash oder unbekannter Benutzer → `InvalidOperationException` | MySQL-Fehler 1062/1452 übersetzen |
| `GetAsync(hash)` | Session oder `null`; **auch abgelaufene** werden zurückgegeben (der Aufrufer prüft `ExpiresAt`) | `AsNoTracking()` |
| `UpdateAsync(session)` | speichert nur `Ticket` und `ExpiresAt`; existiert die Session nicht mehr → **nichts tun** (sie darf nicht wieder auftauchen) | `ExecuteUpdateAsync`, kein Upsert |
| `DeleteAsync(hash)` | löscht; nicht vorhanden → nichts tun | `ExecuteDeleteAsync` |
| `DeleteAllForUserAsync(userId, exceptHash)` | löscht alle Sessions des Benutzers, optional eine ausgenommen; gibt Anzahl zurück | `ExecuteDeleteAsync` |
| `DeleteExpiredAsync(now)` | löscht alle mit `ExpiresAt <= now`; gibt Anzahl zurück | `ExecuteDeleteAsync` |

Wer die Methoden aufruft: `SessionTicketStore` (bei jedem angemeldeten Request), `SessionCleanupService` (Hintergrunddienst, löscht regelmäßig abgelaufene Sessions), `AuthService` und `UserAdministrationService` (Sessions beenden bei Passwortwechsel, Rollenwechsel oder Deaktivierung).

---

## 5. Querschnittsregeln und Stolperfallen

### 5.1 Nebenläufigkeit: ein `DbContext` pro Methodenaufruf

Der Contract-Test `IncrementFailedLoginCount_InParallel_LosesNoCounts` ruft **dieselbe Repository-Instanz 100-mal parallel** auf. Ein `DbContext` verträgt keine parallelen Operationen. Deshalb:

- `AddDbContextFactory<SchuelerfirmaDbContext>(...)` registrieren,
- im Repository `IDbContextFactory<SchuelerfirmaDbContext>` injizieren,
- in jeder Methode einen kurzlebigen Context erzeugen: `await using var db = await contexts.CreateDbContextAsync(ct);`

Das passt auch zu „Repositories geben losgelöste Kopien zurück“: Nach dem Methodenaufruf gibt es keinen Context mehr, der Änderungen verfolgt.

### 5.2 Atomares Hochzählen mit Rückgabewert

`IncrementFailedLoginCountAsync` muss den **eigenen** neuen Wert zurückgeben. Bei 100 parallelen Aufrufen erwartet der Test genau die Werte 1 bis 100. Ein `ExecuteUpdateAsync` mit anschließendem `SELECT` reicht dafür nur innerhalb **einer Transaktion**: Das `UPDATE` sperrt die Zeile bis zum Commit (InnoDB), und das folgende `SELECT` sieht den eigenen Wert.

```csharp
await using var db = await contexts.CreateDbContextAsync(cancellationToken);
await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

var updated = await db.Users
    .Where(u => u.UserId == userId)
    .ExecuteUpdateAsync(s => s.SetProperty(u => u.FailedLoginCount, u => u.FailedLoginCount + 1), cancellationToken);
if (updated == 0)
{
    return 0;
}

var count = await db.Users.Where(u => u.UserId == userId).Select(u => u.FailedLoginCount).SingleAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
return count;
```

**Nicht** so: Benutzer laden, `FailedLoginCount++`, `SaveChanges`. Dabei gehen bei parallelen Angriffen Zählerstände verloren, und die Sperre greift zu spät.

Falls `EnableRetryOnFailure` aktiviert wird, müssen eigene Transaktionen in `db.Database.CreateExecutionStrategy().ExecuteAsync(...)` laufen. Einfacher ist es, Retry hier nicht zu aktivieren.

### 5.3 „Betroffene Zeilen“ bei MySQL

`UpdateAsync` erkennt einen unbekannten Benutzer daran, dass `ExecuteUpdateAsync` `0` zurückgibt. MySQL meldet je nach Verbindungsoption entweder die **gefundenen** oder nur die **tatsächlich geänderten** Zeilen. MySqlConnector (die Basis von Pomelo) liefert standardmäßig die gefundenen Zeilen. Deshalb in der Connection-String **nicht** `UseAffectedRows=true` setzen. Sonst wirft ein `UpdateAsync` ohne echte Änderung fälschlich `KeyNotFoundException` (Test `Update_DuplicateEmail_Throws_AndKeepingOwnEmailIsFine`).

### 5.4 MySQL-Fehler in die erwarteten Exceptions übersetzen

| MySQL-Fehler | Bedeutung | Übersetzen in |
|---|---|---|
| 1062 (`ER_DUP_ENTRY`) auf Unique-Index Username | Name vergeben | `DuplicateEntryException(nameof(User.Username))` |
| 1062 auf Unique-Index Email | E-Mail vergeben | `DuplicateEntryException(nameof(User.Email))` |
| 1062 auf PK `UserSession` | Hash schon vorhanden | `InvalidOperationException` |
| 1452 (`ER_NO_REFERENCED_ROW_2`) | FK zeigt ins Leere (Rolle bzw. Benutzer existiert nicht) | `InvalidOperationException` |

- Bei `SaveChangesAsync` steckt die `MySqlException` in `DbUpdateException.InnerException`. Bei `ExecuteUpdateAsync`/`ExecuteDeleteAsync` kommt sie **direkt** (ohne `DbUpdateException`). Beide Fälle abfangen.
- Welcher Index verletzt wurde, steht in der Fehlermeldung (`… for key 'Users.UX_Users_Email'`). Deshalb den Unique-Indizes **feste Namen** geben (`HasDatabaseName("UX_Users_Username")` / `"UX_Users_Email"`) und daran unterscheiden.
- Die Fehlernummer steht in `MySqlException.Number`.

### 5.5 Groß-/Kleinschreibung und Kollation

Username, E-Mail und Rollenname werden **ohne Beachtung der Groß-/Kleinschreibung** verglichen und sind so auch eindeutig („Anna“ und „anna“ dürfen nicht beide existieren). Das soll über die **Kollation** der Spalten laufen, damit Unique-Indizes und Vergleiche ohne `ToLower()` funktionieren:

```csharp
modelBuilder.HasCharSet("utf8mb4");
builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).UseCollation("utf8mb4_0900_ai_ci");
```

Hinweis: `utf8mb4_0900_ai_ci` (so steht es im Interface-Kommentar) ist zusätzlich **akzent**-unempfindlich („a“ = „ä“). Die In-Memory-Variante vergleicht nur ohne Groß-/Kleinschreibung. Benutzernamen sind auf `a-z0-9._-` beschränkt, dort spielt das keine Rolle. Bei E-Mail-Adressen mit Umlauten schon. Wer exakt dasselbe Verhalten will, nimmt `utf8mb4_0900_as_ci`. Bitte bewusst entscheiden und kurz dokumentieren.

Die `_0900_`-Kollationen gibt es ab **MySQL 8.0**. Läuft auf dem Server MariaDB, heißen sie anders (z. B. `utf8mb4_uca1400_ai_ci`), und Pomelo braucht dann `MariaDbServerVersion`.

### 5.6 Zeitstempel sind UTC – auch nach dem Lesen

MySQL speichert bei `datetime` keine Zeitzone. EF liefert die Werte deshalb mit `DateTimeKind.Unspecified` zurück. Das ist nicht nur Kosmetik: `SessionTicketStore` wandelt `CreatedAt + MaxLifetime` in ein `DateTimeOffset` um, und `Unspecified` wird dabei als **lokale Zeit** interpretiert. Auf einem Server, der nicht auf UTC steht, wären Session-Abläufe dann um Stunden verschoben.

Lösung: einen Value Converter für alle `DateTime`- und `DateTime?`-Properties registrieren, der beim Lesen `DateTimeKind.Utc` setzt:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
}

// UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
//   speichern: v => v      lesen: v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
```

Außerdem gilt: Repositories lesen **keine Uhr**. Kein `DateTime.UtcNow` im Repository; Zeitpunkte wie `now`, `loginAt` und `lockoutEnd` übergibt immer der Aufrufer.

### 5.7 Kurzfassung der Regeln

- Lesende Abfragen immer mit `AsNoTracking()`.
- `User.Role` immer laden, `UserSession.User` nie.
- Bulk-Updates und -Deletes mit `ExecuteUpdateAsync`/`ExecuteDeleteAsync`.
- Keine Domain-Logik im Repository (z. B. keine Prüfung, ob ein Benutzer gesperrt ist). Das machen die Services.
- `CancellationToken` an alle EF-Aufrufe durchreichen.
- XML-Doc-Comments an öffentlichen Klassen und Methoden (`/// <inheritdoc />` reicht bei Interface-Methoden).

---

## 6. Registrierung, Konfiguration, Migrations

### 6.1 Pakete

- `Microsoft.EntityFrameworkCore` in der Version zu .NET 10 (EF Core 10).
- MySQL-Provider: laut Projektvorgabe `Pomelo.EntityFrameworkCore.MySql`. **Vorher prüfen, ob es eine Version für EF Core 10 gibt.** Pomelo erschien in der Vergangenheit oft erst Monate nach einem EF-Release. Falls nicht, mit dem Team besprechen, ob der Oracle-Provider `MySql.EntityFrameworkCore` die Alternative ist. Einige Hinweise oben (z. B. `HasCharSet`) sind Pomelo-spezifisch.
- `Microsoft.EntityFrameworkCore.Design` (mit `PrivateAssets="all"`) für `dotnet ef`.
- `dotnet-ef` als lokales Tool (`dotnet new tool-manifest` + `dotnet tool install dotnet-ef` im Ordner `backend/`), damit alle dieselbe Version nutzen.

### 6.2 Connection-String

- Name: `ConnectionStrings:Default`.
- **Nie committen.** Lokal:
  ```bash
  cd backend
  dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=schuelerfirma;User=sf_app;Password=…" --project src/Schuelerfirma.Api
  ```
  Auf dem Server als Umgebungsvariable `ConnectionStrings__Default`.
- In `appsettings.json` höchstens ein leerer Platzhalter.
- Server-Version **explizit** angeben (z. B. `new MySqlServerVersion(new Version(8, 0, 36))`) statt `ServerVersion.AutoDetect`. AutoDetect öffnet schon beim Start eine Verbindung und funktioniert bei `dotnet ef migrations add` ohne Datenbank nicht.

### 6.3 DI-Registrierung

Eine Extension-Methode analog zu `AddInMemoryPersistence`:

```csharp
public static IServiceCollection AddMySqlPersistence(this IServiceCollection services, string connectionString)
{
    services.AddDbContextFactory<SchuelerfirmaDbContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
    services.AddScoped<IUserRepository, EfUserRepository>();
    services.AddScoped<IRoleRepository, EfRoleRepository>();
    services.AddScoped<IUserSessionRepository, EfUserSessionRepository>();
    return services;
}
```

Die Lebensdauer `Scoped` ist in Ordnung: Alle Singletons und Hintergrunddienste (`SessionTicketStore`, `SessionCleanupService`, `AdminBootstrapper`) holen sich die Repositories bereits über einen eigenen Scope (`IServiceScopeFactory`).

Umschaltung in `Program.cs` (Vorschlag, mit dem Team abstimmen):

```csharp
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddMySqlPersistence(connectionString);
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddInMemoryPersistence();
}
```

Damit bleibt alles Bestehende gültig:

- Ohne Connection-String läuft Development weiter im Arbeitsspeicher.
- Ohne Connection-String verweigert die App in Production weiterhin den Start (`PersistenceStartupCheck`, Test `Production_WithoutRegisteredRepositories_RefusesToStart`).
- **Wichtig:** In `tests/.../Integration/ApiFactory.cs` zusätzlich `["ConnectionStrings:Default"] = null` setzen (wie bei `Bootstrap:*`). Sonst laufen die Integrationstests gegen die lokale Datenbank, sobald ein Entwickler einen Connection-String in seinen user-secrets hat, und `PersistenceStartupTests.Development_UsesInMemoryRepositories` schlägt fehl.

Werden später weitere Repository-Interfaces ergänzt, gehören sie auch in die Liste `required` in `PersistenceStartupCheck`.

### 6.4 Migrations

- `dotnet ef` baut den Host über `Program.cs`. Ohne Connection-String ist dort aber kein `DbContext` registriert. Deshalb eine `IDesignTimeDbContextFactory<SchuelerfirmaDbContext>` anlegen, die den Context mit fester Server-Version erzeugt. Für `migrations add` braucht es dann keine laufende Datenbank.
- Erste Migration:
  ```bash
  cd backend
  dotnet ef migrations add InitialCreate --project src/Schuelerfirma.Api --output-dir Infrastructure/Persistence/Migrations
  ```
- Die Migration enthält den **Seed der Rollen** (`HasData`). Ohne die Rolle `Admin` bricht `AdminBootstrapper` den Start ab.
- Schema-Änderungen **nur über Migrations**, nie von Hand an der Datenbank.
- **Wann werden Migrations angewendet?** `AdminBootstrapper` läuft beim Start und braucht fertige Tabellen. Empfehlung: vor dem Start der neuen Version per `dotnet ef database update` oder Migration-Bundle (`dotnet ef migrations bundle`), nicht automatisch in `Program.cs`. Optional kann die App sie in Development beim Start anwenden. Wer das wie macht, bitte mit dem Team festlegen und im README dokumentieren.
- Optional, aber sinnvoll: getrennte DB-Benutzer. Der App-Benutzer darf nur lesen und schreiben (`SELECT/INSERT/UPDATE/DELETE`), ein eigener Benutzer führt die Migrations aus (DDL).

---

## 7. Tests

### 7.1 Contract-Tests ableiten

Die Contract-Tests sind abstrakt. Jede Implementierung leitet die drei Klassen ab und liefert nur `CreateRepositories()`. So sieht es für In-Memory aus (`InMemoryRepositoryTests.cs`):

```csharp
public class InMemoryUserRepositoryTests : UserRepositoryContractTests
{
    protected override Repositories CreateRepositories() => InMemoryRepositories.Create();
}
```

Für MySQL entsteht `MySqlRepositoryTests.cs` mit `MySqlRoleRepositoryTests`, `MySqlUserRepositoryTests` und `MySqlUserSessionRepositoryTests`. Anforderungen an `CreateRepositories()`:

- Jeder Aufruf liefert **einen frischen, leeren Datenbestand, der nur die Seed-Rollen enthält**.
- Die drei Repositories im `Repositories`-Record arbeiten auf **derselben** Datenbank.

Vorschlag:

- **Testcontainers** (`Testcontainers.MySql`) startet einmal pro Testlauf einen MySQL-Container, z. B. über eine xUnit-Collection-Fixture. Die Testklassen bekommen die Fixture per Konstruktor; die abstrakten Basisklassen haben einen parameterlosen Konstruktor, das passt.
- Pro `CreateRepositories()` eine neue Datenbank mit zufälligem Namen anlegen und die Migrations anwenden (`Database.Migrate()`). Damit werden auch die Migrations selbst getestet.
- **Nicht** den EF-InMemory-Provider oder SQLite als Ersatz verwenden. Die kennen weder die MySQL-Kollation noch Fehler 1062 noch das Sperrverhalten von InnoDB, und genau das sollen die Tests prüfen.
- Der parallele Test öffnet bis zu 100 Verbindungen gleichzeitig. Der Standard-Pool von MySqlConnector (100) und `max_connections` von MySQL (151) reichen dafür.

Die Contract-Tests dürfen **nicht** an die MySQL-Implementierung angepasst werden. Schlägt ein Test fehl, ist die Implementierung falsch. Fehlt ein Verhalten in den Tests, kommt ein neuer Test in die abstrakte Klasse, und er muss dann auch für In-Memory grün sein.

### 7.2 CI

Die CI (`.github/workflows/ci.yml`) führt `dotnet test` aus. Auf den GitHub-Ubuntu-Runnern ist Docker verfügbar, Testcontainers funktioniert dort ohne weitere Einrichtung. Lokal braucht man dafür Docker oder Podman.

### 7.3 Bestehende Tests

`dotnet build` und `dotnet test` müssen weiterhin komplett grün sein. Die Integrationstests (`WebApplicationFactory`) laufen weiter gegen In-Memory (siehe Hinweis zu `ApiFactory` in 6.3).

---

## 8. Ausblick: geplante Entities (noch nicht im Code)

Die Diagramme enthalten weitere Entities, für die es noch **keine** Domain-Klassen und Interfaces gibt: `Category`, `Product`, `ProductImage`, `Event`, `EventImage`, `ContactRequest`, `ApplicationSettings`. Sie gehören nicht zu #22, sondern entstehen mit den jeweiligen Features. Wer sie anlegt, folgt demselben Muster:

1. Entity in `Domain/` (ohne EF-Attribute, Längen als Konstanten, Zeitstempel UTC).
2. Repository-Interface in `Application/Abstractions/` mit XML-Kommentaren zum erwarteten Verhalten.
3. In-Memory-Implementierung in `Infrastructure/InMemory/` und abstrakte Contract-Tests in `tests/.../Persistence/`, abgeleitet für In-Memory **und** MySQL.
4. EF-Mapping, EF-Repository und Migration.
5. Interface in `PersistenceStartupCheck` eintragen, Diagramme aktualisieren.

Hinweise aus den Diagrammen:

- Bilder (`ProductImage`, `EventImage`) werden laut aktuellem Modell als Datei abgelegt (`FilePath`), nicht als `byte[]` in der Datenbank.
- `Product.Price` ist `decimal?`. Präzision explizit festlegen (z. B. `decimal(10,2)`).
- `ApplicationSettings` ist eine Tabelle mit genau einer Zeile (Ziel-Adresse für Kontaktanfragen).
- `Event.StartDateTime`/`EndDateTime` als UTC speichern; die Umrechnung in deutsche Zeit macht das Frontend.

---

## 9. Checkliste (Definition of Done)

- [ ] `SchuelerfirmaDbContext` mit Fluent-API-Mapping für `Role`, `User`, `UserSession` (Längen aus den Konstanten, Unique-Indizes mit festen Namen, Indizes auf `UserSession.UserId` und `ExpiresAt`, FKs mit `Restrict`/`Cascade` wie oben)
- [ ] Rollen-Seed über `HasData(DefaultRoles.CreateSeed() …)`
- [ ] Kollation für Username, Email und Rollenname festgelegt und dokumentiert
- [ ] UTC-Converter für `DateTime`/`DateTime?`
- [ ] `EfRoleRepository`, `EfUserRepository`, `EfUserSessionRepository` mit `IDbContextFactory`
- [ ] Atomare Zähler-Methoden (kein Lesen-Ändern-Speichern), `IncrementFailedLoginCountAsync` in einer Transaktion
- [ ] MySQL-Fehler 1062/1452 in `DuplicateEntryException` bzw. `InvalidOperationException` übersetzt
- [ ] `AddMySqlPersistence` und Umschaltung in `Program.cs`; `ApiFactory` setzt `ConnectionStrings:Default = null`
- [ ] `IDesignTimeDbContextFactory` und Migration `InitialCreate`
- [ ] Contract-Tests für MySQL abgeleitet (Testcontainers), alle grün; bestehende Tests weiterhin grün
- [ ] Keine Secrets im Repository
- [ ] README (Setup der Datenbank, Connection-String, Migrations anwenden), CLAUDE.md und Diagramme aktualisiert; Hinweise „erst mit EF Core (#22)“ entfernt

## 10. Arbeitsweise im Repo

- Branch `feature/<name>` von `dev` abzweigen, PR zurück nach `dev`.
- Commit-Messages nach Conventional Commits auf Englisch, z. B. `feat(backend): add EF Core user repository`.
- Bezeichner im Code auf Englisch, Doku auf Deutsch.
- Vor dem Push lokal `dotnet build` und `dotnet test` im Ordner `backend/` ausführen.
