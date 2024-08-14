CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE bills (
    bill_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    category_id INT NOT NULL,
    currency_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    amount FLOAT NOT NULL,
    due_date DATE NOT NULL,
    frequency_id INT NOT NULL,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (category_id) REFERENCES categories(category_id),
    FOREIGN KEY (currency_id) REFERENCES currencies(currency_id),
    FOREIGN KEY (frequency_id) REFERENCES bill_frequencies(frequency_id)
);

CREATE TABLE categories (
    category_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE currencies (
    currency_id INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(3) NOT NULL UNIQUE,
    name VARCHAR(50) NOT NULL,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE bill_frequencies (
    frequency_id INT AUTO_INCREMENT PRIMARY KEY,
    frequency VARCHAR(50) NOT NULL UNIQUE,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE notifications (
    notification_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    bill_id INT NOT NULL,
    notification_date DATETIME NOT NULL,
    type VARCHAR(255) NOT NULL,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (bill_id) REFERENCES bills(bill_id)
);
