-- Clinical research site operations, modelled on https://www.useintake.ai/
-- Studies, regulatory documents, schedule of assessments -> source documents, connectors,
-- cross-document Q&A with citations, workflow runs and ClinicalTrials.gov opportunities.

CREATE TABLE sponsors (
  sponsor_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(150) NOT NULL,
  sponsor_type VARCHAR(50) NOT NULL DEFAULT 'pharma',
  contact_email VARCHAR(150),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE studies (
  study_id INT AUTO_INCREMENT PRIMARY KEY,
  sponsor_id INT NOT NULL,
  protocol_number VARCHAR(50) NOT NULL,
  title VARCHAR(255) NOT NULL,
  nct_number VARCHAR(20),
  phase VARCHAR(20),
  therapeutic_area VARCHAR(100),
  status VARCHAR(30) NOT NULL DEFAULT 'startup',
  start_date DATE,
  target_enrollment INT,
  current_enrollment INT NOT NULL DEFAULT 0,
  budget_amount DECIMAL(12,2),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (sponsor_id) REFERENCES sponsors(sponsor_id)
);

CREATE TABLE site_staff (
  site_staff_id INT AUTO_INCREMENT PRIMARY KEY,
  full_name VARCHAR(150) NOT NULL,
  staff_role VARCHAR(50) NOT NULL,
  email VARCHAR(150),
  fte DECIMAL(4,2) NOT NULL DEFAULT 1.00,
  gcp_training_expiry DATE,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE study_assignments (
  study_assignment_id INT AUTO_INCREMENT PRIMARY KEY,
  study_id INT NOT NULL,
  site_staff_id INT NOT NULL,
  study_role VARCHAR(50) NOT NULL,
  is_backup_trained TINYINT(1) NOT NULL DEFAULT '0',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (study_id) REFERENCES studies(study_id),
  FOREIGN KEY (site_staff_id) REFERENCES site_staff(site_staff_id)
);

CREATE TABLE connectors (
  connector_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  connector_type VARCHAR(50) NOT NULL,
  access_scope VARCHAR(255),
  status VARCHAR(30) NOT NULL DEFAULT 'connected',
  last_synced_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE documents (
  document_id INT AUTO_INCREMENT PRIMARY KEY,
  study_id INT,
  connector_id INT,
  document_type VARCHAR(50) NOT NULL,
  title VARCHAR(255) NOT NULL,
  version VARCHAR(20),
  file_url VARCHAR(500),
  effective_date DATE,
  status VARCHAR(30) NOT NULL DEFAULT 'current',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (study_id) REFERENCES studies(study_id),
  FOREIGN KEY (connector_id) REFERENCES connectors(connector_id)
);

CREATE TABLE visits (
  visit_id INT AUTO_INCREMENT PRIMARY KEY,
  study_id INT NOT NULL,
  visit_name VARCHAR(100) NOT NULL,
  visit_sequence INT NOT NULL,
  day_offset INT NOT NULL DEFAULT 0,
  window_days INT NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (study_id) REFERENCES studies(study_id)
);

CREATE TABLE assessments (
  assessment_id INT AUTO_INCREMENT PRIMARY KEY,
  study_id INT NOT NULL,
  name VARCHAR(150) NOT NULL,
  category VARCHAR(50),
  instructions TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (study_id) REFERENCES studies(study_id)
);

CREATE TABLE visit_assessments (
  visit_assessment_id INT AUTO_INCREMENT PRIMARY KEY,
  visit_id INT NOT NULL,
  assessment_id INT NOT NULL,
  is_required TINYINT(1) NOT NULL DEFAULT '1',
  notes VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
  FOREIGN KEY (assessment_id) REFERENCES assessments(assessment_id)
);

CREATE TABLE source_documents (
  source_document_id INT AUTO_INCREMENT PRIMARY KEY,
  visit_id INT NOT NULL,
  document_id INT,
  title VARCHAR(255) NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  generated_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (visit_id) REFERENCES visits(visit_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id)
);

CREATE TABLE chat_threads (
  chat_thread_id INT AUTO_INCREMENT PRIMARY KEY,
  study_id INT,
  title VARCHAR(255) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (study_id) REFERENCES studies(study_id)
);

CREATE TABLE chat_messages (
  chat_message_id INT AUTO_INCREMENT PRIMARY KEY,
  chat_thread_id INT NOT NULL,
  sender VARCHAR(20) NOT NULL DEFAULT 'user',
  content TEXT NOT NULL,
  found_in_sources TINYINT(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (chat_thread_id) REFERENCES chat_threads(chat_thread_id)
);

CREATE TABLE message_citations (
  message_citation_id INT AUTO_INCREMENT PRIMARY KEY,
  chat_message_id INT NOT NULL,
  document_id INT NOT NULL,
  page_reference VARCHAR(50),
  excerpt TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (chat_message_id) REFERENCES chat_messages(chat_message_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id)
);

CREATE TABLE automation_runs (
  automation_run_id INT AUTO_INCREMENT PRIMARY KEY,
  study_id INT NOT NULL,
  document_id INT,
  automation_type VARCHAR(50) NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'queued',
  started_at DATETIME,
  completed_at DATETIME,
  summary TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (study_id) REFERENCES studies(study_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id)
);

CREATE TABLE trial_opportunities (
  trial_opportunity_id INT AUTO_INCREMENT PRIMARY KEY,
  sponsor_id INT,
  nct_number VARCHAR(20),
  title VARCHAR(255) NOT NULL,
  phase VARCHAR(20),
  condition_name VARCHAR(150),
  found_via VARCHAR(50) NOT NULL DEFAULT 'clinicaltrials.gov',
  fit_score INT,
  status VARCHAR(30) NOT NULL DEFAULT 'new',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (sponsor_id) REFERENCES sponsors(sponsor_id)
);
