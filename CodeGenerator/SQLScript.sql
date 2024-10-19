CREATE TABLE Appusers (
  app_user_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);
CREATE TABLE expense_categories (
  expense_category_id INT PRIMARY KEY AUTO_INCREMENT,
  category_name VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);

CREATE TABLE group_table (
  group_id INT PRIMARY KEY AUTO_INCREMENT,
  group_name VARCHAR(255),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL
);
CREATE TABLE group_balances (
  balance_id INT PRIMARY KEY AUTO_INCREMENT,
  group_id INT,
  app_user_id INT,
  balance_amount FLOAT,
  FOREIGN KEY (group_id) REFERENCES group_table(group_id),
  FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL
);
CREATE TABLE currencies (
  currency_id INT PRIMARY KEY AUTO_INCREMENT,
  currency_name VARCHAR(255),
  currency_code VARCHAR(10),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);

CREATE TABLE debt_calculations (
  debt_calculation_id INT PRIMARY KEY AUTO_INCREMENT,
  group_id INT,
  app_user_id INT,
  receiver_id INT,
  amount FLOAT,
  currency_id INT,
  is_partial INT,
  date DATE,
  FOREIGN KEY (group_id) REFERENCES group_table(group_id),
  FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);
CREATE TABLE expense_split_rules (
  rule_id INT PRIMARY KEY AUTO_INCREMENT,
  group_id INT,
  split_type VARCHAR(255),
  details TEXT,
  FOREIGN KEY (group_id) REFERENCES group_table(group_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL
);
CREATE TABLE expenses (
  expense_id INT PRIMARY KEY AUTO_INCREMENT,
  group_id INT,
  app_user_id INT,
  amount FLOAT,
  currency_id INT,
  expense_category_id INT,
  date DATE,
  FOREIGN KEY (group_id) REFERENCES group_table(group_id),
  FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  FOREIGN KEY (expense_category_id) REFERENCES expense_categories(expense_category_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);
CREATE TABLE group_memberships (
  membership_id INT PRIMARY KEY AUTO_INCREMENT,
  group_id INT,
  app_user_id INT,
  FOREIGN KEY (group_id) REFERENCES group_table(group_id),
  FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
  isActive tinyint(1) NOT NULL DEFAULT '1',
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL

);

CREATE TABLE reminders (
  reminder_id INT PRIMARY KEY AUTO_INCREMENT,
  app_user_id INT,
  reminder_frequency VARCHAR(255),
  last_reminder_date DATE,
  FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1'

);

CREATE TABLE user_preferences (
  user_preference_id INT PRIMARY KEY AUTO_INCREMENT,
  app_user_id INT,
  currency_id INT,
  reminder_frequency VARCHAR(255),
  FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
  FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
  createdBy VARCHAR(255) NOT NULL,
  modifiedBy VARCHAR(255) NOT NULL,
  createdAt datetime NOT NULL,
  modifiedAt datetime NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1'

);