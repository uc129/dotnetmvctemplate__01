# AppTemplate: ASP.NET Core MVC, Onion, Dapper, stored procedures

A starting point for new line-of-business apps. It fixes the decisions every project otherwise
makes differently: project layout, data access, dates, error handling, logging, tests. Where it
can, it enforces those decisions with the compiler, the test suite and CI rather than relying on
memory.

The sample feature, **Customers** (a server-side DataTables list, create, edit), touches every
layer. Copy its shape for each new feature.

## Stack

ASP.NET Core MVC on .NET 10 (LTS) · Dapper · SQL Server · stored procedures only · SDK-style SQL
Database project (dacpac) · Serilog · jQuery, jQuery UI date picker, DataTables 2, Bootstrap 5 ·
xUnit v3, Testcontainers.

## Layout

```
AppTemplate.sln
├─ src/
│  ├─ AppTemplate.Domain/          Entities and their rules. References nothing.
│  ├─ AppTemplate.Application/     Services (use cases), repository interfaces, DateFormat, BusinessClock.
│  ├─ AppTemplate.Infrastructure/  DapperContext, repositories (one method per stored procedure).
│  └─ AppTemplate.Web/             MVC controllers, views, date binding, error pages, Program.cs.
├─ database/AppTemplate.Database/  Tables and stored procedures → AppTemplate.Database.dacpac
├─ tests/
│  ├─ AppTemplate.UnitTests/          Fast, no database.
│  ├─ AppTemplate.ArchitectureTests/  Layer and convention rules.
│  └─ AppTemplate.IntegrationTests/   Real SQL Server in Docker + the real HTTP pipeline.
├─ BannedSymbols.txt          APIs that are compile errors (see below).
├─ Directory.Build.props      Settings for every project (warnings as errors, nullable, analyzers).
└─ Directory.Packages.props   Every NuGet version, once.
```

Dependencies point inwards: `Web → Application → Domain` and `Infrastructure → Application → Domain`.
Web references Infrastructure only so `Program.cs` can register it.

## A request, end to end

```
Browser ─► CustomersController ─► CustomerService ─► ICustomerRepository
            (bind, respond)        (rules, Result)     │ implemented by
                                                       ▼
                                   CustomerRepository ─► DapperContext ─► dbo.Customer_Insert
                                   (one method per proc)  (proc only, timed,  (THROW 50409 on
                                                           errors → Result)    duplicate email)
```

## How the standards are enforced

Each standard has at least one mechanism that fails loudly when it is broken. Documentation alone
doesn't count as enforcement.

### Dates: `dd-MMM-yyyy` everywhere

| Where | Mechanism | What happens if broken |
|---|---|---|
| SQL storage | `DATE` columns; `ConventionTests.Sql_never_turns_dates_into_text` | Test fails if a `.sql` file uses `FORMAT(` or `CONVERT(VARCHAR, …, style)` |
| C# formatting and parsing | `DateFormat` is the only place; `BannedSymbols.txt` bans `DateTime.Parse`, `DateOnly.TryParse`, `DateTime/DateOnly.ToString(...)`, `ToShortDateString` | **Compile error** RS0030 pointing to `DateFormat` |
| "Today" | `BusinessClock` (Asia/Kolkata); `DateTime.Now/UtcNow/Today` and `DateTimeOffset.Now/UtcNow` banned | **Compile error**. A UTC server would otherwise say it is still yesterday until 05:30 IST |
| Form input → C# | `DateModelBinder` accepts only `19-Nov-2026` | Field error: "Enter Customer since as dd-MMM-yyyy" |
| C# → form input | `DateInputTagHelper` rewrites every `<input asp-for="SomeDateOnly">` to a text box in house format with the date picker attached | Nothing to remember: a plain `asp-for` is already correct |
| JSON (AJAX, DataTables) | `DateJsonConverters` both ways | Wrong format in a request body is a 400; responses are already formatted, so JavaScript never formats dates |
| Date picker | Format comes from `DateFormat.DatePickerFormat` via a `<meta>` tag | Change the format in one constant and C#, JSON and the picker all follow |
| Display | `@Html.DisplayFor(m => m.Date)` (display template) or `date.ToDisplay()` | |

**Known gaps** (code review must catch these): string interpolation `$"{date}"` and writing
`@Model.SomeDate` raw in Razor both call `ToString()` in ways the analyzer cannot see.

### Stored procedures only, through DapperContext

| Mechanism | What happens if broken |
|---|---|
| `Dapper.SqlMapper`, `new SqlConnection(string)` and `new SqlCommand(string, …)` are banned outside `DapperContext` | **Compile error**: no inline SQL anywhere else |
| `DapperContext` fixes `CommandType.StoredProcedure` on every call | Inline SQL can't be passed through it |
| `ConventionTests.Every_procedure_a_repository_calls_exists_in_the_database_project` | Test fails on a typo or a missing procedure |
| The SQL project builds the schema | A procedure naming a missing column fails `dotnet build` (SQL71501) |

`DapperContext` is scoped (one per request), and its connection opens on first use. This matches the
"`_context.connection`, then run the proc" habit, and it is what will make a transaction across
several repositories simple. The rule that comes with it: **never run two calls on the same
context at once** (no `Task.WhenAll` over repository calls).

### Errors

