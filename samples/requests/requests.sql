-- Requests: employees ask (purchases, time off, travel, equipment), managers approve or reject, every step is logged.
CREATE TABLE departments (
  department_id INT AUTO_INCREMENT PRIMARY KEY,
  department_name VARCHAR(100) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE employees (
  employee_id INT AUTO_INCREMENT PRIMARY KEY,
  full_name VARCHAR(120) NOT NULL,
  email VARCHAR(255) NOT NULL,
  login_username VARCHAR(255) NULL,
  department_id INT NOT NULL,
  FOREIGN KEY (department_id) REFERENCES departments(department_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE requests (
  request_id INT AUTO_INCREMENT PRIMARY KEY,
  request_number VARCHAR(30) NULL,
  title VARCHAR(200) NOT NULL,
  request_type VARCHAR(30) NOT NULL,
  amount DECIMAL(12,2) NULL,
  needed_by DATE NULL,
  details TEXT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'Draft',
  employee_id INT NOT NULL,
  decision_note VARCHAR(500) NULL,
  decided_at DATETIME NULL,
  FOREIGN KEY (employee_id) REFERENCES employees(employee_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE request_status_history (
  request_status_history_id INT AUTO_INCREMENT PRIMARY KEY,
  request_id INT NOT NULL,
  old_status VARCHAR(20) NULL,
  new_status VARCHAR(20) NOT NULL,
  changed_by_type VARCHAR(20) NULL,
  changed_by VARCHAR(255) NULL,
  FOREIGN KEY (request_id) REFERENCES requests(request_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

INSERT INTO departments (department_name, createdBy, modifiedBy) VALUES ('Operations', 'sample', 'sample'), ('Finance', 'sample', 'sample');
INSERT INTO employees (full_name, email, login_username, department_id, createdBy, modifiedBy) VALUES
  ('Emma Clarke', 'emma@example.com', 'emma', 1, 'sample', 'sample'),
  ('Marcus Lee', 'marcus@example.com', 'marcus', 1, 'sample', 'sample'),
  ('Noah Patel', 'noah@example.com', 'noah', 2, 'sample', 'sample');
