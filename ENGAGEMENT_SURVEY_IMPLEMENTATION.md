# Engagement Survey — Implementation Change List

This document enumerates **every file** that will be created or modified to integrate the Engagement Survey type, organized by layer (Database, Backend, Frontend). Default behavior for existing data is preserved by defaulting `SurveyType = ThreeSixty`.

Legend: **[NEW]** = new file, **[MOD]** = modify existing file, **[OPT]** = optional / nice-to-have.

---

## 1. Database Changes

All schema changes are delivered through a single EF Core migration.

### 1.1 Migration

| Action | Path | Notes |
|---|---|---|
| **[NEW]** | `Alpha-Api/Alpha.Api/Migrations/<timestamp>_AddSurveyTypeAndQuestionAppliesTo.cs` | Generated via `dotnet ef migrations add AddSurveyTypeAndQuestionAppliesTo` |
| **[MOD]** | `Alpha-Api/Alpha.Api/Migrations/AlphaSurveysDbContextModelSnapshot.cs` | Auto-updated by EF |

### 1.2 Schema diff

**Table `Surveys`**
- Add column `Type` `int NOT NULL DEFAULT 0`
- Add index `IX_Surveys_TenantId_Type` on `(TenantId, Type)`

**Table `Questions`**
- Add column `AppliesTo` `int NOT NULL DEFAULT 0`

**No structural changes** to: `SubjectEvaluators`, `SurveySubmissions`, `SubjectEvaluatorSurveys`, `Subjects`, `Evaluators`, `Employees`, `Clusters`, `Competencies`, `SurveySettings`, `ReportChartImages`, `ReportTemplates`.

### 1.3 Backfill

- Existing `Surveys` rows → `Type = 0` (ThreeSixty). Behavior preserved.
- Existing `Questions` rows → `AppliesTo = 0` (Both).

---

## 2. Backend Changes (`Alpha-Api/Alpha.Api`)

### 2.1 Enums

| Action | Path | Purpose |
|---|---|---|
| **[NEW]** | `Enums/SurveyType.cs` | `enum SurveyType { ThreeSixty = 0, Engagement = 1 }` |
| **[NEW]** | `Enums/QuestionAppliesTo.cs` | `enum QuestionAppliesTo { Both = 0, ThreeSixtyOnly = 1, EngagementOnly = 2 }` |

### 2.2 Entities

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Entities/Survey.cs` | Add `public SurveyType Type { get; set; } = SurveyType.ThreeSixty;` |
| **[MOD]** | `Entities/Question.cs` | Add `public QuestionAppliesTo AppliesTo { get; set; } = QuestionAppliesTo.Both;` |

### 2.3 DbContext

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Data/AlphaSurveysDbContext.cs` | Map `Survey.Type` and `Question.AppliesTo` as `int`; add compound index `(TenantId, Type)` on Survey |

### 2.4 DTOs

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `DTOs/Request/SurveyRequest.cs` | Add `SurveyType Type` (required on create) |
| **[MOD]** | `DTOs/Response/SurveyResponse.cs` | Expose `Type` |
| **[MOD]** | `DTOs/Request/QuestionRequest.cs` | Add `AppliesTo` |
| **[MOD]** | `DTOs/Response/QuestionResponse.cs` | Expose `AppliesTo` |
| **[MOD]** | `DTOs/Request/SurveyAssignmentRequest.cs` | Allow employee-only assignment payload (no evaluators) for Engagement |
| **[MOD]** | `DTOs/Request/AssignSurveyCsvRequest.cs` | New CSV shape: single-column employee list when survey is Engagement |
| **[NEW]** | `DTOs/Request/AssignEngagementRequest.cs` | `{ Guid SurveyId; List<Guid> EmployeeIds; }` |
| **[NEW]** | `DTOs/Response/EngagementReportResponse.cs` | Tenant-wide aggregate (cluster/competency averages, distribution) |
| **[MOD]** | `DTOs/Response/SurveyResponse.cs` | (already covered above) |

