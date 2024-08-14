CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    password VARCHAR(255) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    created_by VARCHAR(255) NOT NULL,
    modified_by VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL,
    modified_at DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    UNIQUE INDEX email_unique (email)
);

CREATE TABLE tasks (
    task_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    description TEXT NOT NULL,
    deadline DATETIME,
    estimated_completion_time INT, -- Stored as minutes
    category_id INT,
    created_by VARCHAR(255) NOT NULL,
    modified_by VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL,
    modified_at DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (category_id) REFERENCES categories(category_id)
);

CREATE TABLE categories (
    category_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    created_by VARCHAR(255) NOT NULL,
    modified_by VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL,
    modified_at DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    UNIQUE INDEX name_unique (name)
);

CREATE TABLE delegations (
    delegation_id INT AUTO_INCREMENT PRIMARY KEY,
    task_id INT NOT NULL,
    delegatee_email VARCHAR(255) NOT NULL,
    reminder_date DATETIME,
    created_by VARCHAR(255) NOT NULL,
    modified_by VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL,
    modified_at DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (task_id) REFERENCES tasks(task_id)
);

CREATE TABLE notifications (
    notification_id INT AUTO_INCREMENT PRIMARY KEY,
    task_id INT NOT NULL,
    notification_time DATETIME NOT NULL,
    message TEXT NOT NULL,
    created_by VARCHAR(255) NOT NULL,
    modified_by VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL,
    modified_at DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (task_id) REFERENCES tasks(task_id)
);

