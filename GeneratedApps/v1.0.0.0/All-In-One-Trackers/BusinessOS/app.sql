-- Database creation
CREATE DATABASE IF NOT EXISTS prod_project_management_system;
USE prod_project_management_system;

-- Set character set and collation
ALTER DATABASE prod_project_management_system CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- AppUsers table (replacing Users table)
CREATE TABLE AppUsers (
  app_user_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL,
  modifiedAt DATETIME NOT NULL,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

-- Projects table
CREATE TABLE projects (
  project_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  project_name VARCHAR(255) NOT NULL,
  description TEXT,
  start_date DATE,
  end_date DATE,
  status VARCHAR(50),
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1
);

-- Tasks table
CREATE TABLE tasks (
  task_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  project_id INT,
  task_name VARCHAR(255) NOT NULL,
  description TEXT,
  status VARCHAR(50),
  priority VARCHAR(50),
  due_date DATE,
  assigned_to INT,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (project_id) REFERENCES projects(project_id),
  FOREIGN KEY (assigned_to) REFERENCES AppUsers(app_user_id)
);

-- Comments table
CREATE TABLE comments (
  comment_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  task_id INT,
  app_user_id INT,
  comment_text TEXT,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (task_id) REFERENCES tasks(task_id),
  FOREIGN KEY (app_user_id) REFERENCES AppUsers(app_user_id)
);

-- Project_members table
CREATE TABLE project_members (
  project_member_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  project_id INT,
  app_user_id INT,
  role VARCHAR(50),
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (project_id) REFERENCES projects(project_id),
  FOREIGN KEY (app_user_id) REFERENCES AppUsers(app_user_id)
);

-- Documents table
CREATE TABLE documents (
  document_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  project_id INT,
  document_name VARCHAR(255) NOT NULL,
  file_path VARCHAR(255) NOT NULL,
  uploaded_by INT,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (project_id) REFERENCES projects(project_id),
  FOREIGN KEY (uploaded_by) REFERENCES AppUsers(app_user_id)
);

-- Financial_transactions table
CREATE TABLE financial_transactions (
  transaction_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  project_id INT,
  amount DECIMAL(10, 2) NOT NULL,
  transaction_type ENUM('Income', 'Expense') NOT NULL,
  description TEXT,
  transaction_date DATE,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (project_id) REFERENCES projects(project_id)
);

-- Contacts table
CREATE TABLE contacts (
  contact_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  email VARCHAR(255),
  phone VARCHAR(50),
  company VARCHAR(255),
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1
);

-- Sales_opportunities table
CREATE TABLE sales_opportunities (
  opportunity_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  contact_id INT,
  project_id INT,
  opportunity_name VARCHAR(255) NOT NULL,
  stage VARCHAR(50),
  amount DECIMAL(10, 2),
  close_date DATE,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (contact_id) REFERENCES contacts(contact_id),
  FOREIGN KEY (project_id) REFERENCES projects(project_id)
);

-- Marketing_campaigns table
CREATE TABLE marketing_campaigns (
  campaign_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  campaign_name VARCHAR(255) NOT NULL,
  description TEXT,
  start_date DATE,
  end_date DATE,
  budget DECIMAL(10, 2),
  status VARCHAR(50),
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1
);

-- Custom_kpis table
CREATE TABLE custom_kpis (
  kpi_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  kpi_name VARCHAR(255) NOT NULL,
  description TEXT,
  measurement_unit VARCHAR(50),
  target_value DECIMAL(10, 2),
  current_value DECIMAL(10, 2),
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1
);

-- Custom_dashboards table
CREATE TABLE custom_dashboards (
  dashboard_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  app_user_id INT,
  dashboard_name VARCHAR(255) NOT NULL,
  layout JSON,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (app_user_id) REFERENCES AppUsers(app_user_id)
);

-- Workflows table
CREATE TABLE workflows (
  workflow_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  workflow_name VARCHAR(255) NOT NULL,
  description TEXT,
  node_red_flow JSON,
  created_by INT,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT 1,
  FOREIGN KEY (created_by) REFERENCES AppUsers(app_user_id)
);

-- Indexes
CREATE INDEX idx_projects_status ON projects(status);
CREATE INDEX idx_tasks_status ON tasks(status);
CREATE INDEX idx_tasks_priority ON tasks(priority);
CREATE INDEX idx_tasks_due_date ON tasks(due_date);
CREATE INDEX idx_financial_transactions_date ON financial_transactions(transaction_date);
CREATE INDEX idx_sales_opportunities_stage ON sales_opportunities(stage);
CREATE INDEX idx_marketing_campaigns_status ON marketing_campaigns(status);

-- Foreign key constraints
ALTER TABLE tasks
ADD CONSTRAINT fk_tasks_projects
FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE;

ALTER TABLE project_members
ADD CONSTRAINT fk_project_members_projects
FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE;

ALTER TABLE documents
ADD CONSTRAINT fk_documents_projects
FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE;

ALTER TABLE financial_transactions
ADD CONSTRAINT fk_financial_transactions_projects
FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE;

ALTER TABLE sales_opportunities
ADD CONSTRAINT fk_sales_opportunities_contacts
FOREIGN KEY (contact_id) REFERENCES contacts(contact_id) ON DELETE SET NULL;

ALTER TABLE sales_opportunities
ADD CONSTRAINT fk_sales_opportunities_projects
FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE SET NULL;