CREATE TABLE users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE tasks (
    task_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    project_id INT,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    estimated_duration FLOAT,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (project_id) REFERENCES projects(project_id)
);

CREATE TABLE projects (
    project_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE time_entries (
    entry_id INT AUTO_INCREMENT PRIMARY KEY,
    task_id INT NOT NULL,
    start_time DATETIME NOT NULL,
    end_time DATETIME,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1',
    FOREIGN KEY (task_id) REFERENCES tasks(task_id)
);

CREATE TABLE task_tags (
    tag_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE task_tag_association (
    task_id INT NOT NULL,
    tag_id INT NOT NULL,
    PRIMARY KEY (task_id, tag_id),
    FOREIGN KEY (task_id) REFERENCES tasks(task_id),
    FOREIGN KEY (tag_id) REFERENCES task_tags(tag_id)
);

