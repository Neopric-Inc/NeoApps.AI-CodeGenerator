CREATE TABLE UserRoles (
  role_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  role_name VARCHAR(50) NOT NULL,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);

CREATE TABLE Appusers (
  app_user_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  role_id INT NOT NULL,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt DATETIME NOT NULL,
  modifiedAt DATETIME NOT NULL,
  isActive tinyint(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (role_id) REFERENCES UserRoles(role_id)
);

CREATE TABLE task_categories (
    category_id INT AUTO_INCREMENT PRIMARY KEY, -- Work, Personal, Home
    name VARCHAR(255) NOT NULL,
    app_user_id INT NOT NULL,
    FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE tasks (
    task_id INT AUTO_INCREMENT PRIMARY KEY,
    app_user_id INT NOT NULL,
    category_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    priority INT,
    deadline DATETIME,
    taskCompleted TINYINT(1) NOT NULL DEFAULT '0',
    FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
    FOREIGN KEY (category_id) REFERENCES task_categories(category_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE task_tags (
    tag_id INT AUTO_INCREMENT PRIMARY KEY, -- Urgent, Important, Meetings, Errands
    name VARCHAR(255) NOT NULL,
    task_id INT NOT NULL,
    FOREIGN KEY (task_id) REFERENCES tasks(task_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE notebooks (
    notebook_id INT AUTO_INCREMENT PRIMARY KEY,
    app_user_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    thumbnail VARCHAR(255),
    FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL, 
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE notes (
    note_id INT AUTO_INCREMENT PRIMARY KEY,
    notebook_id INT,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    FOREIGN KEY (notebook_id) REFERENCES notebooks(notebook_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE note_tags (
    tag_id INT AUTO_INCREMENT PRIMARY KEY,
    note_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    FOREIGN KEY (note_id) REFERENCES notes(note_id), -- ToDo, Important, Ideas
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

CREATE TABLE events (
    event_id INT AUTO_INCREMENT PRIMARY KEY,
    app_user_id INT NOT NULL,
    task_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    start_time DATETIME NOT NULL,
    end_time DATETIME NOT NULL,
    reminder_time DATETIME,
    eventCompleted TINYINT(1) NOT NULL DEFAULT '0',
    FOREIGN KEY (task_id) REFERENCES tasks(task_id),
    FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

-- CREATE TABLE activity_logs (
--     log_id INT AUTO_INCREMENT PRIMARY KEY,
--     app_user_id INT NOT NULL,
--     action VARCHAR(255) NOT NULL,
--     description TEXT NOT NULL,
--     timestamp DATETIME NOT NULL,
--     FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
--     createdBy VARCHAR(255) NOT NULL,
--     modifiedBy VARCHAR(255) NOT NULL,
--     createdAt DATETIME NOT NULL,
--     modifiedAt DATETIME NOT NULL,
--     isActive TINYINT(1) NOT NULL DEFAULT '1'
-- );

CREATE TABLE task_logs (
    log_id INT AUTO_INCREMENT PRIMARY KEY,
    task_id INT NOT NULL,
    app_user_id INT NOT NULL,
    action VARCHAR(50) NOT NULL,
    description VARCHAR(255),
    timestamp DATETIME NOT NULL,
    FOREIGN KEY (task_id) REFERENCES tasks(task_id),
    FOREIGN KEY (app_user_id) REFERENCES Appusers(app_user_id),
    createdBy VARCHAR(255) NOT NULL,
    modifiedBy VARCHAR(255) NOT NULL,
    createdAt DATETIME NOT NULL,
    modifiedAt DATETIME NOT NULL,
    isActive TINYINT(1) NOT NULL DEFAULT '1'
);

DELIMITER $$

CREATE TRIGGER after_task_insert
AFTER INSERT ON tasks
FOR EACH ROW
BEGIN
    INSERT INTO task_logs (task_id, app_user_id, action, description, timestamp, createdBy, modifiedBy, createdAt, modifiedAt, isActive)
    VALUES (NEW.task_id, NEW.app_user_id, 'INSERT', CONCAT('Task created: ', NEW.title), NOW(), 'system', 'system', NOW(), NOW(), 1);
END$$

DELIMITER ;

DELIMITER $$

CREATE TRIGGER update_task_completed
AFTER UPDATE ON events
FOR EACH ROW
BEGIN
    IF NEW.eventCompleted = 1 THEN
        UPDATE tasks
        SET taskCompleted = 1
        WHERE task_id = NEW.task_id;
    END IF;
END$$

DELIMITER ;