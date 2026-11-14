CREATE TABLE staff_members (
  staff_member_id INT AUTO_INCREMENT PRIMARY KEY,
  full_name VARCHAR(120) NOT NULL,
  login_username VARCHAR(255) NOT NULL UNIQUE,
  email VARCHAR(255) NOT NULL,
  staff_level VARCHAR(30) NOT NULL DEFAULT 'Staff',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE clients (
  client_id INT AUTO_INCREMENT PRIMARY KEY,
  client_name VARCHAR(200) NOT NULL,
  industry VARCHAR(120),
  sec_cik VARCHAR(20),
  fiscal_year_end DATE,
  risk_rating VARCHAR(20) NOT NULL DEFAULT 'Medium',
  client_status VARCHAR(20) NOT NULL DEFAULT 'Prospect',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE client_contacts (
  client_contact_id INT AUTO_INCREMENT PRIMARY KEY,
  client_id INT NOT NULL,
  full_name VARCHAR(120) NOT NULL,
  email VARCHAR(255) NOT NULL,
  login_username VARCHAR(255) UNIQUE,
  FOREIGN KEY (client_id) REFERENCES clients(client_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE engagements (
  engagement_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_number VARCHAR(30) DEFAULT NULL UNIQUE,
  client_id INT NOT NULL,
  engagement_type VARCHAR(30) NOT NULL,
  period_end DATE NOT NULL,
  due_date DATE NOT NULL,
  phase VARCHAR(30) NOT NULL DEFAULT 'Prepare',
  partner_id INT NOT NULL,
  manager_id INT NOT NULL,
  staff_lead_id INT,
  revenue_benchmark DECIMAL(16,2),
  materiality_amount DECIMAL(16,2),
  performance_materiality DECIMAL(16,2),
  signed_off_at DATETIME,
  FOREIGN KEY (client_id) REFERENCES clients(client_id),
  FOREIGN KEY (partner_id) REFERENCES staff_members(staff_member_id),
  FOREIGN KEY (manager_id) REFERENCES staff_members(staff_member_id),
  FOREIGN KEY (staff_lead_id) REFERENCES staff_members(staff_member_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE engagement_phase_history (
  engagement_phase_history_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  old_phase VARCHAR(30) DEFAULT NULL,
  new_phase VARCHAR(30) NOT NULL,
  changed_by_type VARCHAR(80),
  FOREIGN KEY (engagement_id) REFERENCES engagements(engagement_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE document_requests (
  document_request_id INT AUTO_INCREMENT PRIMARY KEY,
  request_number VARCHAR(30) DEFAULT NULL UNIQUE,
  engagement_id INT NOT NULL,
  client_contact_id INT,
  item_requested VARCHAR(255) NOT NULL,
  instructions TEXT,
  due_date DATE NOT NULL,
  request_status VARCHAR(20) NOT NULL DEFAULT 'Draft',
  return_comment TEXT,
  FOREIGN KEY (engagement_id) REFERENCES engagements(engagement_id),
  FOREIGN KEY (client_contact_id) REFERENCES client_contacts(client_contact_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE workpapers (
  workpaper_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  audit_area VARCHAR(30) NOT NULL,
  title VARCHAR(200) NOT NULL,
  preparer_id INT NOT NULL,
  reviewer_id INT,
  workpaper_status VARCHAR(30) NOT NULL DEFAULT 'Draft',
  conclusion TEXT,
  review_notes TEXT,
  FOREIGN KEY (engagement_id) REFERENCES engagements(engagement_id),
  FOREIGN KEY (preparer_id) REFERENCES staff_members(staff_member_id),
  FOREIGN KEY (reviewer_id) REFERENCES staff_members(staff_member_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE risks (
  risk_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  audit_area VARCHAR(30) NOT NULL,
  risk_description TEXT NOT NULL,
  likelihood VARCHAR(10) NOT NULL DEFAULT 'Medium',
  impact VARCHAR(10) NOT NULL DEFAULT 'Medium',
  planned_response TEXT,
  FOREIGN KEY (engagement_id) REFERENCES engagements(engagement_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE findings (
  finding_id INT AUTO_INCREMENT PRIMARY KEY,
  finding_number VARCHAR(30) DEFAULT NULL UNIQUE,
  engagement_id INT NOT NULL,
  audit_area VARCHAR(30) NOT NULL,
  finding_description TEXT NOT NULL,
  amount DECIMAL(16,2),
  severity VARCHAR(20) NOT NULL DEFAULT 'Minor',
  finding_status VARCHAR(20) NOT NULL DEFAULT 'Draft',
  source_request_id INT,
  management_response TEXT,
  FOREIGN KEY (engagement_id) REFERENCES engagements(engagement_id),
  FOREIGN KEY (source_request_id) REFERENCES document_requests(document_request_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE sign_offs (
  sign_off_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  phase VARCHAR(30) NOT NULL,
  signed_by_id INT NOT NULL,
  sign_off_comment TEXT,
  FOREIGN KEY (engagement_id) REFERENCES engagements(engagement_id),
  FOREIGN KEY (signed_by_id) REFERENCES staff_members(staff_member_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
CREATE TABLE audit_logs (
  audit_log_id INT AUTO_INCREMENT PRIMARY KEY,
  actor_type VARCHAR(50) NOT NULL DEFAULT 'human',
  actor_ref VARCHAR(100),
  action_code VARCHAR(100) NOT NULL,
  entity_type VARCHAR(100),
  entity_ref VARCHAR(100),
  correlation_ref VARCHAR(100),
  ip_address VARCHAR(50),
  details TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);
INSERT INTO staff_members (staff_member_id, full_name, login_username, email, staff_level, createdBy, modifiedBy) VALUES
(1, 'Priya Shah', 'priya.shah', 'priya.shah@example.com', 'Partner', 'sample', 'sample'),
(2, 'Marcus Lee', 'marcus.lee', 'marcus.lee@example.com', 'Manager', 'sample', 'sample'),
(3, 'Ana Torres', 'ana.torres', 'ana.torres@example.com', 'Staff', 'sample', 'sample'),
(4, 'Ben Okafor', 'ben.okafor', 'ben.okafor@example.com', 'Staff', 'sample', 'sample');
INSERT INTO clients (client_id, client_name, industry, fiscal_year_end, risk_rating, client_status, createdBy, modifiedBy) VALUES
(1, 'Northwind Outfitters Ltd.', 'Retail', '2026-12-31', 'Medium', 'Active', 'sample', 'sample'),
(2, 'Bluebird Health Clinics', 'Healthcare', '2026-09-30', 'High', 'Active', 'sample', 'sample'),
(3, 'Cedar Ridge Builders', 'Construction', '2026-12-31', 'High', 'Active', 'sample', 'sample'),
(4, 'Lumen Software Inc.', 'Technology', '2026-06-30', 'Low', 'Active', 'sample', 'sample');
INSERT INTO client_contacts (client_contact_id, client_id, full_name, email, login_username, createdBy, modifiedBy) VALUES
(1, 1, 'Dana Brooks', 'dana.brooks@example.com', 'dana.brooks', 'sample', 'sample'),
(2, 2, 'Omar Haddad', 'omar.haddad@example.com', 'omar.haddad', 'sample', 'sample');
INSERT INTO engagements (engagement_id, engagement_number, client_id, engagement_type, period_end, due_date, phase, partner_id, manager_id, staff_lead_id, revenue_benchmark, materiality_amount, performance_materiality, createdBy, modifiedBy) VALUES
(1, 'ENG-2026-00001', 1, 'Audit', '2026-12-31', '2027-03-31', 'Fieldwork', 1, 2, 3, 48000000, 240000, 180000, 'sample', 'sample'),
(2, 'ENG-2026-00002', 2, 'Review', '2026-09-30', '2026-12-15', 'Plan', 1, 2, 4, 21000000, 210000, 157500, 'sample', 'sample'),
(3, 'ENG-2026-00003', 3, 'Audit', '2026-12-31', '2027-04-15', 'Prepare', 1, 2, 3, NULL, NULL, NULL, 'sample', 'sample'),
(4, 'ENG-2026-00004', 4, 'Compilation', '2026-06-30', '2026-09-30', 'Report', 1, 2, 4, 9500000, 95000, 71250, 'sample', 'sample');
INSERT INTO document_requests (request_number, engagement_id, client_contact_id, item_requested, instructions, due_date, request_status, createdBy, modifiedBy) VALUES
('DRQ-2026-00001', 1, 1, 'December bank statements, all operating accounts', 'PDF statements for every account open at year end.', '2027-01-15', 'Requested', 'sample', 'sample'),
('DRQ-2026-00002', 1, 1, 'Accounts receivable aging at December 31', 'Export from your accounting system, including customer names.', '2027-01-20', 'Requested', 'sample', 'sample'),
('DRQ-2026-00003', 2, 2, 'Payroll register for September', NULL, '2026-10-20', 'Uploaded', 'sample', 'sample');
INSERT INTO workpapers (engagement_id, audit_area, title, preparer_id, reviewer_id, workpaper_status, createdBy, modifiedBy) VALUES
(1, 'Cash', 'Bank reconciliation testing', 3, 2, 'Draft', 'sample', 'sample'),
(1, 'Revenue', 'Revenue cut-off testing', 3, 2, 'Ready for review', 'sample', 'sample');
INSERT INTO risks (engagement_id, audit_area, risk_description, likelihood, impact, planned_response, createdBy, modifiedBy) VALUES
(1, 'Revenue', 'Revenue recognized before goods ship at year end.', 'Medium', 'High', 'Cut-off testing of shipments 10 days either side of year end.', 'sample', 'sample'),
(3, 'Other', 'Percentage-of-completion estimates on long contracts.', 'High', 'High', 'Recalculate completion on the five largest contracts.', 'sample', 'sample');
INSERT INTO findings (finding_number, engagement_id, audit_area, finding_description, amount, severity, finding_status, createdBy, modifiedBy) VALUES
('FND-2026-00001', 4, 'Payables', 'Two supplier invoices recorded in the wrong period.', 12400, 'Minor', 'Approved', 'sample', 'sample');
