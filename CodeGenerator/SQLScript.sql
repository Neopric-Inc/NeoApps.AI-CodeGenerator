CREATE TABLE AccountTypes (
  account_type_id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  account_type_name varchar(50) NOT NULL
);

CREATE TABLE CategoryTypes (
  category_type_id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  category_type_name varchar(50) NOT NULL
);

CREATE TABLE Accounts (
  account_id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  account_name varchar(100) NOT NULL,
  account_type_id int NOT NULL,
  balance decimal(15,2) NOT NULL,
  created_at timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (account_type_id) REFERENCES AccountTypes(account_type_id)
);

CREATE TABLE Categories (
  category_id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  category_name varchar(100) NOT NULL,
  category_type_id int NOT NULL,
  created_at timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (category_type_id) REFERENCES CategoryTypes(category_type_id)
);

CREATE TABLE Transactions (
  transaction_id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
  account_id int NOT NULL,
  category_id int NOT NULL,
  amount decimal(15,2) NOT NULL,
  transaction_date date NOT NULL,
  transaction_type_id int NOT NULL,
  description varchar(255) DEFAULT NULL,
  created_at timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (account_id) REFERENCES Accounts(account_id),
  FOREIGN KEY (category_id) REFERENCES Categories(category_id)
);