### 2.5 Mappings

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Mappings/*.cs` (Survey + Question profiles) | Map new fields both directions |

### 2.6 Repository / Services

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Repository/SurveyRepository.cs` (or equivalent) | Filter by `Type`; `Type` is immutable on update once submissions exist |
| **[MOD]** | `Repository/QuestionRepository.cs` | `GetByCompetency(..., SurveyType?)` filter on `AppliesTo` |
| **[MOD]** | `Services/SurveyService.cs` | Validate Engagement schema (no evaluator placeholders); enforce immutability of `Type` |
| **[MOD]** | `Services/QuestionService.cs` | When `AppliesTo == EngagementOnly`, require `SelfQuestion`; when `ThreeSixtyOnly`, require `OthersQuestion` |
| **[NEW]** | `Services/EngagementAssignmentService.cs` | For each employee: ensure `Subject` + `Evaluator` exist, create `SubjectEvaluator` with `Relationship="Self"`, create `SubjectEvaluatorSurvey` |
| **[MOD]** | `Services/SubjectEvaluatorService.cs` (or controller-backed service) | Reject evaluator-other assignments for Engagement surveys |
| **[MOD]** | `Services/ReportingService.cs` | Branch on `survey.Type`: Engagement skips Self-vs-Evaluator comparison; produces self-only aggregates |
| **[NEW]** | `Services/EngagementReportingService.cs` | Tenant-wide aggregation (cluster/competency averages across all self submissions, optional department/team breakdown) |
| **[MOD]** | `Services/SurveySettingsService.cs` | Per-type defaults; ignore evaluator-only settings for Engagement |
| **[MOD]** | `Services/ReportTemplateService.cs` | Filter templates by `Type`; default template per type |

### 2.7 Controllers

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Controllers/SurveysController.cs` | Accept `Type` on create; reject changes on update; `GET ?type=` filter |
| **[MOD]** | `Controllers/QuestionsController.cs` | `GET ?surveyType=` filter; validation for `AppliesTo` |
| **[MOD]** | `Controllers/ParticipantController.cs` | Engagement assignment branch |
| **[MOD]** | `Controllers/SubjectEvaluatorController.cs` | Reject evaluator linking when survey is Engagement |
| **[MOD]** | `Controllers/SubjectsController.cs` | No functional change; ensures Engagement self-subjects show up |
| **[MOD]** | `Controllers/EvaluatorsController.cs` | Hide auto-created Self evaluator rows from default listings (filter `Relationship != "Self"`) |
| **[MOD]** | `Controllers/ReportController.cs` | Add `GET /reports/engagement/tenant/{tenantId}`; existing per-subject endpoints branch internally |
| **[MOD]** | `Controllers/ReportingController.cs` | Same branching for analytics endpoints |
| **[MOD]** | `Controllers/ReportTemplateController.cs` | Templates scoped by `SurveyType` |
| **[MOD]** | `Controllers/ReportTemplateSettingsController.cs` | Same |
| **[MOD]** | `Controllers/SurveySettingsController.cs` | Per-type defaults |

### 2.8 Authorization

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Authorization/AuthorizationPolicies.cs` | (Likely no change) Engagement endpoints reuse existing tenant-admin / participant policies |

### 2.9 Seed / Sample data

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Data/ClusterDataBank.cs` | Tag the existing "Engagement of Self" cluster's questions as `AppliesTo = EngagementOnly` so they don't pollute 360 surveys |
| **[OPT][MOD]** | `DataExtractor/ClusterDataBank.cs` | Mirror the same flag if used |

### 2.10 Tests (`Alpha-Api/Alpha.Api.Tests`)

| Action | Path | Change |
|---|---|---|
| **[NEW]** | `Integration/EngagementSurveyTests.cs` | Create engagement survey, assign to employees, submit self-evaluation, fetch tenant report |
| **[NEW]** | `Integration/SurveyTypeImmutabilityTests.cs` | Cannot change `Type` after submissions |
| **[NEW]** | `Integration/QuestionAppliesToFilterTests.cs` | Question Bank filter respects `AppliesTo` |
| **[MOD]** | `Integration/ReportingRegressionTests.cs` (or equivalent) | Confirm 360 reports unchanged after migration |

---

## 3. Frontend Changes (`Alpha-UI`)

### 3.1 Types

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/types/survey.ts` | Add `enum SurveyType`, `enum QuestionAppliesTo`; add `type` to `Survey`, `appliesTo` to `Question` |
| **[MOD]** | `src/types/questions.ts` | Add `appliesTo` field |

### 3.2 Services / API client

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/services/surveyService.ts` (or equivalent) | Send/receive `type`; new `assignEngagementSurvey(surveyId, employeeIds[])` |
| **[MOD]** | `src/services/questionService.ts` | `getQuestions({ surveyType? })` |
| **[MOD]** | `src/services/reportService.ts` | New `getEngagementTenantReport(tenantId)`; per-subject report unchanged |

### 3.3 Survey Tab

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/components/project/ProjectSurveysTab.tsx` | Add Survey Type picker (`360 Feedback` / `Engagement`) in create modal; show type badge on cards; filter chip; disable type editing on existing surveys |

### 3.4 Question Bank Tab

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/components/project/ProjectQuestionsTab.tsx` | Filter dropdown (All / 360 / Engagement); `AppliesTo` selector on editor; hide Others/Self field per scope |
| **[MOD]** | `src/components/project/ProjectQuestionsTab.module.css` | Styling for new badge/filter |

### 3.5 Participants Tab

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/components/project/ProjectParticipantsTab.tsx` | If `survey.type === Engagement`: hide Evaluators sub-tab and SubjectEvaluator linking UI; show simple "Add Employees" list |
| **[MOD]** | `src/components/project/ProjectEvaluatorsTab.tsx` | Hidden for Engagement (parent gates render) |
| **[MOD]** | `src/components/project/ProjectSubjectsTab.tsx` | For Engagement, label as "Employees"; CSV upload uses single-column template |
| **[MOD]** | `src/components/project/SubjectSelectorModal.tsx` | Hide evaluator-linking step for Engagement |

### 3.6 Survey Builder

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/components/survey/CustomSurveyBuilder.tsx` | Accept `surveyType` prop; for Engagement hide rater-group preview, hide relationship-based conditional visibility, only show Self question text |
| **[MOD]** | `src/components/survey/QuestionEditor.tsx` | Hide "Others Question" field when scope is `EngagementOnly` |
| **[MOD]** | `src/components/survey/QuestionRenderer.tsx` | Suppress evaluator placeholders for Engagement |
| **[MOD]** | `src/components/survey/PreviewModal.tsx` | Single self-preview mode for Engagement |
| **[MOD]** | `src/components/survey/PageEditor.tsx` | Disable relationship-conditional pages for Engagement |
| **[MOD]** | `src/components/survey/SurveySettingsTab.tsx` | Hide evaluator-only settings for Engagement |

### 3.7 Reports Tab

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/components/project/ProjectReportsTab.tsx` | Branch on `survey.type`: 360 → existing viewer; Engagement → new `EngagementReportView` |
| **[NEW]** | `src/components/project/EngagementReportView.tsx` | Org-wide cluster/competency averages, distribution histograms, optional department drill-down, PDF export |
| **[MOD]** | `src/components/project/ProjectAnalyticsTab.tsx` | Add engagement aggregate widgets |

### 3.8 Participant Portal

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/app/[tenantSlug]/participant/dashboard/page.tsx` | Surface engagement assignments separately ("My Survey") |
| **[MOD]** | `src/app/[tenantSlug]/participant/evaluations/page.tsx` | Split engagement vs 360 sections; relabel for self-form |
| **[MOD]** | `src/app/[tenantSlug]/participant/submissions/page.tsx` | For engagement, show only the user's own submission |
| **[MOD]** | `src/app/[tenantSlug]/participant/help/page.tsx` | Add engagement help section |

### 3.9 Admin / Project Dashboards

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `src/app/admin/dashboard/page.tsx` | Surface engagement survey metric tile |
| **[MOD]** | `src/app/projects/[slug]/dashboard/page.tsx` | Pass `surveyType` context down to tabs |

### 3.10 Sample data / fixtures

| Action | Path | Change |
|---|---|---|
| **[OPT][MOD]** | `src/sample_data/*` | Add a sample engagement survey for local dev previews |

---

## 4. Cross-cutting / Documentation

| Action | Path | Change |
|---|---|---|
| **[MOD]** | `Alpha-Api/README.md` | Document SurveyType and Engagement flow |
| **[MOD]** | `Alpha-Api/Alpha.Api/README_REPORT_GENERATION.md` | Add Engagement report section |
| **[MOD]** | `Alpha-UI/README.md` | UI flow notes |
| **[OPT][NEW]** | `Alpha-Api/ENGAGEMENT_SURVEY_GUIDE.md` | End-to-end guide for tenant admins |

---

## 5. Rollout Order

1. **DB + Enums + Entities + DbContext + DTOs + Mappings** — invisible to users; defaults preserve all current behavior.
2. **Backend services + controllers (validation + reporting branch + assignment service)** — still no UI exposure.
3. **Frontend: type picker, builder, participants, question bank scoping**.
4. **Frontend: Engagement report view + tenant aggregate endpoint wiring**.
5. **Tests + docs**.

Each step is independently deployable. `Engagement` cannot be selected in the UI until step 3, so the system is safe to ship after every step.

---

## 6. Risk Register (summary)

| Risk | Mitigation |
|---|---|
| Existing 360 reports regress | Default `Type = ThreeSixty`; regression test snapshot before/after |
| Type changed mid-cycle | Service-layer immutability check once `SurveySubmission` rows exist |
| Engagement schema contains evaluator placeholders | Validate on `POST/PUT /surveys` |
| Tenant data leakage in aggregate report | All queries filter by `TenantId` first; reuse existing RBAC policies |
| Question reuse across types | `AppliesTo = Both` covers shared questions; UI shows scope chip on every card |

---

**Total file impact (estimate):** ~12 new files, ~40 modified files, 1 EF migration.
