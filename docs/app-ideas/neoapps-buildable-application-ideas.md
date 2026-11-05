# NeoApps.AI — Buildable Application Ideas

This document lists application concepts that can be built incrementally with the current NeoApps.AI CodeGenerator. Each concept prioritizes standard business-app capabilities: relational data, CRUD screens, authentication, role-based access, dashboards, forms, workflow statuses, document records, and AI-assisted text or extraction where supported.

> **Implementation rule:** Before starting an application, inspect the current repository and confirm which generator features are implemented. Use native capabilities first. Mark unsupported integrations, complex automation, and production hardening as future work rather than silently assuming they exist.

## Shared implementation principles

- Build a small, usable MVP with one end-to-end workflow.
- Reuse existing templates, schema conventions, authentication, authorization, and deployment patterns.
- Keep AI outputs reviewable; require human approval for consequential decisions or external submissions.
- Store workflow state and audit-relevant events explicitly.
- Use sample data for development and demos; do not include real sensitive personal data.
- Separate **MVP** requirements from **Later** enhancements.
- Document any custom code or infrastructure required beyond the generator.

---

## 1. AI-Assisted Case Review Workbench

**Purpose:** Organize document-heavy cases and help human reviewers prepare structured, evidence-linked reviews.

**Primary users:** Organization admin, case manager, reviewer, expert/quality reviewer, client.

### MVP functionality

- Create and manage clients and cases.
- Upload or attach case documents, with document metadata and processing status.
- Assign cases and review tasks to reviewers.
- Maintain structured findings, notes, dates, providers, and source references.
- Provide a case workspace with document list, findings, and review status.
- Support reviewer corrections and expert approval.
- Generate a basic case summary or exportable review report from approved information.
- Track turnaround time, task status, and reviewer workload.

### Core entities

`Organization`, `User`, `Client`, `Case`, `Document`, `ReviewTask`, `Finding`, `SourceReference`, `ReviewerAssignment`, `QualityReview`, `Deliverable`, `AuditEvent`

### Main workflow

1. Case manager creates a case and attaches documents.
2. Case is assigned to a reviewer.
3. Reviewer examines documents and records findings with source references.
4. Reviewer submits the work for quality review.
5. Expert approves or returns the review with comments.
6. Approved deliverable is made available to the client.

### MVP acceptance criteria

- Users can create, assign, update, and search cases.
- Role permissions prevent unauthorized access to cases.
- Review tasks have explicit statuses and a recorded assignee.
- Findings can link to a document and page or source location.
- A reviewer can submit work and an authorized expert can approve or return it.
- The system can produce a basic report containing approved findings.

### Later enhancements

AI extraction and chronology generation, PDF page-level evidence highlighting, automated quality scoring, client portal, billing, and integrations with external case-management systems.

---

## 2. Benefits Application Organizer

**Purpose:** Help applicants and caseworkers organize benefit applications, supporting documents, and renewal tasks. The MVP is a workflow organizer, not an official eligibility or submission service.

**Primary users:** Applicant, caseworker, program administrator, auditor.

### MVP functionality

- Maintain applicant and household profiles.
- Configure benefit programs and their document checklists.
- Create applications and track application status.
- Upload and associate supporting documents.
- Record verification requests, missing items, and caseworker notes.
- Provide a dashboard for outstanding tasks and upcoming renewals.
- Maintain a history of application status changes.

### Core entities

`Applicant`, `Household`, `HouseholdMember`, `BenefitProgram`, `Application`, `ApplicationRequirement`, `Document`, `VerificationRequest`, `CaseworkerAssignment`, `ApplicationEvent`, `Renewal`

### Main workflow

1. Applicant or caseworker creates an applicant profile.
2. Caseworker selects a benefit program and opens an application.
3. System displays a configurable checklist.
4. Applicant uploads documents and completes outstanding requirements.
5. Caseworker reviews the application and requests missing information.
6. Caseworker records the outcome and any renewal date.
7. Dashboard tracks open applications and upcoming renewals.

### MVP acceptance criteria

