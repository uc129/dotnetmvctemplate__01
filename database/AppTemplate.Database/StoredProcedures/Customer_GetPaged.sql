-- Paging convention (DapperContext.QueryPagedAsync relies on it):
--   result set 1: the page of rows
--   result set 2: one row with TotalCount and FilteredCount
-- @SortColumn is compared, never concatenated: the CASE list below is the whitelist, and an
-- unknown name falls through to the CustomerId tie-breaker. No dynamic SQL, no injection.
CREATE PROCEDURE dbo.Customer_GetPaged
    @Start         INT,
    @Length        INT,
    @Search        NVARCHAR(100) = NULL,
    @SortColumn    NVARCHAR(50)  = NULL,
    @SortDirection VARCHAR(4)    = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;

    -- Escape LIKE wildcards so a search for "50%" means the text "50%".
    DECLARE @Pattern NVARCHAR(410) =
        CASE WHEN @Search IS NULL THEN NULL
             ELSE N'%' + REPLACE(REPLACE(REPLACE(@Search, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%'
        END;

    SELECT c.CustomerId, c.Name, c.Email, c.Phone, c.CustomerSince, c.IsActive
    FROM dbo.Customer AS c
    WHERE @Pattern IS NULL
       OR c.Name  LIKE @Pattern
       OR c.Email LIKE @Pattern
       OR c.Phone LIKE @Pattern
    ORDER BY
        -- One CASE per column and direction, so each keeps its own type (no implicit conversion).
        CASE WHEN @SortColumn = 'Name'          AND @SortDirection = 'ASC'  THEN c.Name END ASC,
        CASE WHEN @SortColumn = 'Name'          AND @SortDirection = 'DESC' THEN c.Name END DESC,
        CASE WHEN @SortColumn = 'Email'         AND @SortDirection = 'ASC'  THEN c.Email END ASC,
        CASE WHEN @SortColumn = 'Email'         AND @SortDirection = 'DESC' THEN c.Email END DESC,
        CASE WHEN @SortColumn = 'CustomerSince' AND @SortDirection = 'ASC'  THEN c.CustomerSince END ASC,
        CASE WHEN @SortColumn = 'CustomerSince' AND @SortDirection = 'DESC' THEN c.CustomerSince END DESC,
        CASE WHEN @SortColumn = 'IsActive'      AND @SortDirection = 'ASC'  THEN c.IsActive END ASC,
        CASE WHEN @SortColumn = 'IsActive'      AND @SortDirection = 'DESC' THEN c.IsActive END DESC,
        c.CustomerId ASC   -- Tie-breaker: without it, rows with equal sort values can repeat across pages.
    OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY
    -- Catch-all search: RECOMPILE gives each call a plan for its actual parameters.
    OPTION (RECOMPILE);

    SELECT
        TotalCount    = (SELECT COUNT(*) FROM dbo.Customer),
        FilteredCount = (SELECT COUNT(*)
                         FROM dbo.Customer AS c
                         WHERE @Pattern IS NULL
                            OR c.Name  LIKE @Pattern
                            OR c.Email LIKE @Pattern
                            OR c.Phone LIKE @Pattern)
    OPTION (RECOMPILE);
END;
