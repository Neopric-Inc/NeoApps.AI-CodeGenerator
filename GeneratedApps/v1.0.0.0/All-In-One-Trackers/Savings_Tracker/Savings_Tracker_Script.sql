CREATE TABLE user_accounts (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    biometric_data TEXT,
    password_reset_token VARCHAR(255),
    password_reset_expires_at DATETIME,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE savings_goals (
    goal_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    goal_name VARCHAR(255) NOT NULL,
    total_goal_amount FLOAT NOT NULL,
    initial_amount FLOAT DEFAULT 0,
    deadline DATE,
    FOREIGN KEY (user_id) REFERENCES user_accounts(user_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE monthly_goals_contributions (
    contribution_id INT AUTO_INCREMENT PRIMARY KEY,
    goal_id INT NOT NULL,
    month_year DATE NOT NULL,
    suggested_saving_amount FLOAT,
    actual_saving_amount FLOAT DEFAULT 0,
    FOREIGN KEY (goal_id) REFERENCES savings_goals(goal_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE contributions (
    contribution_id INT AUTO_INCREMENT PRIMARY KEY,
    goal_id INT NOT NULL,
    contribution_date DATE NOT NULL,
    amount FLOAT NOT NULL,
    FOREIGN KEY (goal_id) REFERENCES savings_goals(goal_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);