- Program requirements can be configured without changing application code where supported.
- Each application has a visible status, owner, and checklist.
- Documents are associated with the correct applicant and application.
- Caseworkers can request missing information and mark it received.
- Status changes and caseworker actions are recorded.
- Applicants can view their own application status but not other applicants' records.

### Later enhancements

Jurisdiction-specific eligibility rules, AI document extraction, form prefill, notifications, and authorized integrations with government systems. Eligibility guidance must cite configured rules and must not be represented as an official determination.

---

## 3. Permit and Inspection Tracker

**Purpose:** Track permit applications, document completeness, review comments, inspections, and decisions for a small municipality or permitting team.

**Primary users:** Applicant, intake officer, plan reviewer, inspector, department manager, administrator.

### MVP functionality

- Configure permit types and required documents.
- Create permit applications linked to applicants and properties.
- Track intake, review, corrections, inspection, and decision statuses.
- Assign reviewers and inspectors.
- Record review comments and correction requests.
- Schedule inspections and record results.
- Provide workload and application-aging dashboards.

### Core entities

`Applicant`, `Property`, `PermitType`, `PermitApplication`, `Requirement`, `Document`, `ReviewAssignment`, `ReviewComment`, `CorrectionRequest`, `Inspection`, `InspectionResult`, `Decision`, `AuditEvent`

### Main workflow

1. Applicant or staff creates a permit application.
2. Intake officer checks the configured document checklist.
3. Complete applications are assigned to a reviewer.
4. Reviewer records comments or requests corrections.
5. Applicant/staff submits revisions and the reviewer reassesses.
6. Inspector records an inspection result when required.
7. Authorized staff records the final decision.

### MVP acceptance criteria

- Applications can be filtered by permit type, status, property, and assignee.
- Required documents are visible and can be marked received.
- Review comments and correction requests are tracked.
- Inspections can be scheduled and their outcomes recorded.
- Only authorized roles can record final decisions.
- Dashboard shows pending applications and aging.

### Later enhancements

AI-assisted completeness checks, zoning/code reference search, drawing analysis, online payments, GIS, and integrations with municipal systems. Regulatory decisions remain with authorized officials.

---

## 4. Private Knowledge and Workflow Portal

**Purpose:** Give an organization a controlled workspace for internal documents, knowledge search, and repeatable operational workflows.

**Primary users:** Platform administrator, department manager, employee, auditor.

### MVP functionality

- Manage users, departments, and access roles.
- Upload and categorize internal documents.
- Organize documents into access-controlled collections.
- Provide document search and, if supported by the repository, AI Q&A with source references.
- Create workflow templates with steps, assignees, due dates, and statuses.
- Track workflow runs and approvals.
- Maintain an activity or audit history.

### Core entities

`Department`, `User`, `KnowledgeCollection`, `Document`, `AccessPolicy`, `WorkflowTemplate`, `WorkflowStep`, `WorkflowRun`, `Task`, `Approval`, `AuditEvent`

### Main workflow

1. Administrator configures users, roles, and document collections.
2. Employees add approved documents to collections.
3. Employee searches or asks a question within an authorized collection.
4. Employee starts a workflow from a template.
5. Tasks are assigned and completed in sequence or by configured responsibility.
6. Required approver reviews and approves or returns the work.
7. Workflow completion and activity history remain available.

### MVP acceptance criteria

- Users can only access collections permitted by their role.
- Workflow runs track current step, assignee, and status.
- Tasks can be completed, returned, or reassigned by authorized users.
- Approvals are recorded with user and timestamp.
- Search and AI answers, if enabled, respect document permissions and provide source references.

### Later enhancements

On-premises deployment automation, local model hosting, model routing, external connectors, advanced workflow orchestration, and formal security/compliance certifications. Do not claim air-gapped operation or certification unless separately implemented and verified.

---

## 5. Procurement and Supplier Tracker

**Purpose:** Help project and procurement teams manage suppliers, purchase requests, purchase orders, deliveries, and supply risks.

**Primary users:** Procurement manager, project manager, supplier, finance manager, executive.

### MVP functionality

