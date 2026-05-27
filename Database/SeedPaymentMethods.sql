-- ============================================================================
-- DEVELOPMENT ONLY — do not run on production.
-- ============================================================================
-- Payment method seed (fixed IDs). Idempotent MERGE by ID.
-- Production: use CreateTables.sql only; add payment methods via the API or your import.
-- Aspire local SQL also seeds from LegacyPaymentMethodSeed.cs (not used in Azure deploy).
-- ============================================================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentMethods]') AND type in (N'U'))
BEGIN
    SET IDENTITY_INSERT [dbo].[PaymentMethods] ON;

    MERGE [dbo].[PaymentMethods] AS t
    USING (VALUES
        (1, N'Discover Checking'),
        (2, N'Discover Savings'),
        (3, N'Discover Credit'),
        (4, N'Arvest Checking'),
        (5, N'ABFCU Checking'),
        (6, N'ABFCU Savings'),
        (7, N'Bank Transfer')
    ) AS s ([ID], [PaymentMethod])
    ON t.[ID] = s.[ID]
    WHEN MATCHED THEN
        UPDATE SET [PaymentMethod] = s.[PaymentMethod]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ID], [PaymentMethod]) VALUES (s.[ID], s.[PaymentMethod]);

    SET IDENTITY_INSERT [dbo].[PaymentMethods] OFF;
END
