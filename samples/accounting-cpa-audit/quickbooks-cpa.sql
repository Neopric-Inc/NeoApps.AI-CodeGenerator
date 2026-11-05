-- ============================================================================
-- QUICKBOOKS CLONE - CPA DOCUMENT INTELLIGENCE PLATFORM
-- Complete Database Schema (Optimized for NeoApps Generator)
-- MySQL 8.0+ with lower_case_table_names=1
-- ============================================================================

-- ============================================================================
-- CORE: TENANCY & USERS (Built-in tables managed by generator)
-- ============================================================================
-- The generator creates these automatically: roles, users, entities, permissionmatrix,
-- user_tenants, login_attempts, user_security, versioninfo, messagequeue,
-- s3bucket, s3bucket_folders, background_jobs, background_job_events,
-- ai_files, ai_chunks, ai_conversations, ai_messages, ai_runs,
-- dnd_ui_versions, project_dnd_ui_versions, workflow, workflows, workflows_projects,
-- workflow_builds, workflow_runs, workflow_deployments, workflow_triggers,
-- workflow_trigger_conditions, number_sequences
-- We DO NOT declare them here - just reference via FK

-- ============================================================================
-- AUDIT COLUMN MACRO (use in every table)
-- ============================================================================
-- createdBy VARCHAR(255) NOT NULL,
-- modifiedBy VARCHAR(255) NOT NULL,
-- createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
-- modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
-- isActive TINYINT(1) NOT NULL DEFAULT '1'

-- ============================================================================
-- 1. LOOKUP / REFERENCE TABLES (must be first for FK references)
-- ============================================================================

