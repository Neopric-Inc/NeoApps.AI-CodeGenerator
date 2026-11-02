CREATE TABLE expense_groups (
  expense_group_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(150) NOT NULL,
  group_type VARCHAR(20) NOT NULL DEFAULT 'other',
  default_currency VARCHAR(3) NOT NULL DEFAULT 'INR',
  description VARCHAR(500) NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE members (
  member_id INT AUTO_INCREMENT PRIMARY KEY,
  full_name VARCHAR(150) NOT NULL,
  login_username VARCHAR(255) NULL,
  email VARCHAR(255) NULL,
  phone VARCHAR(30) NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE group_members (
  group_member_id INT AUTO_INCREMENT PRIMARY KEY,
  expense_group_id INT NOT NULL,
  member_id INT NOT NULL,
  joined_on DATE NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (expense_group_id) REFERENCES expense_groups(expense_group_id),
  FOREIGN KEY (member_id) REFERENCES members(member_id)
);

CREATE TABLE expenses (
  expense_id INT AUTO_INCREMENT PRIMARY KEY,
  expense_group_id INT NOT NULL,
  description VARCHAR(255) NOT NULL,
  amount DECIMAL(12,2) NOT NULL,
  currency VARCHAR(3) NOT NULL DEFAULT 'INR',
  expense_date DATE NOT NULL,
  category VARCHAR(20) NOT NULL DEFAULT 'other',
  paid_by_member_id INT NOT NULL,
  split_type VARCHAR(20) NOT NULL DEFAULT 'equal',
  split_status VARCHAR(20) NOT NULL DEFAULT 'not_split',
  notes VARCHAR(1000) NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (expense_group_id) REFERENCES expense_groups(expense_group_id),
  FOREIGN KEY (paid_by_member_id) REFERENCES members(member_id)
);

CREATE TABLE expense_splits (
  expense_split_id INT AUTO_INCREMENT PRIMARY KEY,
  expense_id INT NOT NULL,
  member_id INT NOT NULL,
  share_value DECIMAL(12,2) NULL,
  owed_amount DECIMAL(12,2) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'unsettled',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (expense_id) REFERENCES expenses(expense_id),
  FOREIGN KEY (member_id) REFERENCES members(member_id)
);

CREATE TABLE settlements (
  settlement_id INT AUTO_INCREMENT PRIMARY KEY,
  expense_group_id INT NOT NULL,
  from_member_id INT NOT NULL,
  to_member_id INT NOT NULL,
  amount DECIMAL(12,2) NOT NULL,
  currency VARCHAR(3) NOT NULL DEFAULT 'INR',
  settlement_date DATE NOT NULL,
  method VARCHAR(20) NOT NULL DEFAULT 'upi',
  status VARCHAR(20) NOT NULL DEFAULT 'completed',
  notes VARCHAR(500) NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (expense_group_id) REFERENCES expense_groups(expense_group_id),
  FOREIGN KEY (from_member_id) REFERENCES members(member_id),
  FOREIGN KEY (to_member_id) REFERENCES members(member_id)
);

CREATE TABLE expense_comments (
  expense_comment_id INT AUTO_INCREMENT PRIMARY KEY,
  expense_id INT NOT NULL,
  member_id INT NULL,
  comment_text VARCHAR(1000) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive TINYINT(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (expense_id) REFERENCES expenses(expense_id),
  FOREIGN KEY (member_id) REFERENCES members(member_id)
);
