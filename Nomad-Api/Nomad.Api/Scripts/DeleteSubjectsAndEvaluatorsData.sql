-- Script to delete all Subjects and Evaluators data before schema migration
-- Run this BEFORE creating the migration

-- Delete all SubjectEvaluator relationships first (due to FK constraints)
DELETE FROM "SubjectEvaluators";

-- Delete all Subjects
DELETE FROM "Subjects";

-- Delete all Evaluators
DELETE FROM "Evaluators";

-- Verify deletion
SELECT COUNT(*) AS "SubjectEvaluators Count" FROM "SubjectEvaluators";
SELECT COUNT(*) AS "Subjects Count" FROM "Subjects";
SELECT COUNT(*) AS "Evaluators Count" FROM "Evaluators";