| Kind | Example | How it travels | User sees |
|---|---|---|---|
| Expected | Invalid email, duplicate email, stale edit, not found | `Result` / `Result<T>` carrying `Error(Code, Message, Type, Field)`. Stored procedures raise them with `THROW 504xx, N'Code\|Message'`; `SqlErrorTranslator` converts. | The message next to the field, or at the top of the form |
| Unexpected | Bug, database down, timeout | Exception. Not caught in services or repositories. | "Something went wrong" plus a reference |

- **One log site.** `GlobalExceptionHandler` logs each unhandled exception once, with the real
  path. Nothing else catches an exception just to log it and rethrow.
- **One response site.** `ErrorController` renders every error, both exceptions and empty 4xx/5xx.
  AJAX callers get `application/problem+json`; browsers get a page.
- **Never leaks internals.** Integration tests make the repository throw an exception whose message
  contains a fake password, then check that neither the JSON nor the HTML contains it.
- **The reference is the trace id.** The user sees the W3C trace id, which Serilog writes on every
  log line of that request (`@tr` in the JSON logs). Support can find the whole request from it.

### Logging (Serilog)

- Configured in `appsettings.json` (`Serilog` section): console plus rolling daily JSON files under
  `logs/`, kept for 30 days. Change sinks there, not in code.
- `UseSerilogRequestLogging` writes one line per request: method, path, status, elapsed time.
- Log calls use `[LoggerMessage]` source-generated methods. `DapperContext` logs each procedure's
  elapsed time at Debug, slow calls (above `Database:SlowCallMilliseconds`) at Warning, and
  business refusals at Information. **Parameter values are never logged**, because they hold personal data.
- `Console.WriteLine` is banned.

### Everything else

| Standard | Mechanism |
|---|---|
| Layer boundaries | `LayerTests`: Domain references nothing, Application has no SQL or ASP.NET, no controller depends on an Infrastructure type, repository implementations live only in Infrastructure |
| Write procedures roll back on error | `ConventionTests.Write_procedures_set_xact_abort` |
| CSRF | `AutoValidateAntiforgeryTokenAttribute` is global; `site.js` sends the token on every AJAX call. A test posts without it and expects 400 |
| Stored XSS in grids | `app.dataTable` escapes every cell unless a column supplies its own renderer. Checked in a browser with a customer named `<img src=x onerror=alert(1)>` |
| Lost updates | `ROWVERSION` round-trips through the form; the update procedure refuses a stale version. Integration test simulates two users |
| Warnings | `TreatWarningsAsErrors` for C# and SQL; `.editorconfig` style enforced in the build |
| Package versions | Central (`Directory.Packages.props`), so one version per package |
| Missing config | `Database:ConnectionString` is validated at startup; the app refuses to start without it |
| All of the above | `.github/workflows/ci.yml` runs build plus all three test suites on every push and PR |

## Running locally

Prerequisites: .NET 10 SDK, SQL Server (LocalDB, Developer or Docker), and Docker for the integration tests.

```bash
dotnet build
```

Deploy the schema, either by publishing the database project from Visual Studio or with sqlpackage
(see [database/README.md](database/README.md)).

Set the connection string. `appsettings.Development.json` points at `localhost` with Windows
authentication; for anything else use user-secrets, never appsettings:

```bash
dotnet user-secrets set "Database:ConnectionString" "Server=...;Database=AppTemplate;..." --project src/AppTemplate.Web
```

```bash
dotnet run --project src/AppTemplate.Web
```

Tests (integration tests need Docker running):

```bash
dotnet test --solution AppTemplate.sln
```

Running a non-published build with `ASPNETCORE_ENVIRONMENT=Production` serves empty CSS and JS:
static assets are precompressed only by `dotnet publish`. Use Development locally and publish for
servers.

## Adding a feature (the recipe)

1. **Database:** table and procedures in `database/`, following `database/README.md`.
2. **Domain:** entity with a private constructor, `Create` returning `Result<T>`, `Rehydrate` for
   loading, and errors in an `<Entity>Errors` class.
3. **Application:** `I<Entity>Repository`, `<Entity>Service`, input and output records.
   Register in `DependencyInjection.cs`.
4. **Infrastructure:** `<Entity>Repository` with procedure names as constants, one method per
   procedure, and a private row class when the result has a `DateOnly`. Register in `DependencyInjection.cs`.
5. **Web:** controller (bind → service → respond), form model, views, `app.dataTable(...)` for lists.
6. **Tests:** domain rules (unit), service (unit, with a fake repository), repository against real
   procedures (integration), one HTTP test for anything unusual.

## Decisions worth knowing

- **No `ICustomerService` interface.** An interface earns its place when there is a second
  implementation or a boundary to cross. The repository interface exists because Application must
  not reference Infrastructure; the service has neither reason. Tests use the real service with a
  fake repository.
- **Hand-written fakes, no mocking library.** They are plain code you can step through, and tests
  check outcomes rather than which methods were called.
- **Validation runs twice, deliberately.** DataAnnotations on the form model give instant
  client-side feedback. The entity is the authority. Max lengths come from the entity's constants,
  so the two can't drift.
- **`DateOnly` for calendar dates.** No time and no time zone, so it never shifts a day.
- **Integration tests deploy the dacpac.** They test the exact artifact that goes to production.

## Not in this slice yet

Authentication and authorization · transactions across repositories (unit of work) · AJAX
business errors (ProblemDetails from a `Result`) · `dotnet new` template packaging · audit columns
(who changed what) · health checks.
