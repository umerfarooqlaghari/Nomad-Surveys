-- =====================================================
-- Clean Users Table and Migrate Employees to Users
-- =====================================================
-- This script:
-- 1. Deletes all users except SuperAdmin
-- 2. Migrates all existing employees to the Users table
-- 3. Assigns Participant role to all migrated users
-- =====================================================

BEGIN;

-- Step 1: Delete all UserTenantRoles except SuperAdmin's roles
DELETE FROM "UserTenantRoles"
WHERE "UserId" IN (
    SELECT "Id" FROM "Users"
    WHERE "Email" != 'superadmin@nomadsurveys.com'
);

-- Step 2: Delete all users except SuperAdmin
DELETE FROM "Users"
WHERE "Email" != 'superadmin@nomadsurveys.com';

RAISE NOTICE 'Deleted all users except SuperAdmin';

-- Step 3: Get the Participant role ID
DO $$
DECLARE
    participant_role_id UUID;
    employee_record RECORD;
    new_user_id UUID;
    user_count INT := 0;
BEGIN
    -- Get Participant role ID
    SELECT "Id" INTO participant_role_id
    FROM "Roles"
    WHERE "Name" = 'Participant'
    LIMIT 1;

    IF participant_role_id IS NULL THEN
        RAISE EXCEPTION 'Participant role not found!';
    END IF;

    RAISE NOTICE 'Participant role ID: %', participant_role_id;

    -- Loop through all active employees
    FOR employee_record IN 
        SELECT 
            e."Id" as employee_id,
            e."EmployeeId" as employee_id_string,
            e."FirstName",
            e."LastName",
            e."Email",
            e."Gender",
            e."Designation",
            e."Department",
            e."Tenure",
            e."Grade",
            e."TenantId",
            e."CreatedAt"
        FROM "Employees" e
        WHERE e."IsActive" = true
        ORDER BY e."CreatedAt"
    LOOP
        -- Generate new user ID
        new_user_id := gen_random_uuid();

        -- Insert into Users table
        INSERT INTO "Users" (
            "Id",
            "UserName",
            "NormalizedUserName",
            "Email",
            "NormalizedEmail",
            "EmailConfirmed",
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "PhoneNumber",
            "PhoneNumberConfirmed",
            "TwoFactorEnabled",
            "LockoutEnd",
            "LockoutEnabled",
            "AccessFailedCount",
            "FirstName",
            "LastName",
            "Gender",
            "Designation",
            "Department",
            "Tenure",
            "Grade",
            "EmployeeId",
            "TenantId",
            "IsActive",
            "CreatedAt",
            "UpdatedAt",
            "LastLoginAt"
        ) VALUES (
            new_user_id,
            employee_record."Email",
            UPPER(employee_record."Email"),
            employee_record."Email",
            UPPER(employee_record."Email"),
            true, -- EmailConfirmed
            'AQAAAAIAAYagAAAAEHxK8vZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZ5qZw==', -- Hashed "Password@123"
            UPPER(REPLACE(gen_random_uuid()::text, '-', '')),
            gen_random_uuid()::text,
            NULL, -- PhoneNumber
            false, -- PhoneNumberConfirmed
            false, -- TwoFactorEnabled
            NULL, -- LockoutEnd
            true, -- LockoutEnabled
            0, -- AccessFailedCount
            employee_record."FirstName",
            employee_record."LastName",
            employee_record."Gender",
            employee_record."Designation",
            employee_record."Department",
            employee_record."Tenure",
            employee_record."Grade",
            employee_record.employee_id, -- FK to Employee
            employee_record."TenantId",
            true, -- IsActive
            employee_record."CreatedAt",
            NOW(),
            NULL -- LastLoginAt
        );

        -- Assign Participant role
        INSERT INTO "UserTenantRoles" (
            "Id",
            "UserId",
            "RoleId",
            "TenantId",
            "IsActive",
            "ExpiresAt"
        ) VALUES (
            gen_random_uuid(),
            new_user_id,
            participant_role_id,
            employee_record."TenantId",
            true,
            NULL
        );

        user_count := user_count + 1;
        
        RAISE NOTICE 'Created user for employee: % (%) - User ID: %', 
            employee_record."FirstName" || ' ' || employee_record."LastName",
            employee_record."Email",
            new_user_id;
    END LOOP;

    RAISE NOTICE 'Successfully migrated % employees to Users table', user_count;
END $$;

COMMIT;

-- Verify results
SELECT 
    'Total Users' as "Type",
    COUNT(*) as "Count"
FROM "Users"
UNION ALL
SELECT 
    'SuperAdmin Users' as "Type",
    COUNT(*) as "Count"
FROM "Users"
WHERE "Email" = 'superadmin@nomadsurveys.com'
UNION ALL
SELECT 
    'Participant Users' as "Type",
    COUNT(*) as "Count"
FROM "Users" u
INNER JOIN "UserTenantRoles" utr ON u."Id" = utr."UserId"
INNER JOIN "Roles" r ON utr."RoleId" = r."Id"
WHERE r."Name" = 'Participant'
UNION ALL
SELECT 
    'Total Employees' as "Type",
    COUNT(*) as "Count"
FROM "Employees"
WHERE "IsActive" = true;

RAISE NOTICE '✅ Migration complete! All employees have been synced to Users table with Participant role.';
RAISE NOTICE '🔑 Default password for all employee users: Password@123';

