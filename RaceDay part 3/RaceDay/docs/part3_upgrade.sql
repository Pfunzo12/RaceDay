/* =====================================================================
   RaceDay - Part 3 upgrade for the EXISTING database RaceDayDB
   NON-DESTRUCTIVE: only ADDS columns (plus a couple of safe fix-ups
   described below). Nothing is dropped or deleted; your existing users,
   events, categories, entries and results stay.
   Safe to run more than once.
   Run in SSMS connected to localhost\SQLEXPRESS01.
   ===================================================================== */
USE RaceDayDB;
GO
SET NOCOUNT ON;

/* 0. Safety check: make sure the columns the app expects really exist. */
DECLARE @need TABLE (t SYSNAME, c SYSNAME);
INSERT INTO @need VALUES
 ('Users','user_id'),('Users','full_name'),('Users','email'),('Users','password_hash'),('Users','role'),('Users','phone'),('Users','created_at'),
 ('Events','event_id'),('Events','organiser_id'),('Events','name'),('Events','description'),('Events','event_date'),('Events','location'),('Events','event_type'),('Events','status'),('Events','created_at'),
 ('Categories','category_id'),('Categories','event_id'),('Categories','name'),('Categories','distance_km'),('Categories','price'),('Categories','max_participants'),
 ('Entries','entry_id'),('Entries','category_id'),('Entries','participant_id'),('Entries','entry_date'),('Entries','status'),
 ('Results','result_id'),('Results','entry_id'),('Results','finish_time'),('Results','position_overall'),('Results','recorded_at');
IF EXISTS (SELECT 1 FROM @need WHERE COL_LENGTH(t, c) IS NULL)
BEGIN
    SELECT t AS missing_table, c AS missing_column FROM @need WHERE COL_LENGTH(t, c) IS NULL;
    PRINT 'STOPPED: some columns the app expects were not found (see the grid). NOTHING was changed. Send a screenshot to Claude.';
    SET NOEXEC ON;   -- skips everything below
END
GO

/* 1. Users: profile fields for the Participant profile page */
IF COL_LENGTH('Users','date_of_birth')        IS NULL ALTER TABLE Users ADD date_of_birth DATE NULL;
IF COL_LENGTH('Users','emergency_contact')    IS NULL ALTER TABLE Users ADD emergency_contact VARCHAR(150) NULL;
IF COL_LENGTH('Users','club_name')            IS NULL ALTER TABLE Users ADD club_name VARCHAR(100) NULL;
IF COL_LENGTH('Users','profile_picture_path') IS NULL ALTER TABLE Users ADD profile_picture_path VARCHAR(300) NULL;
GO

/* 2. Events: distance + banner image */
IF COL_LENGTH('Events','distance_km') IS NULL
BEGIN
    ALTER TABLE Events ADD distance_km DECIMAL(6,2) NOT NULL CONSTRAINT DF_Events_distance_km DEFAULT 0;
    -- give existing events a sensible distance: their longest category
    EXEC('UPDATE e SET distance_km = ISNULL((SELECT MAX(c.distance_km) FROM Categories c WHERE c.event_id = e.event_id), 0) FROM Events e');
END
IF COL_LENGTH('Events','banner_image_path') IS NULL ALTER TABLE Events ADD banner_image_path VARCHAR(300) NULL;
GO

/* 2b. Events.description is the old TEXT type (deprecated, awkward to query).
       VARCHAR(MAX) keeps all existing text. */
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'description' AND system_type_id = 35)
    ALTER TABLE Events ALTER COLUMN description VARCHAR(MAX) NULL;
GO

/* 2c. Events.status is NOT NULL but the app does not set it, so give it a DEFAULT.
       The default is a value already used in your data (prefers 'upcoming'). */
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID('Events')
                 AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('Events'), 'status', 'ColumnId'))
BEGIN
    DECLARE @val VARCHAR(20) = (SELECT TOP 1 status FROM Events
                                ORDER BY CASE WHEN status = 'upcoming' THEN 0 WHEN status = 'published' THEN 1 WHEN status = 'open' THEN 2 ELSE 3 END, event_id);
    SET @val = ISNULL(@val, 'upcoming');
    DECLARE @sqlDefault NVARCHAR(400) = N'ALTER TABLE Events ADD CONSTRAINT DF_Events_status DEFAULT ' + QUOTENAME(@val, '''') + N' FOR status';
    EXEC (@sqlDefault);
END
GO

/* 3. Categories: age limits (age categories) */
IF COL_LENGTH('Categories','min_age') IS NULL ALTER TABLE Categories ADD min_age INT NULL;
IF COL_LENGTH('Categories','max_age') IS NULL ALTER TABLE Categories ADD max_age INT NULL;
GO

/* 4. Results: published flag. Results that already exist are treated as published
       so they keep showing for participants. New results start unpublished. */
IF COL_LENGTH('Results','is_published') IS NULL
BEGIN
    ALTER TABLE Results ADD is_published BIT NOT NULL CONSTRAINT DF_Results_is_published DEFAULT 0;
    EXEC('UPDATE Results SET is_published = 1');
END
GO

/* 5. Entries.status: the app uses the lowercase values pending / confirmed / cancelled.
       If a CHECK constraint on Entries.status does not allow 'pending', replace it with one that does. */
DECLARE @dropSql NVARCHAR(MAX) = N'';
SELECT @dropSql += N'ALTER TABLE Entries DROP CONSTRAINT ' + QUOTENAME(cc.name) + N'; '
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('Entries')
  AND cc.definition LIKE '%status%'
  AND cc.definition NOT LIKE '%pending%';
IF @dropSql <> N''
BEGIN
    EXEC (@dropSql);
    ALTER TABLE Entries ADD CONSTRAINT CK_Entries_status CHECK (status IN ('pending','confirmed','cancelled'));
    PRINT 'Entries.status CHECK constraint replaced to allow pending / confirmed / cancelled.';
END
GO

SET NOEXEC OFF;
GO

/* ---------------- Report: what the database looks like now ---------------- */
PRINT 'Upgrade finished. Check the grids below.';

-- primary keys that are not IDENTITY would make inserts fail (all rows here should be empty)
SELECT v.t AS table_name, v.c AS key_column, 'NOT identity - tell Claude' AS warning
FROM (VALUES ('Users','user_id'),('Events','event_id'),('Categories','category_id'),('Entries','entry_id'),('Results','result_id')) v(t, c)
WHERE COLUMNPROPERTY(OBJECT_ID(v.t), v.c, 'IsIdentity') = 0;

-- any CHECK constraints (if creating events/categories ever fails, send this grid to Claude)
SELECT OBJECT_NAME(parent_object_id) AS table_name, name AS constraint_name, definition
FROM sys.check_constraints
WHERE OBJECT_NAME(parent_object_id) IN ('Users','Events','Categories','Entries','Results');

SELECT DISTINCT event_type AS existing_event_types FROM Events;
SELECT DISTINCT status     AS existing_entry_status FROM Entries;
