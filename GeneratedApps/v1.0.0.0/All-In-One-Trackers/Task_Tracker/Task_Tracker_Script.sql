CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(255) NOT NULL, -- 'end-user' or 'administrator'
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    UNIQUE INDEX email_unique (email)
);

CREATE TABLE tasks (
    task_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    category_id INT,
    urgency VARCHAR(255), -- could be 'urgent', 'not_urgent' etc. Considered a VARCHAR for flexibility
    importance VARCHAR(255), -- similar reasoning as urgency
    status VARCHAR(255) NOT NULL DEFAULT 'pending', -- 'pending', 'completed', etc.
    deadline DATETIME,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (category_id) REFERENCES categories(category_id)
);

CREATE TABLE categories (
    category_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    user_id INT, -- NULL for predefined categories, user ID for custom ones
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    UNIQUE INDEX name_user_unique (name, user_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE TABLE actions (
    action_id INT AUTO_INCREMENT PRIMARY KEY,
    task_id INT NOT NULL,
    action_type VARCHAR(255) NOT NULL, -- 'do_now', 'do_later', 'postpone', etc.
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (task_id) REFERENCES tasks(task_id)
);

