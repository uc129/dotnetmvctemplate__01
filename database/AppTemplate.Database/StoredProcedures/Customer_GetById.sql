-- Read procedures never THROW for "not found": they return no rows and the repository returns null.
CREATE PROCEDURE dbo.Customer_GetById
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.CustomerId, c.Name, c.Email, c.Phone, c.CustomerSince, c.IsActive, c.RowVersion
    FROM dbo.Customer AS c
    WHERE c.CustomerId = @CustomerId;
END;
