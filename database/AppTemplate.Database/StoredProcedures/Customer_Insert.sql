-- Write procedure convention:
--   SET XACT_ABORT ON: any error rolls back the whole procedure.
--   Business refusals: THROW 504xx, N'Code|Message', 1;  (see SqlErrorTranslator)
--   Returns the new id as a single value.
CREATE PROCEDURE dbo.Customer_Insert
    @Name          NVARCHAR(200),
    @Email         NVARCHAR(256),
    @Phone         NVARCHAR(30) = NULL,
    @CustomerSince DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.Customer WHERE Email = @Email)
        THROW 50409, N'Customer.EmailTaken|Another customer already uses this email.', 1;

    INSERT dbo.Customer (Name, Email, Phone, CustomerSince)
    OUTPUT inserted.CustomerId
    VALUES (@Name, @Email, @Phone, @CustomerSince);
END;
