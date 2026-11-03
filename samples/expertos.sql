-- ExpertOS: multi-tenant AI professional services platform (all domains from the spec)

CREATE TABLE saas_tenants (
  saas_tenant_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_code VARCHAR(50) NOT NULL,
  name VARCHAR(255) NOT NULL,
  legal_name VARCHAR(255),
  primary_email VARCHAR(255),
  country VARCHAR(100),
  region VARCHAR(50),
  timezone VARCHAR(50),
  deployment_model VARCHAR(50) NOT NULL DEFAULT 'shared',
  status VARCHAR(50) NOT NULL DEFAULT 'trial',
  trial_ends_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE saas_plans (
  saas_plan_id INT AUTO_INCREMENT PRIMARY KEY,
  plan_code VARCHAR(50) NOT NULL,
  name VARCHAR(255) NOT NULL,
  monthly_price DECIMAL(19,4),
  annual_price DECIMAL(19,4),
  currency VARCHAR(20) NOT NULL DEFAULT 'USD',
  included_seats INT,
  included_verticals INT,
  monthly_ai_credits INT,
  ai_tier VARCHAR(50),
  workflow_tier VARCHAR(50),
  analytics_tier VARCHAR(50),
  sso_enabled TINYINT(1) NOT NULL DEFAULT '0',
  dedicated_infra_option TINYINT(1) NOT NULL DEFAULT '0',
  is_custom TINYINT(1) NOT NULL DEFAULT '0',
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE saas_plan_features (
  saas_plan_feature_id INT AUTO_INCREMENT PRIMARY KEY,
  saas_plan_id INT NOT NULL,
  feature_code VARCHAR(100) NOT NULL,
  included_quantity INT,
  is_enabled TINYINT(1) NOT NULL DEFAULT '1',
  overage_unit_price DECIMAL(19,4),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (saas_plan_id) REFERENCES saas_plans(saas_plan_id)
);

CREATE TABLE saas_subscriptions (
  saas_subscription_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  saas_plan_id INT NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'trial',
  billing_cycle VARCHAR(50) NOT NULL DEFAULT 'monthly',
  seats INT,
  started_at DATETIME,
  current_period_start DATETIME,
  current_period_end DATETIME,
  trial_ends_at DATETIME,
  grace_ends_at DATETIME,
  cancelled_at DATETIME,
  cancel_reason TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (saas_plan_id) REFERENCES saas_plans(saas_plan_id)
);

CREATE TABLE saas_subscription_items (
  saas_subscription_item_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  saas_subscription_id INT NOT NULL,
  feature_code VARCHAR(100) NOT NULL,
  quantity INT,
  unit_price DECIMAL(19,4),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (saas_subscription_id) REFERENCES saas_subscriptions(saas_subscription_id)
);

CREATE TABLE saas_usage_records (
  saas_usage_record_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  saas_subscription_id INT,
  metric_code VARCHAR(50) NOT NULL,
  quantity DECIMAL(9,4) NOT NULL,
  unit_price DECIMAL(19,4),
  period_start DATE,
  period_end DATE,
  is_billable TINYINT(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (saas_subscription_id) REFERENCES saas_subscriptions(saas_subscription_id)
);

CREATE TABLE saas_invoices (
  saas_invoice_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  saas_subscription_id INT,
  invoice_number VARCHAR(50) NOT NULL,
  period_start DATE,
  period_end DATE,
  subtotal DECIMAL(19,4) NOT NULL DEFAULT 0,
  tax DECIMAL(19,4) NOT NULL DEFAULT 0,
  total DECIMAL(19,4) NOT NULL DEFAULT 0,
  currency VARCHAR(20) NOT NULL DEFAULT 'USD',
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  due_date DATE,
  issued_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (saas_subscription_id) REFERENCES saas_subscriptions(saas_subscription_id)
);

CREATE TABLE saas_invoice_items (
  saas_invoice_item_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  saas_invoice_id INT NOT NULL,
  description VARCHAR(255) NOT NULL,
  quantity DECIMAL(9,4) NOT NULL DEFAULT 1,
  unit_price DECIMAL(19,4) NOT NULL DEFAULT 0,
  amount DECIMAL(19,4) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (saas_invoice_id) REFERENCES saas_invoices(saas_invoice_id)
);

CREATE TABLE saas_payments (
  saas_payment_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  saas_invoice_id INT NOT NULL,
  provider_code VARCHAR(50) NOT NULL,
  provider_reference VARCHAR(150),
  idempotency_key VARCHAR(100),
  amount DECIMAL(19,4) NOT NULL,
  currency VARCHAR(20) NOT NULL DEFAULT 'USD',
  status VARCHAR(50) NOT NULL DEFAULT 'pending',
  paid_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (saas_invoice_id) REFERENCES saas_invoices(saas_invoice_id)
);

CREATE TABLE platform_roles (
  platform_role_id INT AUTO_INCREMENT PRIMARY KEY,
  role_code VARCHAR(50) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE platform_users (
  platform_user_id INT AUTO_INCREMENT PRIMARY KEY,
  platform_role_id INT,
  login_username VARCHAR(100),
  full_name VARCHAR(255) NOT NULL,
  email VARCHAR(255) NOT NULL,
  mfa_enabled TINYINT(1) NOT NULL DEFAULT '1',
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  last_login_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (platform_role_id) REFERENCES platform_roles(platform_role_id)
);

CREATE TABLE platform_audit_logs (
  platform_audit_log_id INT AUTO_INCREMENT PRIMARY KEY,
  platform_user_id INT,
  tenant_ref_id INT,
  action_code VARCHAR(100) NOT NULL,
  entity_type VARCHAR(100),
  entity_ref VARCHAR(100),
  ip_address VARCHAR(50),
  correlation_ref VARCHAR(100),
  details TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (platform_user_id) REFERENCES platform_users(platform_user_id),
  FOREIGN KEY (tenant_ref_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE feature_flags (
  feature_flag_id INT AUTO_INCREMENT PRIMARY KEY,
  feature_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  default_enabled TINYINT(1) NOT NULL DEFAULT '0',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE tenant_feature_overrides (
  tenant_feature_override_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  feature_flag_id INT NOT NULL,
  is_enabled TINYINT(1) NOT NULL DEFAULT '0',
  reason VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (feature_flag_id) REFERENCES feature_flags(feature_flag_id)
);

CREATE TABLE platform_settings (
  platform_setting_id INT AUTO_INCREMENT PRIMARY KEY,
  config_key VARCHAR(100) NOT NULL,
  config_value TEXT,
  value_type VARCHAR(50) DEFAULT 'string',
  description VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE system_health_events (
  system_health_event_id INT AUTO_INCREMENT PRIMARY KEY,
  component VARCHAR(100) NOT NULL,
  severity VARCHAR(50) NOT NULL DEFAULT 'info',
  message TEXT,
  resolved_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE organizations (
  organization_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  legal_name VARCHAR(255) NOT NULL,
  trading_name VARCHAR(255),
  industry VARCHAR(100),
  company_size VARCHAR(50),
  tax_ref VARCHAR(100),
  country VARCHAR(100),
  timezone VARCHAR(50),
  default_currency VARCHAR(20) DEFAULT 'USD',
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE organization_settings (
  organization_setting_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  setting_key VARCHAR(100) NOT NULL,
  setting_value TEXT,
  value_type VARCHAR(50) DEFAULT 'string',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE organization_branding (
  organization_branding_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  logo_key VARCHAR(255),
  portal_title VARCHAR(255),
  primary_color VARCHAR(20),
  secondary_color VARCHAR(20),
  custom_domain VARCHAR(255),
  email_from_name VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE customers (
  customer_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_code VARCHAR(50) NOT NULL,
  name VARCHAR(255) NOT NULL,
  customer_type VARCHAR(50) NOT NULL DEFAULT 'business',
  industry VARCHAR(100),
  company_size VARCHAR(50),
  country VARCHAR(100),
  billing_email VARCHAR(255),
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE tenant_users (
  tenant_user_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  login_username VARCHAR(100),
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  email VARCHAR(255) NOT NULL,
  job_title VARCHAR(255),
  user_type VARCHAR(50) NOT NULL DEFAULT 'employee',
  customer_id INT,
  mfa_enabled TINYINT(1) NOT NULL DEFAULT '0',
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  last_login_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE tenant_roles (
  tenant_role_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  role_code VARCHAR(50) NOT NULL,
  role_name VARCHAR(255) NOT NULL,
  description TEXT,
  is_system TINYINT(1) NOT NULL DEFAULT '0',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE permission_catalog (
  permission_catalog_entry_id INT AUTO_INCREMENT PRIMARY KEY,
  permission_code VARCHAR(100) NOT NULL,
  module_code VARCHAR(50) NOT NULL,
  description VARCHAR(255),
  risk_level VARCHAR(50) DEFAULT 'normal',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE tenant_user_roles (
  tenant_user_role_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  tenant_user_id INT NOT NULL,
  tenant_role_id INT NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id),
  FOREIGN KEY (tenant_role_id) REFERENCES tenant_roles(tenant_role_id)
);

CREATE TABLE tenant_role_permissions (
  tenant_role_permission_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  tenant_role_id INT NOT NULL,
  permission_catalog_entry_id INT NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (tenant_role_id) REFERENCES tenant_roles(tenant_role_id),
  FOREIGN KEY (permission_catalog_entry_id) REFERENCES permission_catalog(permission_catalog_entry_id)
);

CREATE TABLE user_invitations (
  user_invitation_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  email VARCHAR(255) NOT NULL,
  tenant_role_id INT,
  invited_by_id INT,
  token_hash VARCHAR(255),
  status VARCHAR(50) NOT NULL DEFAULT 'pending',
  expires_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (tenant_role_id) REFERENCES tenant_roles(tenant_role_id),
  FOREIGN KEY (invited_by_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE teams (
  team_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  team_name VARCHAR(255) NOT NULL,
  team_lead_id INT,
  description TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (team_lead_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE team_members (
  team_member_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  team_id INT NOT NULL,
  tenant_user_id INT NOT NULL,
  member_role VARCHAR(50) DEFAULT 'member',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (team_id) REFERENCES teams(team_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE user_skills (
  user_skill_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  tenant_user_id INT NOT NULL,
  skill_code VARCHAR(100) NOT NULL,
  proficiency VARCHAR(50) DEFAULT 'intermediate',
  certified_at DATE,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE verticals (
  vertical_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_code VARCHAR(20) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  compliance_notes TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'available',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE vertical_versions (
  vertical_version_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT NOT NULL,
  version_label VARCHAR(20) NOT NULL,
  release_notes TEXT,
  config_json TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  released_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE vertical_modules (
  vertical_module_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT NOT NULL,
  module_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE tenant_verticals (
  tenant_vertical_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  vertical_id INT NOT NULL,
  vertical_version_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  activated_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id),
  FOREIGN KEY (vertical_version_id) REFERENCES vertical_versions(vertical_version_id)
);

CREATE TABLE tenant_module_settings (
  tenant_module_setting_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  vertical_module_id INT NOT NULL,
  is_enabled TINYINT(1) NOT NULL DEFAULT '1',
  settings_json TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (vertical_module_id) REFERENCES vertical_modules(vertical_module_id)
);

CREATE TABLE vertical_knowledge_sources (
  vertical_knowledge_source_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT NOT NULL,
  name VARCHAR(255) NOT NULL,
  source_type VARCHAR(50),
  uri VARCHAR(255),
  description TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE vertical_validation_rules (
  vertical_validation_rule_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT NOT NULL,
  rule_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  applies_to VARCHAR(100),
  rule_expression TEXT,
  severity VARCHAR(50) DEFAULT 'error',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE document_types (
  document_type_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT,
  type_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  extraction_schema_json TEXT,
  schema_version VARCHAR(20) DEFAULT 1.0,
  is_platform_defined TINYINT(1) NOT NULL DEFAULT '0',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE report_templates (
  report_template_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT,
  template_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  output_format VARCHAR(50) NOT NULL DEFAULT 'pdf',
  sections_json TEXT,
  template_body TEXT,
  version_label VARCHAR(20) DEFAULT 1.0,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE service_categories (
  service_category_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  vertical_id INT,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE services (
  service_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  vertical_id INT NOT NULL,
  service_category_id INT,
  service_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  estimated_turnaround_days INT,
  requires_human_review TINYINT(1) NOT NULL DEFAULT '1',
  requires_qa TINYINT(1) NOT NULL DEFAULT '1',
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id),
  FOREIGN KEY (service_category_id) REFERENCES service_categories(service_category_id)
);

CREATE TABLE ai_providers (
  ai_provider_id INT AUTO_INCREMENT PRIMARY KEY,
  provider_code VARCHAR(50) NOT NULL,
  name VARCHAR(255) NOT NULL,
  base_url VARCHAR(255),
  data_policy TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE ai_models (
  ai_model_id INT AUTO_INCREMENT PRIMARY KEY,
  ai_provider_id INT NOT NULL,
  model_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  input_cost_per_mtok DECIMAL(19,4),
  output_cost_per_mtok DECIMAL(19,4),
  context_window INT,
  timeout_seconds INT DEFAULT 120,
  max_retries INT DEFAULT 2,
  fallback_model_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (ai_provider_id) REFERENCES ai_providers(ai_provider_id)
);

CREATE TABLE ai_prompts (
  ai_prompt_id INT AUTO_INCREMENT PRIMARY KEY,
  prompt_code VARCHAR(100) NOT NULL,
  version_label VARCHAR(20) NOT NULL,
  prompt_content TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE ai_agents (
  ai_agent_id INT AUTO_INCREMENT PRIMARY KEY,
  agent_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  agent_scope VARCHAR(50) NOT NULL DEFAULT 'shared',
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE ai_agent_versions (
  ai_agent_version_id INT AUTO_INCREMENT PRIMARY KEY,
  ai_agent_id INT NOT NULL,
  version_label VARCHAR(20) NOT NULL,
  instructions TEXT,
  tool_permissions TEXT,
  output_schema_json TEXT,
  ai_model_id INT,
  ai_prompt_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  released_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (ai_agent_id) REFERENCES ai_agents(ai_agent_id),
  FOREIGN KEY (ai_model_id) REFERENCES ai_models(ai_model_id),
  FOREIGN KEY (ai_prompt_id) REFERENCES ai_prompts(ai_prompt_id)
);

CREATE TABLE vertical_agent_profiles (
  vertical_agent_profile_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT NOT NULL,
  ai_agent_id INT NOT NULL,
  ai_agent_version_id INT,
  role_in_workflow VARCHAR(100),
  config_json TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id),
  FOREIGN KEY (ai_agent_id) REFERENCES ai_agents(ai_agent_id),
  FOREIGN KEY (ai_agent_version_id) REFERENCES ai_agent_versions(ai_agent_version_id)
);

CREATE TABLE ai_workflows (
  ai_workflow_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT,
  workflow_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE ai_workflow_versions (
  ai_workflow_version_id INT AUTO_INCREMENT PRIMARY KEY,
  ai_workflow_id INT NOT NULL,
  version_label VARCHAR(20) NOT NULL,
  steps_json TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (ai_workflow_id) REFERENCES ai_workflows(ai_workflow_id)
);

CREATE TABLE wf_definitions (
  wf_definition_id INT AUTO_INCREMENT PRIMARY KEY,
  vertical_id INT,
  workflow_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE wf_versions (
  wf_version_id INT AUTO_INCREMENT PRIMARY KEY,
  wf_definition_id INT NOT NULL,
  version_label VARCHAR(20) NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  published_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (wf_definition_id) REFERENCES wf_definitions(wf_definition_id)
);

CREATE TABLE wf_stages (
  wf_stage_id INT AUTO_INCREMENT PRIMARY KEY,
  wf_version_id INT NOT NULL,
  stage_code VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  stage_type VARCHAR(50) NOT NULL DEFAULT 'human',
  sort_order INT,
  assigned_role_code VARCHAR(100),
  sla_hours INT,
  is_terminal TINYINT(1) NOT NULL DEFAULT '0',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (wf_version_id) REFERENCES wf_versions(wf_version_id)
);

CREATE TABLE wf_transitions (
  wf_transition_id INT AUTO_INCREMENT PRIMARY KEY,
  wf_version_id INT NOT NULL,
  from_stage_id INT NOT NULL,
  to_stage_id INT NOT NULL,
  transition_code VARCHAR(100) NOT NULL,
  required_permission VARCHAR(100),
  allowed_actor VARCHAR(50) DEFAULT 'human',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (wf_version_id) REFERENCES wf_versions(wf_version_id),
  FOREIGN KEY (from_stage_id) REFERENCES wf_stages(wf_stage_id),
  FOREIGN KEY (to_stage_id) REFERENCES wf_stages(wf_stage_id)
);

CREATE TABLE wf_conditions (
  wf_condition_id INT AUTO_INCREMENT PRIMARY KEY,
  wf_transition_id INT NOT NULL,
  condition_expression TEXT NOT NULL,
  description VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (wf_transition_id) REFERENCES wf_transitions(wf_transition_id)
);

CREATE TABLE wf_actions (
  wf_action_id INT AUTO_INCREMENT PRIMARY KEY,
  wf_stage_id INT NOT NULL,
  run_on VARCHAR(50) NOT NULL DEFAULT 'enter',
  action_type VARCHAR(100) NOT NULL,
  action_config_json TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (wf_stage_id) REFERENCES wf_stages(wf_stage_id)
);

CREATE TABLE wf_assignments (
  wf_assignment_id INT AUTO_INCREMENT PRIMARY KEY,
  wf_stage_id INT NOT NULL,
  assignment_strategy VARCHAR(50) NOT NULL DEFAULT 'round_robin',
  team_ref VARCHAR(100),
  skill_code VARCHAR(100),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (wf_stage_id) REFERENCES wf_stages(wf_stage_id)
);

CREATE TABLE wf_sla_rules (
  wf_sla_rule_id INT AUTO_INCREMENT PRIMARY KEY,
  wf_version_id INT NOT NULL,
  wf_stage_id INT,
  sla_hours INT NOT NULL,
  warning_hours INT,
  escalate_to_role_code VARCHAR(100),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (wf_version_id) REFERENCES wf_versions(wf_version_id),
  FOREIGN KEY (wf_stage_id) REFERENCES wf_stages(wf_stage_id)
);

CREATE TABLE service_versions (
  service_version_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  service_id INT NOT NULL,
  version_label VARCHAR(20) NOT NULL,
  wf_version_id INT,
  ai_workflow_version_id INT,
  config_json TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  effective_from DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (service_id) REFERENCES services(service_id),
  FOREIGN KEY (wf_version_id) REFERENCES wf_versions(wf_version_id),
  FOREIGN KEY (ai_workflow_version_id) REFERENCES ai_workflow_versions(ai_workflow_version_id)
);

CREATE TABLE service_pricing (
  service_pricing_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  service_id INT NOT NULL,
  pricing_type VARCHAR(50) NOT NULL DEFAULT 'fixed',
  base_price DECIMAL(19,4) NOT NULL DEFAULT 0,
  unit_label VARCHAR(50),
  unit_price DECIMAL(19,4),
  currency VARCHAR(20) NOT NULL DEFAULT 'USD',
  effective_from DATE,
  effective_to DATE,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (service_id) REFERENCES services(service_id)
);

CREATE TABLE service_questions (
  service_question_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  service_id INT NOT NULL,
  field_code VARCHAR(100) NOT NULL,
  question_text TEXT NOT NULL,
  field_type VARCHAR(50) NOT NULL DEFAULT 'text',
  options_text TEXT,
  is_required TINYINT(1) NOT NULL DEFAULT '1',
  sort_order INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (service_id) REFERENCES services(service_id)
);

CREATE TABLE service_document_requirements (
  service_document_requirement_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  service_id INT NOT NULL,
  document_type_id INT NOT NULL,
  is_required TINYINT(1) NOT NULL DEFAULT '1',
  min_count INT DEFAULT 1,
  notes VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (service_id) REFERENCES services(service_id),
  FOREIGN KEY (document_type_id) REFERENCES document_types(document_type_id)
);

CREATE TABLE service_deliverable_templates (
  service_deliverable_template_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  service_id INT NOT NULL,
  report_template_id INT NOT NULL,
  is_default TINYINT(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (service_id) REFERENCES services(service_id),
  FOREIGN KEY (report_template_id) REFERENCES report_templates(report_template_id)
);

CREATE TABLE customer_contacts (
  customer_contact_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_id INT NOT NULL,
  full_name VARCHAR(255) NOT NULL,
  email VARCHAR(255),
  phone VARCHAR(50),
  job_title VARCHAR(255),
  is_primary TINYINT(1) NOT NULL DEFAULT '0',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE customer_addresses (
  customer_address_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_id INT NOT NULL,
  address_type VARCHAR(50) NOT NULL DEFAULT 'billing',
  line1 VARCHAR(255) NOT NULL,
  line2 VARCHAR(255),
  city VARCHAR(100),
  region VARCHAR(100),
  postal_code VARCHAR(20),
  country VARCHAR(100),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE customer_consents (
  customer_consent_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_id INT NOT NULL,
  consent_type VARCHAR(50) NOT NULL,
  granted TINYINT(1) NOT NULL DEFAULT '0',
  granted_at DATETIME,
  revoked_at DATETIME,
  evidence TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE customer_notes (
  customer_note_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_id INT NOT NULL,
  note_text TEXT NOT NULL,
  visibility VARCHAR(50) NOT NULL DEFAULT 'internal',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE cases (
  case_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  public_id VARCHAR(36),
  case_number VARCHAR(50) NOT NULL,
  customer_id INT NOT NULL,
  vertical_id INT NOT NULL,
  service_id INT NOT NULL,
  service_version_id INT,
  title VARCHAR(255) NOT NULL,
  description TEXT,
  customer_objective TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  priority VARCHAR(50) NOT NULL DEFAULT 'normal',
  channel VARCHAR(50) DEFAULT 'portal',
  owner_user_id INT,
  assigned_team_id INT,
  due_at DATETIME,
  submitted_at DATETIME,
  delivered_at DATETIME,
  closed_at DATETIME,
  cancel_reason TEXT,
  human_minutes INT DEFAULT 0,
  ai_cost DECIMAL(19,4) DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  UNIQUE KEY uq_cases_tenant_id_case_number (tenant_id, case_number),
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id),
  FOREIGN KEY (service_id) REFERENCES services(service_id),
  FOREIGN KEY (service_version_id) REFERENCES service_versions(service_version_id),
  FOREIGN KEY (owner_user_id) REFERENCES tenant_users(tenant_user_id),
  FOREIGN KEY (assigned_team_id) REFERENCES teams(team_id)
);

CREATE TABLE case_intake_responses (
  case_intake_response_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  service_question_id INT NOT NULL,
  response_value TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (service_question_id) REFERENCES service_questions(service_question_id)
);

CREATE TABLE case_assignments (
  case_assignment_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  tenant_user_id INT NOT NULL,
  assignment_role VARCHAR(50) NOT NULL DEFAULT 'expert',
  assigned_at DATETIME,
  unassigned_at DATETIME,
  is_current TINYINT(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE case_tasks (
  case_task_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  assigned_to_id INT,
  task_code VARCHAR(100),
  title VARCHAR(255) NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'open',
  due_at DATETIME,
  completed_at DATETIME,
  notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (assigned_to_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE case_comments (
  case_comment_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  tenant_user_id INT,
  comment_text TEXT NOT NULL,
  visibility VARCHAR(50) NOT NULL DEFAULT 'internal',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE case_status_history (
  case_status_history_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  old_status VARCHAR(50),
  new_status VARCHAR(50) NOT NULL,
  changed_by_type VARCHAR(50) NOT NULL DEFAULT 'human',
  changed_by_ref VARCHAR(100),
  reason TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id)
);

CREATE TABLE case_deadlines (
  case_deadline_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  deadline_type VARCHAR(50) NOT NULL,
  due_at DATETIME NOT NULL,
  met_at DATETIME,
  is_breached TINYINT(1) NOT NULL DEFAULT '0',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id)
);

CREATE TABLE case_escalations (
  case_escalation_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  reason TEXT NOT NULL,
  severity VARCHAR(50) NOT NULL DEFAULT 'medium',
  assigned_to_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'open',
  resolved_at DATETIME,
  resolution TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (assigned_to_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE case_information_requests (
  case_information_request_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  requested_items TEXT NOT NULL,
  message TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'open',
  due_at DATETIME,
  responded_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id)
);

CREATE TABLE case_messages (
  case_message_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  sender_type VARCHAR(50) NOT NULL DEFAULT 'staff',
  sender_user_id INT,
  message_text TEXT NOT NULL,
  read_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (sender_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE documents (
  document_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  public_id VARCHAR(36),
  case_id INT,
  customer_id INT,
  document_type_id INT,
  title VARCHAR(255) NOT NULL,
  original_filename VARCHAR(255),
  storage_key VARCHAR(255),
  mime_type VARCHAR(100),
  file_size INT,
  checksum_sha256 VARCHAR(100),
  classification VARCHAR(50) NOT NULL DEFAULT 'confidential',
  status VARCHAR(50) NOT NULL DEFAULT 'uploaded',
  retention_until DATE,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (document_type_id) REFERENCES document_types(document_type_id)
);

CREATE TABLE document_versions (
  document_version_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  document_id INT NOT NULL,
  version_number INT NOT NULL DEFAULT 1,
  storage_key VARCHAR(255) NOT NULL,
  checksum_sha256 VARCHAR(100),
  change_note VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id)
);

CREATE TABLE document_processing_jobs (
  document_processing_job_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  document_id INT NOT NULL,
  job_type VARCHAR(50) NOT NULL DEFAULT 'extract',
  status VARCHAR(50) NOT NULL DEFAULT 'queued',
  attempts INT DEFAULT 0,
  job_ref VARCHAR(100),
  started_at DATETIME,
  completed_at DATETIME,
  error_message TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id)
);

CREATE TABLE document_extractions (
  document_extraction_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  document_id INT NOT NULL,
  document_type_id INT,
  schema_version VARCHAR(20) DEFAULT 1.0,
  extracted_json TEXT,
  confidence DECIMAL(9,4),
  agent_run_ref INT,
  status VARCHAR(50) NOT NULL DEFAULT 'unverified',
  validated_by_id INT,
  validated_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id),
  FOREIGN KEY (document_type_id) REFERENCES document_types(document_type_id),
  FOREIGN KEY (validated_by_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE document_extracted_fields (
  document_extracted_field_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  document_extraction_id INT NOT NULL,
  field_code VARCHAR(100) NOT NULL,
  field_value TEXT,
  normalized_value TEXT,
  confidence DECIMAL(9,4),
  is_verified TINYINT(1) NOT NULL DEFAULT '0',
  corrected_value TEXT,
  corrected_by_id INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (document_extraction_id) REFERENCES document_extractions(document_extraction_id),
  FOREIGN KEY (corrected_by_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE document_validation_results (
  document_validation_result_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  document_extraction_id INT NOT NULL,
  vertical_validation_rule_id INT,
  validation_status VARCHAR(50) NOT NULL DEFAULT 'passed',
  issues TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (document_extraction_id) REFERENCES document_extractions(document_extraction_id),
  FOREIGN KEY (vertical_validation_rule_id) REFERENCES vertical_validation_rules(vertical_validation_rule_id)
);

CREATE TABLE document_evidence (
  document_evidence_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  document_extraction_id INT NOT NULL,
  document_extracted_field_id INT,
  page_number INT,
  sheet_name VARCHAR(100),
  cell_reference VARCHAR(50),
  row_ref VARCHAR(50),
  bounding_box VARCHAR(100),
  snippet TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (document_extraction_id) REFERENCES document_extractions(document_extraction_id),
  FOREIGN KEY (document_extracted_field_id) REFERENCES document_extracted_fields(document_extracted_field_id)
);

CREATE TABLE document_access_logs (
  document_access_log_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  document_id INT NOT NULL,
  tenant_user_id INT,
  access_type VARCHAR(50) NOT NULL DEFAULT 'view',
  ip_address VARCHAR(50),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE tenant_agent_configs (
  tenant_agent_config_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  ai_agent_version_id INT NOT NULL,
  vertical_id INT,
  ai_model_id INT,
  is_enabled TINYINT(1) NOT NULL DEFAULT '1',
  monthly_token_limit INT,
  config_json TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (ai_agent_version_id) REFERENCES ai_agent_versions(ai_agent_version_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id),
  FOREIGN KEY (ai_model_id) REFERENCES ai_models(ai_model_id)
);

CREATE TABLE tenant_model_permissions (
  tenant_model_permission_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  ai_model_id INT NOT NULL,
  is_allowed TINYINT(1) NOT NULL DEFAULT '1',
  max_tokens_per_run INT,
  data_handling_policy VARCHAR(50) DEFAULT 'no_training',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (ai_model_id) REFERENCES ai_models(ai_model_id)
);

CREATE TABLE ai_escalation_rules (
  ai_escalation_rule_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  ai_agent_id INT,
  rule_code VARCHAR(100) NOT NULL,
  condition_text TEXT NOT NULL,
  action_code VARCHAR(50) NOT NULL DEFAULT 'escalate_to_human',
  is_enabled TINYINT(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (ai_agent_id) REFERENCES ai_agents(ai_agent_id)
);

CREATE TABLE ai_workflow_runs (
  ai_workflow_run_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  ai_workflow_version_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'queued',
  correlation_ref VARCHAR(100),
  started_at DATETIME,
  completed_at DATETIME,
  error_message TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (ai_workflow_version_id) REFERENCES ai_workflow_versions(ai_workflow_version_id)
);

CREATE TABLE ai_agent_runs (
  ai_agent_run_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  ai_workflow_run_id INT,
  case_id INT NOT NULL,
  ai_agent_version_id INT NOT NULL,
  ai_model_id INT,
  ai_prompt_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'queued',
  attempt INT DEFAULT 1,
  input_refs_json TEXT,
  started_at DATETIME,
  completed_at DATETIME,
  input_tokens INT,
  output_tokens INT,
  cost DECIMAL(19,4),
  error_message TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (ai_workflow_run_id) REFERENCES ai_workflow_runs(ai_workflow_run_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (ai_agent_version_id) REFERENCES ai_agent_versions(ai_agent_version_id),
  FOREIGN KEY (ai_model_id) REFERENCES ai_models(ai_model_id),
  FOREIGN KEY (ai_prompt_id) REFERENCES ai_prompts(ai_prompt_id)
);

CREATE TABLE ai_agent_outputs (
  ai_agent_output_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  ai_agent_run_id INT NOT NULL,
  output_schema_version VARCHAR(20) DEFAULT 1.0,
  output_json TEXT,
  schema_valid TINYINT(1) NOT NULL DEFAULT '0',
  confidence DECIMAL(9,4),
  needs_human_review TINYINT(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (ai_agent_run_id) REFERENCES ai_agent_runs(ai_agent_run_id)
);

CREATE TABLE ai_findings (
  ai_finding_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  ai_agent_run_id INT,
  finding_code VARCHAR(100),
  finding_type VARCHAR(50) NOT NULL,
  severity VARCHAR(50) NOT NULL DEFAULT 'medium',
  title VARCHAR(255) NOT NULL,
  ai_description TEXT,
  expert_description TEXT,
  evidence_json TEXT,
  fact_basis VARCHAR(50) NOT NULL DEFAULT 'verified',
  confidence DECIMAL(9,4),
  review_status VARCHAR(50) NOT NULL DEFAULT 'pending',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (ai_agent_run_id) REFERENCES ai_agent_runs(ai_agent_run_id)
);

CREATE TABLE ai_recommendations (
  ai_recommendation_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  ai_finding_id INT,
  recommendation_code VARCHAR(100),
  ai_recommendation TEXT,
  expert_recommendation TEXT,
  rationale TEXT,
  estimated_value DECIMAL(19,4),
  requires_human_approval TINYINT(1) NOT NULL DEFAULT '1',
  review_status VARCHAR(50) NOT NULL DEFAULT 'pending',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (ai_finding_id) REFERENCES ai_findings(ai_finding_id)
);

CREATE TABLE ai_feedback (
  ai_feedback_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  ai_agent_run_id INT,
  ai_finding_id INT,
  reviewer_id INT,
  feedback_type VARCHAR(50) NOT NULL,
  comments TEXT,
  corrected_output_json TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (ai_agent_run_id) REFERENCES ai_agent_runs(ai_agent_run_id),
  FOREIGN KEY (ai_finding_id) REFERENCES ai_findings(ai_finding_id),
  FOREIGN KEY (reviewer_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE ai_evaluation_cases (
  ai_evaluation_case_id INT AUTO_INCREMENT PRIMARY KEY,
  ai_agent_id INT NOT NULL,
  vertical_id INT,
  name VARCHAR(255) NOT NULL,
  input_json TEXT,
  expected_output_json TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (ai_agent_id) REFERENCES ai_agents(ai_agent_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE ai_evaluations (
  ai_evaluation_id INT AUTO_INCREMENT PRIMARY KEY,
  ai_agent_version_id INT NOT NULL,
  ai_evaluation_case_id INT,
  score DECIMAL(9,4),
  result VARCHAR(50) NOT NULL DEFAULT 'pending',
  details TEXT,
  evaluated_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (ai_agent_version_id) REFERENCES ai_agent_versions(ai_agent_version_id),
  FOREIGN KEY (ai_evaluation_case_id) REFERENCES ai_evaluation_cases(ai_evaluation_case_id)
);

CREATE TABLE ai_usage_logs (
  ai_usage_log_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT,
  ai_agent_run_id INT,
  ai_model_id INT,
  input_tokens INT NOT NULL DEFAULT 0,
  output_tokens INT NOT NULL DEFAULT 0,
  cost DECIMAL(19,4) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (ai_agent_run_id) REFERENCES ai_agent_runs(ai_agent_run_id),
  FOREIGN KEY (ai_model_id) REFERENCES ai_models(ai_model_id)
);

CREATE TABLE knowledge_bases (
  knowledge_base_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  vertical_id INT,
  name VARCHAR(255) NOT NULL,
  description TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE knowledge_sources (
  knowledge_source_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  knowledge_base_id INT NOT NULL,
  vertical_knowledge_source_id INT,
  source_type VARCHAR(50) NOT NULL DEFAULT 'upload',
  name VARCHAR(255) NOT NULL,
  uri VARCHAR(255),
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (knowledge_base_id) REFERENCES knowledge_bases(knowledge_base_id),
  FOREIGN KEY (vertical_knowledge_source_id) REFERENCES vertical_knowledge_sources(vertical_knowledge_source_id)
);

CREATE TABLE knowledge_documents (
  knowledge_document_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  knowledge_source_id INT NOT NULL,
  title VARCHAR(255) NOT NULL,
  storage_key VARCHAR(255),
  version_label VARCHAR(20),
  effective_date DATE,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (knowledge_source_id) REFERENCES knowledge_sources(knowledge_source_id)
);

CREATE TABLE knowledge_chunks (
  knowledge_chunk_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  knowledge_document_id INT NOT NULL,
  chunk_index INT NOT NULL,
  content_text TEXT,
  page_number INT,
  token_count INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (knowledge_document_id) REFERENCES knowledge_documents(knowledge_document_id)
);

CREATE TABLE knowledge_embeddings (
  knowledge_embedding_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  knowledge_chunk_id INT NOT NULL,
  embedding_model VARCHAR(100) NOT NULL,
  vector_ref VARCHAR(255) NOT NULL,
  dimensions INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (knowledge_chunk_id) REFERENCES knowledge_chunks(knowledge_chunk_id)
);

CREATE TABLE knowledge_access_policies (
  knowledge_access_policy_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  knowledge_base_id INT NOT NULL,
  tenant_role_id INT,
  access_level VARCHAR(50) NOT NULL DEFAULT 'read',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (knowledge_base_id) REFERENCES knowledge_bases(knowledge_base_id),
  FOREIGN KEY (tenant_role_id) REFERENCES tenant_roles(tenant_role_id)
);

CREATE TABLE knowledge_ingestion_jobs (
  knowledge_ingestion_job_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  knowledge_source_id INT NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'queued',
  documents_processed INT DEFAULT 0,
  started_at DATETIME,
  completed_at DATETIME,
  error_message TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (knowledge_source_id) REFERENCES knowledge_sources(knowledge_source_id)
);

CREATE TABLE ai_recommendation_sources (
  ai_recommendation_source_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  ai_recommendation_id INT NOT NULL,
  knowledge_chunk_id INT,
  document_id INT,
  citation_note VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (ai_recommendation_id) REFERENCES ai_recommendations(ai_recommendation_id),
  FOREIGN KEY (knowledge_chunk_id) REFERENCES knowledge_chunks(knowledge_chunk_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id)
);

CREATE TABLE wf_instances (
  wf_instance_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  wf_version_id INT NOT NULL,
  current_stage_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'running',
  started_at DATETIME,
  completed_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (wf_version_id) REFERENCES wf_versions(wf_version_id),
  FOREIGN KEY (current_stage_id) REFERENCES wf_stages(wf_stage_id)
);

CREATE TABLE wf_stage_instances (
  wf_stage_instance_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  wf_instance_id INT NOT NULL,
  wf_stage_id INT NOT NULL,
  assigned_to_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'pending',
  entered_at DATETIME,
  exited_at DATETIME,
  due_at DATETIME,
  attempts INT DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (wf_instance_id) REFERENCES wf_instances(wf_instance_id),
  FOREIGN KEY (wf_stage_id) REFERENCES wf_stages(wf_stage_id),
  FOREIGN KEY (assigned_to_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE wf_events (
  wf_event_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  wf_instance_id INT NOT NULL,
  event_type VARCHAR(100) NOT NULL,
  from_stage_id INT,
  to_stage_id INT,
  actor_type VARCHAR(50) NOT NULL DEFAULT 'system',
  details TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (wf_instance_id) REFERENCES wf_instances(wf_instance_id),
  FOREIGN KEY (from_stage_id) REFERENCES wf_stages(wf_stage_id),
  FOREIGN KEY (to_stage_id) REFERENCES wf_stages(wf_stage_id)
);

CREATE TABLE expert_reviews (
  expert_review_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  reviewer_id INT,
  review_round INT NOT NULL DEFAULT 1,
  status VARCHAR(50) NOT NULL DEFAULT 'in_progress',
  started_at DATETIME,
  submitted_at DATETIME,
  summary TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (reviewer_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE expert_review_decisions (
  expert_review_decision_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  expert_review_id INT NOT NULL,
  ai_finding_id INT,
  ai_recommendation_id INT,
  decision VARCHAR(50) NOT NULL,
  comments TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (expert_review_id) REFERENCES expert_reviews(expert_review_id),
  FOREIGN KEY (ai_finding_id) REFERENCES ai_findings(ai_finding_id),
  FOREIGN KEY (ai_recommendation_id) REFERENCES ai_recommendations(ai_recommendation_id)
);

CREATE TABLE approvals (
  approval_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  approval_type VARCHAR(50) NOT NULL,
  requested_by_id INT,
  approver_id INT,
  status VARCHAR(50) NOT NULL DEFAULT 'pending',
  decided_at DATETIME,
  comments TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (requested_by_id) REFERENCES tenant_users(tenant_user_id),
  FOREIGN KEY (approver_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE approval_history (
  approval_history_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  approval_id INT NOT NULL,
  action_code VARCHAR(50) NOT NULL,
  actor_id INT,
  comments TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (approval_id) REFERENCES approvals(approval_id),
  FOREIGN KEY (actor_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE quality_checklists (
  quality_checklist_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  service_id INT,
  vertical_id INT,
  name VARCHAR(255) NOT NULL,
  version_label VARCHAR(20) DEFAULT 1.0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (service_id) REFERENCES services(service_id),
  FOREIGN KEY (vertical_id) REFERENCES verticals(vertical_id)
);

CREATE TABLE quality_checklist_items (
  quality_checklist_item_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  quality_checklist_id INT NOT NULL,
  item_code VARCHAR(100) NOT NULL,
  item_text VARCHAR(255) NOT NULL,
  issue_category VARCHAR(50),
  is_required TINYINT(1) NOT NULL DEFAULT '1',
  sort_order INT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (quality_checklist_id) REFERENCES quality_checklists(quality_checklist_id)
);

CREATE TABLE quality_reviews (
  quality_review_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  quality_checklist_id INT,
  reviewer_id INT,
  reviewer_type VARCHAR(50) NOT NULL DEFAULT 'human',
  score DECIMAL(9,4),
  status VARCHAR(50) NOT NULL DEFAULT 'in_progress',
  comments TEXT,
  completed_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (quality_checklist_id) REFERENCES quality_checklists(quality_checklist_id),
  FOREIGN KEY (reviewer_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE quality_review_items (
  quality_review_item_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  quality_review_id INT NOT NULL,
  quality_checklist_item_id INT NOT NULL,
  result VARCHAR(50) NOT NULL DEFAULT 'pending',
  issue_category VARCHAR(50),
  comments TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (quality_review_id) REFERENCES quality_reviews(quality_review_id),
  FOREIGN KEY (quality_checklist_item_id) REFERENCES quality_checklist_items(quality_checklist_item_id)
);

CREATE TABLE employee_performance_metrics (
  employee_performance_metric_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  tenant_user_id INT NOT NULL,
  metric_code VARCHAR(100) NOT NULL,
  metric_value DECIMAL(9,4) NOT NULL,
  period_start DATE,
  period_end DATE,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE employee_training_records (
  employee_training_record_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  tenant_user_id INT NOT NULL,
  skill_code VARCHAR(100) NOT NULL,
  course_name VARCHAR(255) NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'assigned',
  completed_at DATETIME,
  expires_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE tax_rates (
  tax_rate_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  region VARCHAR(100) NOT NULL,
  name VARCHAR(255) NOT NULL,
  rate_percent DECIMAL(9,4) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE discounts (
  discount_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  discount_code VARCHAR(50) NOT NULL,
  discount_type VARCHAR(50) NOT NULL DEFAULT 'percent',
  amount DECIMAL(19,4) NOT NULL,
  valid_until DATE,
  is_active_offer TINYINT(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE customer_quotes (
  customer_quote_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT,
  customer_id INT NOT NULL,
  quote_number VARCHAR(50) NOT NULL,
  subtotal DECIMAL(19,4) NOT NULL DEFAULT 0,
  discount_amount DECIMAL(19,4) DEFAULT 0,
  tax DECIMAL(19,4) NOT NULL DEFAULT 0,
  total DECIMAL(19,4) NOT NULL DEFAULT 0,
  currency VARCHAR(20) NOT NULL DEFAULT 'USD',
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  valid_until DATE,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE customer_invoices (
  customer_invoice_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  public_id VARCHAR(36),
  customer_id INT NOT NULL,
  case_id INT,
  customer_quote_id INT,
  invoice_number VARCHAR(50) NOT NULL,
  subtotal DECIMAL(19,4) NOT NULL DEFAULT 0,
  discount_amount DECIMAL(19,4) DEFAULT 0,
  tax DECIMAL(19,4) NOT NULL DEFAULT 0,
  total DECIMAL(19,4) NOT NULL DEFAULT 0,
  currency VARCHAR(20) NOT NULL DEFAULT 'USD',
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  due_date DATE,
  issued_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  UNIQUE KEY uq_customer_invoices_tenant_id_invoice_number (tenant_id, invoice_number),
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (customer_quote_id) REFERENCES customer_quotes(customer_quote_id)
);

CREATE TABLE customer_invoice_items (
  customer_invoice_item_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_invoice_id INT NOT NULL,
  description VARCHAR(255) NOT NULL,
  quantity DECIMAL(9,4) NOT NULL DEFAULT 1,
  unit_price DECIMAL(19,4) NOT NULL DEFAULT 0,
  amount DECIMAL(19,4) NOT NULL DEFAULT 0,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_invoice_id) REFERENCES customer_invoices(customer_invoice_id)
);

CREATE TABLE customer_payments (
  customer_payment_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_invoice_id INT NOT NULL,
  provider_code VARCHAR(50) NOT NULL,
  provider_reference VARCHAR(150),
  idempotency_key VARCHAR(100),
  amount DECIMAL(19,4) NOT NULL,
  currency VARCHAR(20) NOT NULL DEFAULT 'USD',
  status VARCHAR(50) NOT NULL DEFAULT 'pending',
  paid_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_invoice_id) REFERENCES customer_invoices(customer_invoice_id)
);

CREATE TABLE customer_refunds (
  customer_refund_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  customer_payment_id INT NOT NULL,
  amount DECIMAL(19,4) NOT NULL,
  reason TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'requested',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (customer_payment_id) REFERENCES customer_payments(customer_payment_id)
);

CREATE TABLE case_deliverables (
  case_deliverable_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  public_id VARCHAR(36),
  case_id INT NOT NULL,
  report_template_id INT,
  title VARCHAR(255) NOT NULL,
  output_format VARCHAR(50) NOT NULL DEFAULT 'pdf',
  version_number INT NOT NULL DEFAULT 1,
  file_key VARCHAR(255),
  executive_summary TEXT,
  content_markdown TEXT,
  limitations TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'draft',
  approved_by_id INT,
  delivered_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (report_template_id) REFERENCES report_templates(report_template_id),
  FOREIGN KEY (approved_by_id) REFERENCES tenant_users(tenant_user_id)
);

CREATE TABLE customer_feedback (
  customer_feedback_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  customer_id INT,
  rating INT NOT NULL,
  would_repurchase TINYINT(1) NOT NULL DEFAULT '0',
  comments TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

CREATE TABLE notification_templates (
  notification_template_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  template_code VARCHAR(100) NOT NULL,
  channel VARCHAR(50) NOT NULL DEFAULT 'email',
  subject VARCHAR(255),
  body TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'active',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE notifications (
  notification_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  tenant_user_id INT,
  notification_template_id INT,
  notification_type VARCHAR(50) NOT NULL,
  channel VARCHAR(50) NOT NULL DEFAULT 'in_app',
  title VARCHAR(255),
  message TEXT,
  entity_type VARCHAR(100),
  entity_ref VARCHAR(100),
  status VARCHAR(50) NOT NULL DEFAULT 'queued',
  sent_at DATETIME,
  read_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (tenant_user_id) REFERENCES tenant_users(tenant_user_id),
  FOREIGN KEY (notification_template_id) REFERENCES notification_templates(notification_template_id)
);

CREATE TABLE audit_logs (
  audit_log_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
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
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE data_retention_policies (
  data_retention_policy_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  entity_type VARCHAR(100) NOT NULL,
  retention_days INT NOT NULL,
  policy_action VARCHAR(50) NOT NULL DEFAULT 'archive',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE integration_connections (
  integration_connection_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  provider_code VARCHAR(50) NOT NULL,
  name VARCHAR(255) NOT NULL,
  status VARCHAR(50) NOT NULL DEFAULT 'inactive',
  config_json TEXT,
  secret_ref VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE webhook_events (
  webhook_event_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  provider_code VARCHAR(50) NOT NULL,
  external_event_id VARCHAR(150) NOT NULL,
  event_type VARCHAR(100),
  payload TEXT,
  signature_valid TINYINT(1) NOT NULL DEFAULT '0',
  status VARCHAR(50) NOT NULL DEFAULT 'received',
  processed_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  UNIQUE KEY uq_webhook_events_provider_code_external_event_id (provider_code, external_event_id),
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE outbox_events (
  outbox_event_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  event_type VARCHAR(100) NOT NULL,
  aggregate_type VARCHAR(100) NOT NULL,
  aggregate_ref VARCHAR(100) NOT NULL,
  schema_version VARCHAR(20) NOT NULL DEFAULT 1.0,
  correlation_ref VARCHAR(100),
  payload TEXT,
  status VARCHAR(50) NOT NULL DEFAULT 'pending',
  attempts INT DEFAULT 0,
  published_at DATETIME,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE analytics_daily_metrics (
  analytics_daily_metric_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  metric_date DATE NOT NULL,
  metric_code VARCHAR(100) NOT NULL,
  dimension_ref VARCHAR(100),
  metric_value DECIMAL(9,4) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE scm_suppliers (
  scm_supplier_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  name VARCHAR(255) NOT NULL,
  country VARCHAR(100),
  contact_email VARCHAR(255),
  website VARCHAR(255),
  notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id)
);

CREATE TABLE scm_quote_lines (
  scm_quote_line_id INT AUTO_INCREMENT PRIMARY KEY,
  tenant_id INT NOT NULL,
  case_id INT NOT NULL,
  document_id INT,
  document_extraction_id INT,
  scm_supplier_id INT NOT NULL,
  product_name VARCHAR(255) NOT NULL,
  specification TEXT,
  unit_of_measure VARCHAR(50),
  unit_price DECIMAL(19,4),
  currency VARCHAR(20),
  minimum_order_qty INT,
  lead_time_days INT,
  payment_terms VARCHAR(150),
  incoterms VARCHAR(50),
  freight_included TINYINT(1) NOT NULL DEFAULT '0',
  quote_valid_until DATE,
  source_reference VARCHAR(150),
  extraction_confidence DECIMAL(9,4),
  validation_status VARCHAR(50) NOT NULL DEFAULT 'unverified',
  expert_notes TEXT,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (tenant_id) REFERENCES saas_tenants(saas_tenant_id),
  FOREIGN KEY (case_id) REFERENCES cases(case_id),
  FOREIGN KEY (document_id) REFERENCES documents(document_id),
  FOREIGN KEY (document_extraction_id) REFERENCES document_extractions(document_extraction_id),
  FOREIGN KEY (scm_supplier_id) REFERENCES scm_suppliers(scm_supplier_id)
);
