-- Tables are singular (Customer, not Customers). Constraints are named, so a schema compare
-- or a deployment script never sees a random system-generated name change.
CREATE TABLE dbo.Customer
(
    CustomerId    INT           IDENTITY(1, 1) NOT NULL,
    Name          NVARCHAR(200) NOT NULL,
    Email         NVARCHAR(256) NOT NULL,
    Phone         NVARCHAR(30)  NULL,
    CustomerSince DATE          NOT NULL,   -- A calendar date: DATE, never DATETIME, never a string.
    IsActive      BIT           NOT NULL CONSTRAINT DF_Customer_IsActive DEFAULT (1),
    CreatedAtUtc  DATETIME2(3)  NOT NULL CONSTRAINT DF_Customer_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedAtUtc  DATETIME2(3)  NULL,       -- Instants are stored in UTC and converted for display.
    RowVersion    ROWVERSION    NOT NULL,   -- Changes on every update; used to refuse stale edits.

    CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED (CustomerId),
    CONSTRAINT UQ_Customer_Email UNIQUE (Email)
);