- Maintain supplier and material catalogs.
- Create projects and associate required materials.
- Create purchase requests and approval steps.
- Record purchase orders and line items.
- Track promised and actual delivery dates.
- Record shipment updates and receiving status.
- Flag overdue deliveries and unresolved shortages.
- Provide supplier and project procurement dashboards.

### Core entities

`Project`, `Supplier`, `SupplierContact`, `Material`, `PurchaseRequest`, `PurchaseRequestLine`, `PurchaseOrder`, `PurchaseOrderLine`, `Delivery`, `ReceivingRecord`, `Risk`, `MitigationAction`

### Main workflow

1. Project manager records material requirements.
2. Procurement creates a purchase request.
3. Authorized manager approves or rejects the request.
4. Procurement records supplier and purchase order details.
5. Delivery dates and shipment status are updated.
6. Team records received quantities and discrepancies.
7. Overdue or short deliveries are flagged and assigned for mitigation.

### MVP acceptance criteria

- Purchase requests support draft, submitted, approved, rejected, and ordered statuses.
- Purchase orders link to suppliers, projects, and line items.
- Delivery status and dates can be updated and searched.
- Partial deliveries and quantity discrepancies can be recorded.
- Dashboard identifies overdue deliveries and open procurement requests.

### Later enhancements

Quotation document extraction, supplier risk scoring, predictive lead times, ERP integrations, and automated supplier communications.

---

## 6. AI Workflow Evaluation and Improvement Hub

**Purpose:** Help teams manage AI workflow versions, reference examples, human corrections, and repeatable quality evaluations.

**Primary users:** Workflow owner, domain expert, AI administrator, employee, auditor.

### MVP functionality

- Register AI workflows and versions.
- Store input examples and approved reference outputs.
- Record model outputs and human corrections.
- Define evaluation criteria and manually score results.
- Compare workflow versions using the same examples.
- Track approval and release history.
- Provide dashboards for quality, correction rates, and evaluation progress.

### Core entities

`Workflow`, `WorkflowVersion`, `ModelConfiguration`, `EvaluationDataset`, `EvaluationExample`, `ReferenceOutput`, `Prediction`, `HumanCorrection`, `EvaluationRun`, `EvaluationResult`, `ReleaseApproval`

### Main workflow

1. Workflow owner defines a task and acceptance criteria.
2. Domain experts create or approve reference examples.
3. A workflow version processes evaluation examples.
4. Reviewers compare outputs with references and record scores or corrections.
5. Team compares versions and reviews regressions.
6. Authorized owner approves a version for use.
7. Production feedback is captured for future evaluation.

### MVP acceptance criteria

- Workflow versions and their configurations are identifiable.
- Evaluation examples can be grouped into datasets.
- Results can be scored and compared across versions.
- Corrections are linked to the original prediction and reviewer.
- A release requires an explicit approval record.
- Dashboard summarizes evaluation coverage and quality metrics.

### Later enhancements

Automated model-as-judge scoring, statistical evaluation, CI integration, automated regression gates, fine-tuning pipelines, and production monitoring integrations. Do not automatically train or deploy models in the MVP.

---

## Suggested build order

1. **AI-Assisted Case Review Workbench** — closest to existing domain expertise and a concrete reviewer workflow.
2. **AI Workflow Evaluation Hub** — reusable capability that can improve the quality of AI features across applications.
3. **Private Knowledge and Workflow Portal** — broadly reusable internal application pattern.
4. **Procurement and Supplier Tracker** — clear structured workflow and dashboard use case.
5. **Benefits Application Organizer** — valuable but requires careful jurisdiction-specific configuration.
6. **Permit and Inspection Tracker** — feasible as a tracker first; advanced regulatory automation can follow.

## Builder checklist for every project

- [ ] Inspect repository and confirm native generator support.
- [ ] Define MVP scope and explicitly defer unsupported features.
- [ ] Create schema and relationships.
- [ ] Define roles and record-level access rules.
- [ ] Generate navigation, forms, tables, and dashboards.
- [ ] Implement the end-to-end workflow and status transitions.
- [ ] Add sample data and role-specific test accounts.
- [ ] Test permissions, validation, and workflow transitions.
- [ ] Document custom code, integrations, deployment steps, and known limitations.
