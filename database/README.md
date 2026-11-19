# Database conventions

The schema lives in `AppTemplate.Database` (SDK-style SQL project). `dotnet build` validates
every object against every other, so a procedure that names a missing column fails the build.
The output is `bin/<Configuration>/AppTemplate.Database.dacpac`.

## Deploying

```bash
dotnet tool install -g microsoft.sqlpackage
sqlpackage /Action:Publish /SourceFile:database/AppTemplate.Database/bin/Release/AppTemplate.Database.dacpac /TargetConnectionString:"<connection string>"
```

Use `/Action:Script` instead of `Publish` to get the change script for review before it runs on
a shared server. The dacpac describes the target state; sqlpackage works out the ALTERs.

## Naming

| Object | Pattern | Example |
|---|---|---|
| Table | Singular PascalCase | `dbo.Customer` |
| Primary key column | `<Table>Id` | `CustomerId` |
| Procedure | `<Table>_<Action>` | `dbo.Customer_GetPaged` |
| Constraint | `PK_/UQ_/FK_/DF_/CK_<Table>_<Column>` | `UQ_Customer_Email` |

## Types

- Calendar dates: `DATE`. Instants: `DATETIME2(3)` in UTC, column suffix `Utc`.
- Never store or return a date as text. Formatting happens in C# (`DateFormat`).
  An architecture test fails if a `.sql` file uses `FORMAT(` or `CONVERT(VARCHAR, ..., style)`.
- Money: `DECIMAL(18, 2)`. Never `FLOAT` or `MONEY`.
- Concurrency: a `ROWVERSION` column on any table users edit.

## Procedures

Every procedure starts with `SET NOCOUNT ON;`. Write procedures also `SET XACT_ABORT ON;`
(an architecture test checks this), so any error rolls the whole procedure back.

**Read procedures** never THROW for "not found": they return no rows.

**Paged procedures** (`*_GetPaged`) take `@Start, @Length, @Search, @SortColumn, @SortDirection`
and return two result sets: the page of rows, then one row with `TotalCount, FilteredCount`.
Sorting uses one `CASE` per column and direction, never dynamic SQL.

**Business refusals** use THROW with a number from this table and a message `Code|Text`:

| Number | Meaning | Becomes |
|---|---|---|
| 50400 | Validation | `ErrorType.Validation` |
| 50404 | Not found | `ErrorType.NotFound` (the page returns 404) |
| 50409 | Conflict (duplicate, stale edit) | `ErrorType.Conflict` |

```sql
THROW 50409, N'Customer.EmailTaken|Another customer already uses this email.', 1;
```

`SqlErrorTranslator` turns these into `Result` errors. Any other SQL error stays an exception,
is logged once and shows the user a reference number.