CREATE TABLE payment_terms (
  payment_terms_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  days_due INT NOT NULL DEFAULT 30,
  discount_percent DECIMAL(5,2) DEFAULT 0,
  discount_days INT DEFAULT 0,
  description TEXT,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE payment_methods (
  payment_method_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  type VARCHAR(30) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  is_online TINYINT(1) NOT NULL DEFAULT 0,
  config_json JSON,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE class_codes (
  class_code_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  description TEXT,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  parent_class_code_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (parent_class_code_id) REFERENCES `class_codes`(class_code_id)
);

CREATE TABLE geo_locations (
  geo_location_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  description TEXT,
  address JSON,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  parent_geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (parent_geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE currencies (
  currency_id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(3) NOT NULL,
  name VARCHAR(50) NOT NULL,
  symbol VARCHAR(10),
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  is_base TINYINT(1) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  UNIQUE KEY uk_currency (code)
);

CREATE TABLE exchange_rates (
  exchange_rate_id INT AUTO_INCREMENT PRIMARY KEY,
  from_currency_id INT NOT NULL,
  to_currency_id INT NOT NULL,
  rate DECIMAL(18,6) NOT NULL,
  effective_date DATE NOT NULL,
  source VARCHAR(50),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (from_currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (to_currency_id) REFERENCES currencies(currency_id),
  UNIQUE KEY uk_rate (from_currency_id, to_currency_id, effective_date)
);

CREATE TABLE tax_codes (
  tax_code_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  description TEXT,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE tax_rates (
  tax_rate_id INT AUTO_INCREMENT PRIMARY KEY,
  tax_code_id INT NOT NULL,
  agency_name VARCHAR(100) NOT NULL,
  rate_percent DECIMAL(8,4) NOT NULL,
  effective_from DATE NOT NULL,
  effective_to DATE,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id)
);

-- ============================================================================
-- 2. INTAKE & DOCUMENT MANAGEMENT (10 tables)
-- ============================================================================

CREATE TABLE intake_channels (
  intake_channel_id INT AUTO_INCREMENT PRIMARY KEY,
  channel_type VARCHAR(30) NOT NULL,
  channel_name VARCHAR(100) NOT NULL,
  config_json JSON NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  last_polled_at DATETIME,
  last_error TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE source_documents (
  source_document_id INT AUTO_INCREMENT PRIMARY KEY,
  intake_channel_id INT,
  file_name VARCHAR(500) NOT NULL,
  file_path VARCHAR(1000) NOT NULL,
  file_size BIGINT NOT NULL,
  mime_type VARCHAR(100) NOT NULL,
  file_hash VARCHAR(64) NOT NULL,
  source_type VARCHAR(30) NOT NULL,
  source_reference VARCHAR(500),
  status VARCHAR(30) NOT NULL DEFAULT 'uploaded',
  processing_started_at DATETIME,
  processing_completed_at DATETIME,
  error_message TEXT,
  ocr_provider VARCHAR(30),
  raw_ocr_text LONGTEXT,
  page_count INT,
  language VARCHAR(10) DEFAULT 'en',
  tags JSON,
  uploaded_by VARCHAR(255) NOT NULL,
  uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (intake_channel_id) REFERENCES intake_channels(intake_channel_id),
  UNIQUE KEY uk_file_hash (file_hash)
);

CREATE TABLE document_versions (
  version_id INT AUTO_INCREMENT PRIMARY KEY,
  source_document_id INT NOT NULL,
  version_number INT NOT NULL,
  file_path VARCHAR(1000) NOT NULL,
  file_size BIGINT NOT NULL,
  file_hash VARCHAR(64) NOT NULL,
  change_reason VARCHAR(500),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (source_document_id) REFERENCES `source_documents`(source_document_id),
  UNIQUE KEY uk_document_version (source_document_id, version_number)
);

CREATE TABLE document_classifications (
  classification_id INT AUTO_INCREMENT PRIMARY KEY,
  source_document_id INT NOT NULL,
  document_type VARCHAR(50) NOT NULL,
  confidence DECIMAL(5,4) NOT NULL,
  sub_type VARCHAR(50),
  classifier_version VARCHAR(50),
  classified_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  is_manual_override TINYINT(1) NOT NULL DEFAULT 0,
  overridden_by VARCHAR(255),
  overridden_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (source_document_id) REFERENCES `source_documents`(source_document_id)
);

CREATE TABLE extraction_templates (
  template_id INT AUTO_INCREMENT PRIMARY KEY,
  template_name VARCHAR(100) NOT NULL,
  document_type VARCHAR(50) NOT NULL,
  sub_type VARCHAR(50),
  schema_json JSON NOT NULL,
  validation_rules_json JSON,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  is_system TINYINT(1) NOT NULL DEFAULT 0,
  version INT NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  UNIQUE KEY uk_template_type (document_type, sub_type, version)
);

CREATE TABLE extracted_fields (
  extracted_field_id INT AUTO_INCREMENT PRIMARY KEY,
  source_document_id INT NOT NULL,
  template_id INT,
  field_name VARCHAR(100) NOT NULL,
  field_value TEXT,
  field_type VARCHAR(30),
  confidence DECIMAL(5,4),
  bounding_box JSON,
  validation_status VARCHAR(20) DEFAULT 'pending',
  validation_message TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (source_document_id) REFERENCES `source_documents`(source_document_id),
  FOREIGN KEY (template_id) REFERENCES extraction_templates(template_id)
);

CREATE TABLE document_parsing_jobs (
  job_id INT AUTO_INCREMENT PRIMARY KEY,
  source_document_id INT NOT NULL,
  job_type VARCHAR(30) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'queued',
  provider VARCHAR(30),
  input_params JSON,
  output_result JSON,
  pages_processed INT DEFAULT 0,
  fields_extracted INT DEFAULT 0,
  started_at DATETIME,
  completed_at DATETIME,
  error_details TEXT,
  retry_count INT DEFAULT 0,
  max_retries INT DEFAULT 3,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (source_document_id) REFERENCES `source_documents`(source_document_id)
);

-- ============================================================================
-- 3. WORKFLOW ENGINE (8 tables)
-- ============================================================================

CREATE TABLE workflow_definitions (
  workflow_definition_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  description TEXT,
  category VARCHAR(50),
  definition_json JSON NOT NULL,
  version INT NOT NULL DEFAULT 1,
  is_template TINYINT(1) NOT NULL DEFAULT 1,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE workflow_versions (
  version_id INT AUTO_INCREMENT PRIMARY KEY,
  workflow_definition_id INT NOT NULL,
  version_number INT NOT NULL,
  definition_json JSON NOT NULL,
  change_log TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (workflow_definition_id) REFERENCES workflow_definitions(workflow_definition_id),
  UNIQUE KEY uk_workflow_version (workflow_definition_id, version_number)
);

CREATE TABLE workflow_instances (
  instance_id INT AUTO_INCREMENT PRIMARY KEY,
  workflow_definition_id INT NOT NULL,
  version_id INT,
  name VARCHAR(200),
  current_state VARCHAR(100) NOT NULL,
  variables_json JSON,
  status VARCHAR(30) NOT NULL DEFAULT 'running',
  started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  completed_at DATETIME,
  error_message TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (workflow_definition_id) REFERENCES workflow_definitions(workflow_definition_id),
  FOREIGN KEY (version_id) REFERENCES workflow_versions(version_id)
);

CREATE TABLE workflow_tasks (
  task_id INT AUTO_INCREMENT PRIMARY KEY,
  instance_id INT NOT NULL,
  task_definition_key VARCHAR(100) NOT NULL,
  task_name VARCHAR(200) NOT NULL,
  task_type VARCHAR(30) NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'pending',
  priority INT DEFAULT 0,
  due_at DATETIME,
  started_at DATETIME,
  completed_at DATETIME,
  assigned_to_user_id INT,
  assigned_to_role VARCHAR(50),
  input_variables_json JSON,
  output_variables_json JSON,
  form_schema_json JSON,
  form_data_json JSON,
  error_message TEXT,
  retry_count INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (instance_id) REFERENCES workflow_instances(instance_id)
);

CREATE TABLE task_assignments (
  assignment_id INT AUTO_INCREMENT PRIMARY KEY,
  task_id INT NOT NULL,
  user_id INT NOT NULL,
  role VARCHAR(50),
  assigned_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  assigned_by VARCHAR(255),
  acknowledged_at DATETIME,
  acknowledged_by VARCHAR(255),
  is_primary TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (task_id) REFERENCES workflow_tasks(task_id)
);

CREATE TABLE task_comments (
  comment_id INT AUTO_INCREMENT PRIMARY KEY,
  task_id INT NOT NULL,
  user_id INT NOT NULL,
  comment_text TEXT NOT NULL,
  is_system TINYINT(1) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (task_id) REFERENCES workflow_tasks(task_id)
);

CREATE TABLE workflow_transitions (
  transition_id INT AUTO_INCREMENT PRIMARY KEY,
  instance_id INT NOT NULL,
  from_state VARCHAR(100),
  to_state VARCHAR(100) NOT NULL,
  trigger_type VARCHAR(30),
  trigger_reference VARCHAR(200),
  variables_snapshot JSON,
  executed_by VARCHAR(255),
  executed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (instance_id) REFERENCES workflow_instances(instance_id)
);

-- ============================================================================
-- 4. CLIENT PORTAL & COLLABORATION (6 tables)
-- ============================================================================

CREATE TABLE client_portals (
  portal_id INT AUTO_INCREMENT PRIMARY KEY,
  subdomain VARCHAR(100) UNIQUE,
  custom_domain VARCHAR(200),
  branding_json JSON,
  settings_json JSON,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE portal_users (
  portal_user_id INT AUTO_INCREMENT PRIMARY KEY,
  portal_id INT NOT NULL,
  email VARCHAR(255) NOT NULL,
  first_name VARCHAR(100),
  last_name VARCHAR(100),
  role VARCHAR(50) NOT NULL DEFAULT 'viewer',
  status VARCHAR(20) NOT NULL DEFAULT 'invited',
  last_login_at DATETIME,
  invited_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  invited_by VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (portal_id) REFERENCES client_portals(portal_id),
  UNIQUE KEY uk_portal_email (portal_id, email)
);

CREATE TABLE document_requests (
  request_id INT AUTO_INCREMENT PRIMARY KEY,
  request_name VARCHAR(200) NOT NULL,
  description TEXT,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  due_date DATE,
  sent_at DATETIME,
  completed_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE request_items (
  product_service_id INT AUTO_INCREMENT PRIMARY KEY,
  request_id INT NOT NULL,
  item_name VARCHAR(200) NOT NULL,
  description TEXT,
  document_type VARCHAR(50),
  is_required TINYINT(1) NOT NULL DEFAULT 1,
  status VARCHAR(30) NOT NULL DEFAULT 'pending',
  uploaded_document_id INT,
  reviewed_by VARCHAR(255),
  reviewed_at DATETIME,
  review_notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (request_id) REFERENCES document_requests(request_id),
  FOREIGN KEY (uploaded_document_id) REFERENCES `source_documents`(source_document_id)
);

CREATE TABLE client_notifications (
  notification_id INT AUTO_INCREMENT PRIMARY KEY,
  portal_id INT NOT NULL,
  portal_user_id INT,
  type VARCHAR(50) NOT NULL,
  title VARCHAR(200) NOT NULL,
  message TEXT,
  reference_type VARCHAR(50),
  reference_id INT,
  is_read TINYINT(1) NOT NULL DEFAULT 0,
  read_at DATETIME,
  sent_via_email TINYINT(1) NOT NULL DEFAULT 0,
  email_sent_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (portal_id) REFERENCES client_portals(portal_id),
  FOREIGN KEY (portal_user_id) REFERENCES portal_users(portal_user_id)
);

-- ============================================================================
-- 5. CORE ACCOUNTING (15 tables) - QuickBooks Parity
-- ============================================================================







CREATE TABLE gl_accounts (
  gl_account_id INT AUTO_INCREMENT PRIMARY KEY,
  account_number VARCHAR(20),
  code VARCHAR(20) NOT NULL,
  name VARCHAR(200) NOT NULL,
  account_type VARCHAR(30) NOT NULL,
  detail_type VARCHAR(50),
  parent_gl_account_id INT,
  description TEXT,
  opening_balance DECIMAL(18,2) DEFAULT 0,
  opening_balance_date DATE,
  current_balance DECIMAL(18,2) DEFAULT 0,
  currency_id INT,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  is_sub_account TINYINT(1) NOT NULL DEFAULT 0,
  account_level INT DEFAULT 0,
  sort_order INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (parent_gl_account_id) REFERENCES `gl_accounts`(gl_account_id)
);

CREATE TABLE gl_entries (
  gl_entry_id INT AUTO_INCREMENT PRIMARY KEY,
  entry_number VARCHAR(50) NOT NULL,
  entry_date DATE NOT NULL,
  memo TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'draft',
  is_adjusting TINYINT(1) NOT NULL DEFAULT 0,
  is_reversing TINYINT(1) NOT NULL DEFAULT 0,
  reversing_gl_entry_id INT,
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  total_debit DECIMAL(18,2) DEFAULT 0,
  total_credit DECIMAL(18,2) DEFAULT 0,
  reference_type VARCHAR(50),
  reference_id INT,
  posted_at DATETIME,
  posted_by VARCHAR(255),
  voided_at DATETIME,
  voided_by VARCHAR(255),
  void_reason TEXT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (reversing_gl_entry_id) REFERENCES `gl_entries`(gl_entry_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);


CREATE TABLE customers (
  customer_id INT AUTO_INCREMENT PRIMARY KEY,
  display_name VARCHAR(200) NOT NULL,
  company_name VARCHAR(200),
  first_name VARCHAR(100),
  last_name VARCHAR(100),
  email VARCHAR(255),
  phone VARCHAR(50),
  mobile VARCHAR(50),
  fax VARCHAR(50),
  website VARCHAR(255),
  billing_address JSON,
  shipping_address JSON,
  payment_terms_id INT,
  tax_exempt TINYINT(1) NOT NULL DEFAULT 0,
  tax_id VARCHAR(50),
  resale_number VARCHAR(50),
  currency_id INT,
  preferred_delivery_method VARCHAR(30),
  notes TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'active',
  price_level_id INT,
  sales_rep_id INT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (payment_terms_id) REFERENCES payment_terms(payment_terms_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE client_engagements (
  client_engagement_id INT AUTO_INCREMENT PRIMARY KEY,
  customer_id INT NOT NULL,
  name VARCHAR(200) NOT NULL,
  description TEXT,
  start_date DATE,
  end_date DATE,
  status VARCHAR(30) NOT NULL DEFAULT 'planning',
  budget_amount DECIMAL(18,2) DEFAULT 0,
  actual_cost DECIMAL(18,2) DEFAULT 0,
  billing_type VARCHAR(30) NOT NULL DEFAULT 'time_material',
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  project_manager_id INT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE journal_lines (
  journal_line_id INT AUTO_INCREMENT PRIMARY KEY,
  gl_entry_id INT NOT NULL,
  gl_account_id INT NOT NULL,
  description TEXT,
  debit_amount DECIMAL(18,2) DEFAULT 0,
  credit_amount DECIMAL(18,2) DEFAULT 0,
  entity_type VARCHAR(30),
  entity_id INT,
  class_code_id INT,
  geo_location_id INT,
  client_engagement_id INT,
  sort_order INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (gl_entry_id) REFERENCES `gl_entries`(gl_entry_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id),
  FOREIGN KEY (client_engagement_id) REFERENCES `client_engagements`(client_engagement_id)
);


CREATE TABLE customer_contacts (
  contact_id INT AUTO_INCREMENT PRIMARY KEY,
  customer_id INT NOT NULL,
  first_name VARCHAR(100),
  last_name VARCHAR(100),
  email VARCHAR(255),
  phone VARCHAR(50),
  mobile VARCHAR(50),
  fax VARCHAR(50),
  address JSON,
  is_primary TINYINT(1) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE vendors (
  vendor_id INT AUTO_INCREMENT PRIMARY KEY,
  display_name VARCHAR(200) NOT NULL,
  company_name VARCHAR(200),
  first_name VARCHAR(100),
  last_name VARCHAR(100),
  email VARCHAR(255),
  phone VARCHAR(50),
  mobile VARCHAR(50),
  fax VARCHAR(50),
  website VARCHAR(255),
  address JSON,
  payment_terms_id INT,
  tax_id VARCHAR(50),
  account_number VARCHAR(50),
  currency_id INT,
  default_account_id INT,
  default_payment_method_id INT,
  is_1099_eligible TINYINT(1) NOT NULL DEFAULT 0,
  notes TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'active',
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (payment_terms_id) REFERENCES payment_terms(payment_terms_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (default_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (default_payment_method_id) REFERENCES payment_methods(payment_method_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE vendor_contacts (
  contact_id INT AUTO_INCREMENT PRIMARY KEY,
  vendor_id INT NOT NULL,
  first_name VARCHAR(100),
  last_name VARCHAR(100),
  email VARCHAR(255),
  phone VARCHAR(50),
  mobile VARCHAR(50),
  fax VARCHAR(50),
  address JSON,
  is_primary TINYINT(1) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vendor_id) REFERENCES vendors(vendor_id)
);


CREATE TABLE products_services (
  product_service_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  item_type VARCHAR(30) NOT NULL,
  sku VARCHAR(100),
  upc VARCHAR(50),
  description TEXT,
  sales_description TEXT,
  purchase_description TEXT,
  unit_price DECIMAL(18,2) DEFAULT 0,
  cost DECIMAL(18,2) DEFAULT 0,
  sales_tax_code_id INT,
  purchase_tax_code_id INT,
  income_account_id INT,
  expense_account_id INT,
  asset_account_id INT,
  cogs_account_id INT,
  reorder_point INT DEFAULT 0,
  max_stock_level INT,
  preferred_vendor_id INT,
  weight DECIMAL(10,3),
  length DECIMAL(10,3),
  width DECIMAL(10,3),
  height DECIMAL(10,3),
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  track_qty_on_hand TINYINT(1) NOT NULL DEFAULT 0,
  qty_on_hand DECIMAL(18,4) DEFAULT 0,
  qty_on_sales_order DECIMAL(18,4) DEFAULT 0,
  qty_on_purchase_order DECIMAL(18,4) DEFAULT 0,
  is_billable TINYINT(1) NOT NULL DEFAULT 0,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (sales_tax_code_id) REFERENCES tax_codes(tax_code_id),
  FOREIGN KEY (purchase_tax_code_id) REFERENCES tax_codes(tax_code_id),
  FOREIGN KEY (income_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (expense_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (asset_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (cogs_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (preferred_vendor_id) REFERENCES vendors(vendor_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE item_bundles (
  bundle_id INT AUTO_INCREMENT PRIMARY KEY,
  parent_item_id INT NOT NULL,
  component_item_id INT NOT NULL,
  quantity DECIMAL(18,4) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (parent_item_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (component_item_id) REFERENCES `products_services`(product_service_id),
  UNIQUE KEY uk_bundle_component (parent_item_id, component_item_id)
);

-- ============================================================================
-- 6. SALES TRANSACTIONS (6 tables)
-- ============================================================================

CREATE TABLE invoices (
  invoice_id INT AUTO_INCREMENT PRIMARY KEY,
  customer_id INT NOT NULL,
  invoice_number VARCHAR(50) NOT NULL,
  invoice_date DATE NOT NULL,
  due_date DATE,
  ship_date DATE,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  payment_terms_id INT,
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  subtotal DECIMAL(18,2) DEFAULT 0,
  discount_amount DECIMAL(18,2) DEFAULT 0,
  discount_type VARCHAR(20),
  tax_amount DECIMAL(18,2) DEFAULT 0,
  shipping_amount DECIMAL(18,2) DEFAULT 0,
  total_amount DECIMAL(18,2) DEFAULT 0,
  balance_due DECIMAL(18,2) DEFAULT 0,
  memo TEXT,
  private_note TEXT,
  sales_rep_id INT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (payment_terms_id) REFERENCES payment_terms(payment_terms_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE invoice_lines (
  invoice_line_id INT AUTO_INCREMENT PRIMARY KEY,
  invoice_id INT NOT NULL,
  product_service_id INT,
  description TEXT,
  quantity DECIMAL(18,4) DEFAULT 1,
  unit_price DECIMAL(18,2) DEFAULT 0,
  rate_percent DECIMAL(5,2),
  amount DECIMAL(18,2) DEFAULT 0,
  discount_amount DECIMAL(18,2) DEFAULT 0,
  discount_type VARCHAR(20),
  tax_code_id INT,
  gl_account_id INT,
  class_code_id INT,
  geo_location_id INT,
  service_date DATE,
  sort_order INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (invoice_id) REFERENCES invoices(invoice_id),
  FOREIGN KEY (product_service_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE estimates (
  estimate_id INT AUTO_INCREMENT PRIMARY KEY,
  customer_id INT NOT NULL,
  estimate_number VARCHAR(50) NOT NULL,
  estimate_date DATE NOT NULL,
  expiration_date DATE,
  status VARCHAR(30) NOT NULL DEFAULT 'pending',
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  subtotal DECIMAL(18,2) DEFAULT 0,
  discount_amount DECIMAL(18,2) DEFAULT 0,
  tax_amount DECIMAL(18,2) DEFAULT 0,
  shipping_amount DECIMAL(18,2) DEFAULT 0,
  total_amount DECIMAL(18,2) DEFAULT 0,
  memo TEXT,
  sales_rep_id INT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE estimate_lines (
  estimate_line_id INT AUTO_INCREMENT PRIMARY KEY,
  estimate_id INT NOT NULL,
  product_service_id INT,
  description TEXT,
  quantity DECIMAL(18,4) DEFAULT 1,
  unit_price DECIMAL(18,2) DEFAULT 0,
  rate_percent DECIMAL(5,2),
  amount DECIMAL(18,2) DEFAULT 0,
  discount_amount DECIMAL(18,2) DEFAULT 0,
  tax_code_id INT,
  gl_account_id INT,
  class_code_id INT,
  geo_location_id INT,
  service_date DATE,
  sort_order INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (estimate_id) REFERENCES estimates(estimate_id),
  FOREIGN KEY (product_service_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE credit_memos (
  credit_memo_id INT AUTO_INCREMENT PRIMARY KEY,
  customer_id INT NOT NULL,
  credit_memo_number VARCHAR(50) NOT NULL,
  credit_memo_date DATE NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'open',
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  subtotal DECIMAL(18,2) DEFAULT 0,
  discount_amount DECIMAL(18,2) DEFAULT 0,
  tax_amount DECIMAL(18,2) DEFAULT 0,
  total_amount DECIMAL(18,2) DEFAULT 0,
  balance_due DECIMAL(18,2) DEFAULT 0,
  memo TEXT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE credit_memo_lines (
  credit_memo_line_id INT AUTO_INCREMENT PRIMARY KEY,
  credit_memo_id INT NOT NULL,
  product_service_id INT,
  description TEXT,
  quantity DECIMAL(18,4) DEFAULT 1,
  unit_price DECIMAL(18,2) DEFAULT 0,
  amount DECIMAL(18,2) DEFAULT 0,
  tax_code_id INT,
  gl_account_id INT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (credit_memo_id) REFERENCES credit_memos(credit_memo_id),
  FOREIGN KEY (product_service_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

-- ============================================================================
-- 7. PURCHASE TRANSACTIONS (5 tables)
-- ============================================================================

CREATE TABLE bills (
  bill_id INT AUTO_INCREMENT PRIMARY KEY,
  vendor_id INT NOT NULL,
  bill_number VARCHAR(50) NOT NULL,
  bill_date DATE NOT NULL,
  due_date DATE,
  status VARCHAR(30) NOT NULL DEFAULT 'open',
  payment_terms_id INT,
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  subtotal DECIMAL(18,2) DEFAULT 0,
  discount_amount DECIMAL(18,2) DEFAULT 0,
  tax_amount DECIMAL(18,2) DEFAULT 0,
  total_amount DECIMAL(18,2) DEFAULT 0,
  balance_due DECIMAL(18,2) DEFAULT 0,
  memo TEXT,
  private_note TEXT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vendor_id) REFERENCES vendors(vendor_id),
  FOREIGN KEY (payment_terms_id) REFERENCES payment_terms(payment_terms_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE bill_lines (
  bill_line_id INT AUTO_INCREMENT PRIMARY KEY,
  bill_id INT NOT NULL,
  product_service_id INT,
  description TEXT,
  quantity DECIMAL(18,4) DEFAULT 1,
  unit_price DECIMAL(18,2) DEFAULT 0,
  rate_percent DECIMAL(5,2),
  amount DECIMAL(18,2) DEFAULT 0,
  discount_amount DECIMAL(18,2) DEFAULT 0,
  tax_code_id INT,
  gl_account_id INT,
  class_code_id INT,
  geo_location_id INT,
  billable TINYINT(1) NOT NULL DEFAULT 0,
  customer_id INT,
  sort_order INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (bill_id) REFERENCES bills(bill_id),
  FOREIGN KEY (product_service_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE purchase_orders (
  purchase_order_id INT AUTO_INCREMENT PRIMARY KEY,
  vendor_id INT NOT NULL,
  po_number VARCHAR(50) NOT NULL,
  po_date DATE NOT NULL,
  expected_date DATE,
  status VARCHAR(30) NOT NULL DEFAULT 'open',
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  subtotal DECIMAL(18,2) DEFAULT 0,
  tax_amount DECIMAL(18,2) DEFAULT 0,
  shipping_amount DECIMAL(18,2) DEFAULT 0,
  total_amount DECIMAL(18,2) DEFAULT 0,
  memo TEXT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vendor_id) REFERENCES vendors(vendor_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE purchase_order_lines (
  po_line_id INT AUTO_INCREMENT PRIMARY KEY,
  purchase_order_id INT NOT NULL,
  product_service_id INT,
  description TEXT,
  quantity DECIMAL(18,4) DEFAULT 1,
  unit_price DECIMAL(18,2) DEFAULT 0,
  amount DECIMAL(18,2) DEFAULT 0,
  tax_code_id INT,
  gl_account_id INT,
  class_code_id INT,
  geo_location_id INT,
  received_qty DECIMAL(18,4) DEFAULT 0,
  sort_order INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(purchase_order_id),
  FOREIGN KEY (product_service_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE vendor_credits (
  vendor_credit_id INT AUTO_INCREMENT PRIMARY KEY,
  vendor_id INT NOT NULL,
  credit_number VARCHAR(50) NOT NULL,
  credit_date DATE NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'open',
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  subtotal DECIMAL(18,2) DEFAULT 0,
  tax_amount DECIMAL(18,2) DEFAULT 0,
  total_amount DECIMAL(18,2) DEFAULT 0,
  balance_due DECIMAL(18,2) DEFAULT 0,
  memo TEXT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vendor_id) REFERENCES vendors(vendor_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

-- ============================================================================
-- 8. PAYMENTS & BANKING (10 tables)
-- ============================================================================

CREATE TABLE payments (
  payment_id INT AUTO_INCREMENT PRIMARY KEY,
  payment_type VARCHAR(20) NOT NULL,
  customer_id INT,
  vendor_id INT,
  payment_method_id INT,
  deposit_to_account_id INT,
  payment_date DATE NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  reference VARCHAR(100),
  memo TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'uncleared',
  processed_date DATE,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (vendor_id) REFERENCES vendors(vendor_id),
  FOREIGN KEY (payment_method_id) REFERENCES payment_methods(payment_method_id),
  FOREIGN KEY (deposit_to_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id)
);

CREATE TABLE payment_applications (
  payment_application_id INT AUTO_INCREMENT PRIMARY KEY,
  payment_id INT NOT NULL,
  invoice_id INT,
  bill_id INT,
  credit_memo_id INT,
  vendor_credit_id INT,
  amount_applied DECIMAL(18,2) NOT NULL,
  discount_taken DECIMAL(18,2) DEFAULT 0,
  write_off_amount DECIMAL(18,2) DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (payment_id) REFERENCES payments(payment_id),
  FOREIGN KEY (invoice_id) REFERENCES invoices(invoice_id),
  FOREIGN KEY (bill_id) REFERENCES bills(bill_id),
  FOREIGN KEY (credit_memo_id) REFERENCES credit_memos(credit_memo_id),
  FOREIGN KEY (vendor_credit_id) REFERENCES vendor_credits(vendor_credit_id)
);

CREATE TABLE deposits (
  deposit_id INT AUTO_INCREMENT PRIMARY KEY,
  deposit_number VARCHAR(50) NOT NULL,
  deposit_date DATE NOT NULL,
  gl_account_id INT NOT NULL,
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  total_amount DECIMAL(18,2) DEFAULT 0,
  memo TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'pending',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id)
);

CREATE TABLE deposit_lines (
  deposit_line_id INT AUTO_INCREMENT PRIMARY KEY,
  deposit_id INT NOT NULL,
  payment_id INT NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (deposit_id) REFERENCES deposits(deposit_id),
  FOREIGN KEY (payment_id) REFERENCES payments(payment_id)
);

CREATE TABLE bank_accounts (
  bank_account_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  account_type VARCHAR(30) NOT NULL,
  account_number_last4 VARCHAR(4),
  routing_number VARCHAR(20),
  institution_name VARCHAR(200),
  current_balance DECIMAL(18,2) DEFAULT 0,
  bank_balance DECIMAL(18,2) DEFAULT 0,
  currency_id INT,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  last_reconciled_date DATE,
  last_reconciled_balance DECIMAL(18,2),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id)
);

CREATE TABLE bank_transactions (
  transaction_id INT AUTO_INCREMENT PRIMARY KEY,
  bank_account_id INT NOT NULL,
  transaction_date DATE NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  transaction_type VARCHAR(10) NOT NULL,
  description TEXT,
  payee VARCHAR(200),
  category VARCHAR(100),
  reference VARCHAR(100),
  check_number VARCHAR(50),
  is_reconciled TINYINT(1) NOT NULL DEFAULT 0,
  reconciled_date DATE,
  matched_transaction_id INT,
  match_confidence DECIMAL(5,4),
  memo TEXT,
  class_code_id INT,
  geo_location_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (bank_account_id) REFERENCES bank_accounts(bank_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

CREATE TABLE transfers (
  transfer_id INT AUTO_INCREMENT PRIMARY KEY,
  from_account_id INT NOT NULL,
  to_account_id INT NOT NULL,
  transfer_date DATE NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  currency_id INT,
  exchange_rate DECIMAL(18,6) DEFAULT 1,
  memo TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'pending',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (from_account_id) REFERENCES bank_accounts(bank_account_id),
  FOREIGN KEY (to_account_id) REFERENCES bank_accounts(bank_account_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id)
);

CREATE TABLE bank_statements (
  statement_id INT AUTO_INCREMENT PRIMARY KEY,
  bank_account_id INT NOT NULL,
  file_name VARCHAR(500) NOT NULL,
  file_path VARCHAR(1000) NOT NULL,
  file_size BIGINT NOT NULL,
  mime_type VARCHAR(100) NOT NULL,
  statement_date_from DATE NOT NULL,
  statement_date_to DATE NOT NULL,
  opening_balance DECIMAL(18,2) DEFAULT 0,
  closing_balance DECIMAL(18,2) DEFAULT 0,
  status VARCHAR(30) NOT NULL DEFAULT 'uploaded',
  uploaded_by VARCHAR(255) NOT NULL,
  uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  processed_at DATETIME,
  error_message TEXT,
  ocr_provider VARCHAR(30),
  raw_ocr_text LONGTEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (bank_account_id) REFERENCES bank_accounts(bank_account_id)
);

CREATE TABLE statement_transactions (
  stmt_txn_id INT AUTO_INCREMENT PRIMARY KEY,
  statement_id INT NOT NULL,
  transaction_date DATE NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  transaction_type VARCHAR(10) NOT NULL,
  description TEXT,
  payee VARCHAR(200),
  check_number VARCHAR(50),
  reference VARCHAR(100),
  balance DECIMAL(18,2),
  is_matched TINYINT(1) NOT NULL DEFAULT 0,
  matched_transaction_id INT,
  match_confidence DECIMAL(5,4),
  suggested_category VARCHAR(100),
  suggested_class_id INT,
  suggested_location_id INT,
  needs_review TINYINT(1) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (statement_id) REFERENCES bank_statements(statement_id),
  FOREIGN KEY (suggested_class_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (suggested_location_id) REFERENCES `geo_locations`(geo_location_id)
);

-- ============================================================================
-- 9. PROJECTS & TIME TRACKING (5 tables)
-- ============================================================================

CREATE TABLE time_entries (
  time_entry_id INT AUTO_INCREMENT PRIMARY KEY,
  client_engagement_id INT NOT NULL,
  employee_id INT,
  customer_id INT,
  product_service_id INT,
  entry_date DATE NOT NULL,
  hours DECIMAL(6,2) NOT NULL,
  rate DECIMAL(18,2),
  billable TINYINT(1) NOT NULL DEFAULT 1,
  description TEXT,
  status VARCHAR(20) NOT NULL DEFAULT 'draft',
  invoice_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (client_engagement_id) REFERENCES `client_engagements`(client_engagement_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (product_service_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (invoice_id) REFERENCES invoices(invoice_id)
);

CREATE TABLE employees (
  employee_id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  email VARCHAR(255),
  phone VARCHAR(50),
  hire_date DATE,
  termination_date DATE,
  department VARCHAR(100),
  position VARCHAR(100),
  pay_rate DECIMAL(18,2),
  pay_type VARCHAR(20),
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE recurring_templates (
  template_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  template_type VARCHAR(30) NOT NULL,
  frequency VARCHAR(20) NOT NULL,
  next_date DATE,
  end_date DATE,
  occurrences INT,
  customer_id INT,
  vendor_id INT,
  bank_account_id INT,
  memo TEXT,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  last_generated DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (vendor_id) REFERENCES vendors(vendor_id),
  FOREIGN KEY (bank_account_id) REFERENCES bank_accounts(bank_account_id)
);

CREATE TABLE recurring_template_lines (
  template_line_id INT AUTO_INCREMENT PRIMARY KEY,
  template_id INT NOT NULL,
  product_service_id INT,
  description TEXT,
  quantity DECIMAL(18,4) DEFAULT 1,
  unit_price DECIMAL(18,2) DEFAULT 0,
  gl_account_id INT,
  class_code_id INT,
  geo_location_id INT,
  tax_code_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (template_id) REFERENCES recurring_templates(template_id),
  FOREIGN KEY (product_service_id) REFERENCES `products_services`(product_service_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id),
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id)
);

-- ============================================================================
-- 10. TAX & COMPLIANCE (8 tables) - CRA Focused
-- ============================================================================

CREATE TABLE tax_filings (
  tax_filing_id INT AUTO_INCREMENT PRIMARY KEY,
  tax_code_id INT NOT NULL,
  period_start DATE NOT NULL,
  period_end DATE NOT NULL,
  due_date DATE NOT NULL,
  amount_due DECIMAL(18,2) DEFAULT 0,
  amount_paid DECIMAL(18,2) DEFAULT 0,
  status VARCHAR(30) NOT NULL DEFAULT 'open',
  filing_date DATE,
  confirmation_number VARCHAR(100),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tax_code_id) REFERENCES tax_codes(tax_code_id)
);

CREATE TABLE cra_business_profiles (
  profile_id INT AUTO_INCREMENT PRIMARY KEY,
  business_number VARCHAR(15) NOT NULL,
  program_accounts JSON,
  legal_name VARCHAR(200) NOT NULL,
  operating_name VARCHAR(200),
  address JSON,
  filing_frequency VARCHAR(20),
  reporting_period_end DATE,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  UNIQUE KEY uk_business_number (business_number)
);

CREATE TABLE cra_gst_hst_returns (
  return_id INT AUTO_INCREMENT PRIMARY KEY,
  profile_id INT NOT NULL,
  period_start DATE NOT NULL,
  period_end DATE NOT NULL,
  due_date DATE NOT NULL,
  filing_status VARCHAR(30) NOT NULL DEFAULT 'draft',
  total_sales DECIMAL(18,2) DEFAULT 0,
  taxable_sales DECIMAL(18,2) DEFAULT 0,
  zero_rated_sales DECIMAL(18,2) DEFAULT 0,
  exempt_sales DECIMAL(18,2) DEFAULT 0,
  gst_collected DECIMAL(18,2) DEFAULT 0,
  hst_collected DECIMAL(18,2) DEFAULT 0,
  gst_hst_paid_itcs DECIMAL(18,2) DEFAULT 0,
  net_tax DECIMAL(18,2) DEFAULT 0,
  installment_payments DECIMAL(18,2) DEFAULT 0,
  refund_claimed DECIMAL(18,2) DEFAULT 0,
  amount_owing DECIMAL(18,2) DEFAULT 0,
  filing_date DATE,
  confirmation_number VARCHAR(100),
  filed_by VARCHAR(255),
  notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (profile_id) REFERENCES cra_business_profiles(profile_id)
);

CREATE TABLE cra_gst_hst_line_details (
  line_id INT AUTO_INCREMENT PRIMARY KEY,
  return_id INT NOT NULL,
  line_number INT NOT NULL,
  description VARCHAR(200),
  amount DECIMAL(18,2) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (return_id) REFERENCES cra_gst_hst_returns(return_id),
  UNIQUE KEY uk_return_line (return_id, line_number)
);

CREATE TABLE cra_payroll_remittances (
  remittance_id INT AUTO_INCREMENT PRIMARY KEY,
  profile_id INT NOT NULL,
  period_start DATE NOT NULL,
  period_end DATE NOT NULL,
  due_date DATE NOT NULL,
  cpp_employee DECIMAL(18,2) DEFAULT 0,
  cpp_employer DECIMAL(18,2) DEFAULT 0,
  ei_employee DECIMAL(18,2) DEFAULT 0,
  ei_employer DECIMAL(18,2) DEFAULT 0,
  federal_tax DECIMAL(18,2) DEFAULT 0,
  provincial_tax DECIMAL(18,2) DEFAULT 0,
  total_remittance DECIMAL(18,2) DEFAULT 0,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  filing_date DATE,
  confirmation_number VARCHAR(100),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (profile_id) REFERENCES cra_business_profiles(profile_id)
);

CREATE TABLE cra_corp_tax_returns (
  return_id INT AUTO_INCREMENT PRIMARY KEY,
  profile_id INT NOT NULL,
  tax_year_end DATE NOT NULL,
  due_date DATE NOT NULL,
  filing_status VARCHAR(30) NOT NULL DEFAULT 'draft',
  net_income DECIMAL(18,2) DEFAULT 0,
  taxable_income DECIMAL(18,2) DEFAULT 0,
  federal_tax DECIMAL(18,2) DEFAULT 0,
  provincial_tax DECIMAL(18,2) DEFAULT 0,
  total_tax DECIMAL(18,2) DEFAULT 0,
  refund_applied DECIMAL(18,2) DEFAULT 0,
  balance_due DECIMAL(18,2) DEFAULT 0,
  filing_date DATE,
  confirmation_number VARCHAR(100),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (profile_id) REFERENCES cra_business_profiles(profile_id)
);

CREATE TABLE cra_notices_assessments (
  notice_id INT AUTO_INCREMENT PRIMARY KEY,
  profile_id INT NOT NULL,
  notice_type VARCHAR(50) NOT NULL,
  tax_year INT NOT NULL,
  date_issued DATE NOT NULL,
  amount_owing DECIMAL(18,2) DEFAULT 0,
  amount_refund DECIMAL(18,2) DEFAULT 0,
  balance DECIMAL(18,2) DEFAULT 0,
  pdf_path VARCHAR(1000),
  is_reviewed TINYINT(1) NOT NULL DEFAULT 0,
  reviewed_by VARCHAR(255),
  reviewed_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (profile_id) REFERENCES cra_business_profiles(profile_id)
);

-- ============================================================================
-- 11. AUDIT MANAGEMENT (8 tables)
-- ============================================================================

CREATE TABLE audit_engagements (
  engagement_id INT AUTO_INCREMENT PRIMARY KEY,
  client_name VARCHAR(200) NOT NULL,
  fiscal_year_end DATE NOT NULL,
  engagement_type VARCHAR(30) NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'planning',
  partner_id INT,
  manager_id INT,
  senior_id INT,
  staff_ids JSON,
  planning_materiality DECIMAL(18,2),
  performance_materiality DECIMAL(18,2),
  trivial_threshold DECIMAL(18,2),
  risk_assessment TEXT,
  engagement_letter_path VARCHAR(1000),
  started_at DATETIME,
  completed_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE audit_workpapers (
  workpaper_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  reference VARCHAR(50) NOT NULL,
  title VARCHAR(200) NOT NULL,
  description TEXT,
  preparer_id INT,
  preparer_date DATETIME,
  reviewer_id INT,
  reviewer_date DATETIME,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  file_path VARCHAR(1000),
  tick_marks JSON,
  cross_references JSON,
  conclusions TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (engagement_id) REFERENCES audit_engagements(engagement_id)
);

CREATE TABLE audit_programs (
  program_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  industry VARCHAR(100),
  risk_level VARCHAR(20),
  description TEXT,
  is_template TINYINT(1) NOT NULL DEFAULT 1,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE audit_procedures (
  procedure_id INT AUTO_INCREMENT PRIMARY KEY,
  program_id INT NOT NULL,
  engagement_id INT,
  step_number VARCHAR(20) NOT NULL,
  title VARCHAR(200) NOT NULL,
  description TEXT,
  objective TEXT,
  procedure_type VARCHAR(30),
  estimated_hours DECIMAL(6,2),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (program_id) REFERENCES audit_programs(program_id),
  FOREIGN KEY (engagement_id) REFERENCES audit_engagements(engagement_id)
);

CREATE TABLE procedure_executions (
  execution_id INT AUTO_INCREMENT PRIMARY KEY,
  procedure_id INT NOT NULL,
  engagement_id INT NOT NULL,
  tester_id INT NOT NULL,
  execution_date DATETIME NOT NULL,
  evidence TEXT,
  conclusion VARCHAR(30),
  exceptions TEXT,
  hours_spent DECIMAL(6,2),
  reviewer_id INT,
  review_date DATETIME,
  review_notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (procedure_id) REFERENCES audit_procedures(procedure_id),
  FOREIGN KEY (engagement_id) REFERENCES audit_engagements(engagement_id)
);

CREATE TABLE sampling_plans (
  plan_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  procedure_id INT,
  name VARCHAR(200) NOT NULL,
  sampling_method VARCHAR(30) NOT NULL,
  population_size INT,
  sample_size INT,
  confidence_level DECIMAL(5,2),
  tolerable_error DECIMAL(18,2),
  expected_error DECIMAL(18,2),
  stratification JSON,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (engagement_id) REFERENCES audit_engagements(engagement_id),
  FOREIGN KEY (procedure_id) REFERENCES audit_procedures(procedure_id)
);

CREATE TABLE sample_items (
  sample_item_id INT AUTO_INCREMENT PRIMARY KEY,
  plan_id INT NOT NULL,
  item_identifier VARCHAR(200) NOT NULL,
  item_value DECIMAL(18,2),
  is_selected TINYINT(1) NOT NULL DEFAULT 0,
  test_result VARCHAR(30),
  test_notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (plan_id) REFERENCES sampling_plans(plan_id)
);

CREATE TABLE analytical_procedures (
  analytical_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  name VARCHAR(200) NOT NULL,
  procedure_type VARCHAR(50) NOT NULL,
  account_ids JSON,
  comparison_periods JSON,
  thresholds JSON,
  results_json JSON,
  explanations TEXT,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  preparer_id INT,
  preparer_date DATETIME,
  reviewer_id INT,
  review_date DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (engagement_id) REFERENCES audit_engagements(engagement_id)
);

CREATE TABLE representation_letters (
  letter_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  letter_type VARCHAR(50) NOT NULL,
  status VARCHAR(30) NOT NULL DEFAULT 'draft',
  content LONGTEXT,
  sent_date DATETIME,
  signed_date DATETIME,
  signed_by VARCHAR(255),
  file_path VARCHAR(1000),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (engagement_id) REFERENCES audit_engagements(engagement_id)
);

-- ============================================================================
-- 12. ATTACHMENTS & DOCUMENTS (1 table)
-- ============================================================================

CREATE TABLE attachments (
  attachment_id INT AUTO_INCREMENT PRIMARY KEY,
  entity_type VARCHAR(50) NOT NULL,
  entity_id INT NOT NULL,
  file_name VARCHAR(500) NOT NULL,
  file_path VARCHAR(1000) NOT NULL,
  file_size BIGINT NOT NULL,
  mime_type VARCHAR(100) NOT NULL,
  uploaded_by VARCHAR(255) NOT NULL,
  uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  description TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

-- ============================================================================
-- 13. BUDGETING (2 tables)
-- ============================================================================

CREATE TABLE budgets (
  budget_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  fiscal_year INT NOT NULL,
  interval_type VARCHAR(20) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE budget_lines (
  budget_line_id INT AUTO_INCREMENT PRIMARY KEY,
  budget_id INT NOT NULL,
  gl_account_id INT NOT NULL,
  class_code_id INT,
  geo_location_id INT,
  period_start DATE NOT NULL,
  period_end DATE NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (budget_id) REFERENCES budgets(budget_id),
  FOREIGN KEY (gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (class_code_id) REFERENCES `class_codes`(class_code_id),
  FOREIGN KEY (geo_location_id) REFERENCES `geo_locations`(geo_location_id)
);

-- ============================================================================
-- 14. FIXED ASSETS (2 tables)
-- ============================================================================

CREATE TABLE fixed_assets (
  asset_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  description TEXT,
  purchase_date DATE NOT NULL,
  purchase_cost DECIMAL(18,2) NOT NULL,
  salvage_value DECIMAL(18,2) DEFAULT 0,
  useful_life_months INT NOT NULL,
  depreciation_method VARCHAR(30) NOT NULL DEFAULT 'straight_line',
  depreciation_gl_account_id INT,
  accumulated_depreciation_gl_account_id INT,
  depreciation_expense_account_id INT,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  disposed_date DATE,
  disposal_amount DECIMAL(18,2),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (depreciation_gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (accumulated_depreciation_gl_account_id) REFERENCES `gl_accounts`(gl_account_id),
  FOREIGN KEY (depreciation_expense_account_id) REFERENCES `gl_accounts`(gl_account_id)
);

CREATE TABLE asset_depreciation (
  depreciation_id INT AUTO_INCREMENT PRIMARY KEY,
  asset_id INT NOT NULL,
  period_start DATE NOT NULL,
  period_end DATE NOT NULL,
  depreciation_amount DECIMAL(18,2) NOT NULL,
  accumulated_depreciation DECIMAL(18,2) NOT NULL,
  gl_entry_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (asset_id) REFERENCES fixed_assets(asset_id),
  FOREIGN KEY (gl_entry_id) REFERENCES `gl_entries`(gl_entry_id)
);

-- ============================================================================
-- 15. AI AGENT SYSTEM (6 tables)
-- ============================================================================

CREATE TABLE agent_definitions (
  agent_definition_id INT AUTO_INCREMENT PRIMARY KEY,
  agent_type VARCHAR(50) NOT NULL,
  name VARCHAR(100) NOT NULL,
  description TEXT,
  system_prompt LONGTEXT NOT NULL,
  tools_json JSON NOT NULL,
  model_config_json JSON,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  is_system TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE agent_conversations (
  conversation_id INT AUTO_INCREMENT PRIMARY KEY,
  agent_definition_id INT NOT NULL,
  user_id INT,
  title VARCHAR(200),
  status VARCHAR(20) NOT NULL DEFAULT 'active',
  context_json JSON,
  started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  completed_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (agent_definition_id) REFERENCES agent_definitions(agent_definition_id)
);

CREATE TABLE agent_messages (
  message_id INT AUTO_INCREMENT PRIMARY KEY,
  conversation_id INT NOT NULL,
  role VARCHAR(20) NOT NULL,
  content LONGTEXT,
  tool_calls JSON,
  tool_results JSON,
  metadata_json JSON,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (conversation_id) REFERENCES agent_conversations(conversation_id)
);

CREATE TABLE agent_workflows (
  workflow_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  description TEXT,
  agent_sequence_json JSON NOT NULL,
  trigger_type VARCHAR(30),
  trigger_config_json JSON,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE agent_workflow_executions (
  execution_id INT AUTO_INCREMENT PRIMARY KEY,
  workflow_id INT NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'running',
  current_agent_index INT DEFAULT 0,
  shared_context_json JSON,
  started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  completed_at DATETIME,
  error_message TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (workflow_id) REFERENCES agent_workflows(workflow_id)
);

CREATE TABLE agent_human_escalations (
  escalation_id INT AUTO_INCREMENT PRIMARY KEY,
  execution_id INT NOT NULL,
  conversation_id INT,
  reason VARCHAR(200) NOT NULL,
  details TEXT,
  assigned_to_user_id INT,
  status VARCHAR(20) NOT NULL DEFAULT 'pending',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (execution_id) REFERENCES agent_workflow_executions(execution_id),
  FOREIGN KEY (conversation_id) REFERENCES agent_conversations(conversation_id)
);

-- ============================================================================
-- 16. VECTOR EMBEDDINGS (for agent memory & semantic search)
-- Note: VECTOR type requires PostgreSQL with pgvector extension.
-- For MySQL, we store as JSON and use external vector DB.
-- ============================================================================

CREATE TABLE document_embeddings (
  embedding_id INT AUTO_INCREMENT PRIMARY KEY,
  source_document_id INT NOT NULL,
  chunk_index INT NOT NULL,
  content_hash VARCHAR(64) NOT NULL,
  content_text TEXT NOT NULL,
  embedding_json JSON NOT NULL,
  metadata_json JSON,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (source_document_id) REFERENCES `source_documents`(source_document_id)
);

CREATE TABLE knowledge_base_embeddings (
  embedding_id INT AUTO_INCREMENT PRIMARY KEY,
  source_type VARCHAR(50) NOT NULL,
  source_id VARCHAR(200) NOT NULL,
  chunk_index INT NOT NULL,
  content_text TEXT NOT NULL,
  embedding_json JSON NOT NULL,
  metadata_json JSON,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE agent_conversation_embeddings (
  embedding_id INT AUTO_INCREMENT PRIMARY KEY,
  conversation_id INT NOT NULL,
  message_id INT NOT NULL,
  role VARCHAR(20) NOT NULL,
  content_text TEXT NOT NULL,
  embedding_json JSON NOT NULL,
  metadata_json JSON,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (conversation_id) REFERENCES agent_conversations(conversation_id)
);

CREATE TABLE workpaper_embeddings (
  embedding_id INT AUTO_INCREMENT PRIMARY KEY,
  engagement_id INT NOT NULL,
  workpaper_id INT NOT NULL,
  chunk_index INT NOT NULL,
  content_text TEXT NOT NULL,
  embedding_json JSON NOT NULL,
  metadata_json JSON,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (engagement_id) REFERENCES audit_engagements(engagement_id),
  FOREIGN KEY (workpaper_id) REFERENCES audit_workpapers(workpaper_id)
);
