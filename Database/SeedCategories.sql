-- ============================================================================
-- DEVELOPMENT ONLY — do not run on production.
-- ============================================================================
-- Legacy category seed (Categories.txt). ParentCategory_I NULL = top-level.
-- Idempotent MERGE by Category_I; safe to re-run after CreateTables.sql in dev.
-- Production: use CreateTables.sql only; add categories via the API or your import.
-- Aspire local SQL also seeds from LegacyCategorySeed.cs (not used in Azure deploy).
-- ============================================================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
BEGIN
    SET IDENTITY_INSERT [dbo].[Categories] ON;

    MERGE [dbo].[Categories] AS t
    USING (VALUES
        (3, N'Tithe', 90, 1, 0),
        (4, N'Housing', NULL, 0, 0),
        (5, N'Transportation', NULL, 0, 0),
        (6, N'Groceries', NULL, 1, 0),
        (7, N'Entertainment', NULL, 0, 0),
        (8, N'Insurance', NULL, 0, 0),
        (9, N'PhoneCards', NULL, 1, 0),
        (10, N'Cell Phone', NULL, 1, 0),
        (12, N'Education', NULL, 1, 0),
        (13, N'Investments', NULL, 1, 0),
        (14, N'Doctor Visit', 75, 1, 0),
        (15, N'Clothing', NULL, 0, 0),
        (16, N'Other Giving', 90, 1, 0),
        (17, N'Miscellaneous', NULL, 0, 0),
        (18, N'Gas - Car', 5, 1, 0),
        (19, N'Split', NULL, 0, 0),
        (20, N'Savings', NULL, 0, 0),
        (21, N'Dining/Eating Out', 7, 0, 0),
        (42, N'Vacation', NULL, 0, 0),
        (43, N'Health', 75, 0, 0),
        (44, N'Wedding', 46, 0, 0),
        (45, N'Baby', NULL, 1, 0),
        (46, N'Special Occasions', NULL, 0, 0),
        (48, N'Prescriptions', 75, 1, 0),
        (49, N'Medicine/Drugs', 75, 1, 0),
        (50, N'Dental', 75, 1, 0),
        (52, N'Electricity', 4, 1, 0),
        (53, N'Cable/Internet', 4, 1, 0),
        (54, N'Auto Maintenance', 5, 1, 0),
        (55, N'Auto Insurance', 5, 1, 0),
        (56, N'Other Income', NULL, 0, 0),
        (57, N'Mortgage/Rent', 4, 1, 0),
        (58, N'House Insurance', 4, 1, 0),
        (60, N'Natural Gas', 4, 0, 0),
        (61, N'Water', 4, 1, 0),
        (64, N'Auto Payment', 5, 1, 0),
        (66, N'Auto Taxes', 5, 0, 0),
        (68, N'Life Insurance', 8, 1, 0),
        (69, N'Health Insurance', 8, 0, 0),
        (70, N'Baby-sitters', 7, 0, 0),
        (71, N'Activities/Trips', NULL, 0, 0),
        (75, N'Medical Expenses', NULL, 0, 0),
        (78, N'Personal Care', NULL, 0, 0),
        (80, N'School Tuition', 12, 1, 0),
        (81, N'Other Expenses', NULL, 0, 0),
        (82, N'Other Housing', 4, 0, 0),
        (83, N'Streaming providers', 7, 1, 0),
        (84, N'Business', NULL, 1, 0),
        (86, N'Bert Cash', NULL, 1, 0),
        (87, N'Debts', NULL, 0, 0),
        (88, N'Tools', NULL, 0, 0),
        (89, N'Children', NULL, 0, 0),
        (90, N'Giving', NULL, 0, 0),
        (91, N'Offering', 90, 1, 0),
        (92, N'Uber/Taxi/Bus', 5, 0, 0),
        (94, N'School bus', 5, 0, 0),
        (95, N'Cleaning Supplies', 4, 1, 0),
        (96, N'Gifts', NULL, 0, 0),
        (97, N'Music Lessons', 12, 0, 0),
        (98, N'Sports', 12, 0, 0),
        (99, N'School Supplies', 12, 0, 0),
        (100, N'Toys/Gadgets', NULL, 0, 0),
        (101, N'Furnishings/Appliances', 4, 0, 0),
        (102, N'Pet Supplies', NULL, 0, 0),
        (103, N'Parking', 5, 0, 0),
        (104, N'Taxes', NULL, 1, 0),
        (105, N'Rocio Cash', NULL, 0, 0),
        (106, N'Vacation Food', 42, 0, 0),
        (107, N'Vacation Gnd Transport', 42, 0, 0),
        (108, N'Vacation Activity', 42, 0, 0),
        (109, N'Vacation Lodging', 42, 0, 0),
        (110, N'Vacation Restaurant', 42, 0, 0),
        (111, N'Outdoors', NULL, 0, 0)
    ) AS s ([Category_I], [Name], [ParentCategory_I], [Required], [Archived])
    ON t.[Category_I] = s.[Category_I]
    WHEN MATCHED THEN
        UPDATE SET [Name] = s.[Name], [ParentCategory_I] = s.[ParentCategory_I],
                   [Required] = s.[Required], [Archived] = s.[Archived]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Category_I], [Name], [ParentCategory_I], [Required], [Archived])
        VALUES (s.[Category_I], s.[Name], s.[ParentCategory_I], s.[Required], s.[Archived]);

    SET IDENTITY_INSERT [dbo].[Categories] OFF;
END
