CREATE PROCEDURE dbo.Customer_Update
    @CustomerId    INT,
    @Name          NVARCHAR(200),
    @Email         NVARCHAR(256),
    @Phone         NVARCHAR(30) = NULL,
    @CustomerSince DATE,
    @IsActive      BIT,
    @RowVersion    BINARY(8)    -- The version the user loaded.
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerId = @CustomerId)
        THROW 50404, N'Customer.NotFound|Customer not found.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Customer WHERE Email = @Email AND CustomerId <> @CustomerId)
        THROW 50409, N'Customer.EmailTaken|Another customer already uses this email.', 1;

    UPDATE dbo.Customer
    SET Name          = @Name,
        Email         = @Email,
        Phone         = @Phone,
        CustomerSince = @CustomerSince,
        IsActive      = @IsActive,
        UpdatedAtUtc  = SYSUTCDATETIME()
    WHERE CustomerId = @CustomerId
      AND RowVersion = @RowVersion;   -- Optimistic concurrency: no match means someone saved first.

    IF @@ROWCOUNT = 0
        THROW 50409, N'Customer.Stale|Someone else changed this customer after you opened it. Reload and try again.', 1;
END;
