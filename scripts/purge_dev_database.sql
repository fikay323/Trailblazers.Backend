-- ==============================================================================
-- Trailblazers LMS - Dev Stage Database Purge Script
-- Purpose: Reset database for clean ready-to-use launch.
-- 
-- PRESERVED:
--   1. All Exam Questions ("exam_questions") - NEVER DELETED
--   2. System Roles ("AspNetRoles" - Admin, Instructor, Student)
--   3. System Administrator account(s) ("AspNetUsers" where role is Admin or email is admin@trailblazer.edu)
--   4. Administrator role assignments ("AspNetUserRoles")
--   5. Academy attendance geolocation configuration ("attendance_settings")
--
-- PURGED / RESET:
--   - All student exam attempts and results ("exam_results", "exam_sessions")
--   - All student attendance clock-in records ("attendance_records")
--   - All authentication session tokens ("refresh_tokens")
--   - All staff invitations ("staff_invitations")
--   - All student registration and contact inquiries ("submissions")
--   - All non-admin accounts (students, instructors created during testing)
-- ==============================================================================

BEGIN;

-- 1. Delete candidate test sessions and scores
DELETE FROM "exam_results";
DELETE FROM "exam_sessions";

-- 2. Delete attendance records
DELETE FROM "attendance_records";

-- 3. Delete user session refresh tokens
DELETE FROM "refresh_tokens";

-- 4. Delete staff invitations
DELETE FROM "staff_invitations";

-- 5. Delete registration and contact inquiries
DELETE FROM "submissions";

-- 6. Collect Admin User IDs to preserve
CREATE TEMP TABLE temp_admin_ids ON COMMIT DROP AS
SELECT DISTINCT u."Id"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE UPPER(r."NormalizedName") = 'ADMIN' 
   OR LOWER(u."Email") = 'admin@trailblazer.edu';

-- 7. Clean up Identity child tables for non-admin accounts
DELETE FROM "AspNetUserRoles"
WHERE "UserId" NOT IN (SELECT "Id" FROM temp_admin_ids);

DELETE FROM "AspNetUserClaims"
WHERE "UserId" NOT IN (SELECT "Id" FROM temp_admin_ids);

DELETE FROM "AspNetUserLogins"
WHERE "UserId" NOT IN (SELECT "Id" FROM temp_admin_ids);

DELETE FROM "AspNetUserTokens"
WHERE "UserId" NOT IN (SELECT "Id" FROM temp_admin_ids);

-- 8. Delete all non-admin users from AspNetUsers
DELETE FROM "AspNetUsers"
WHERE "Id" NOT IN (SELECT "Id" FROM temp_admin_ids);

COMMIT;

-- 9. Verification Summary
SELECT 'Preserved Admin Users' AS entity, COUNT(*) AS count FROM "AspNetUsers"
UNION ALL
SELECT 'Preserved Exam Questions', COUNT(*) FROM "exam_questions"
UNION ALL
SELECT 'Remaining Submissions', COUNT(*) FROM "submissions"
UNION ALL
SELECT 'Remaining Exam Sessions', COUNT(*) FROM "exam_sessions"
UNION ALL
SELECT 'Remaining Attendance Records', COUNT(*) FROM "attendance_records"
UNION ALL
SELECT 'Remaining Staff Invitations', COUNT(*) FROM "staff_invitations";
