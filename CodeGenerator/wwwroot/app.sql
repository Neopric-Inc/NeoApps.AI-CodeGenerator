
CREATE TABLE users (
    user_id INT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    password VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL,
    role VARCHAR(20) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (username),
    UNIQUE (email)
);

CREATE TABLE projects (
    project_id INT PRIMARY KEY,
    project_name VARCHAR(100) NOT NULL,
    description TEXT,
    start_date DATE,
    end_date DATE,
    created_by INT,
    FOREIGN KEY (created_by) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE tasks (
    task_id INT PRIMARY KEY,
    task_name VARCHAR(100) NOT NULL,
    description TEXT,
    priority INT,
    deadline DATE,
    status VARCHAR(20),
    project_id INT,
    assigned_to INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE,
    FOREIGN KEY (assigned_to) REFERENCES users(user_id) ON DELETE SET NULL
);

CREATE TABLE task_dependencies (
    dependency_id INT PRIMARY KEY,
    depends_on INT,
    dependent_task INT,
    FOREIGN KEY (depends_on) REFERENCES tasks(task_id) ON DELETE CASCADE,
    FOREIGN KEY (dependent_task) REFERENCES tasks(task_id) ON DELETE CASCADE
);

CREATE TABLE project_collaborators (
    project_id INT,
    user_id INT,
    PRIMARY KEY (project_id, user_id),
    FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE reports (
    report_id INT PRIMARY KEY,
    project_id INT,
    report_date DATE,
    progress INT,
    time_tracked TIME,
    resource_allocation DECIMAL(10,2),
    FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE
);

CREATE TABLE external_systems (
    system_id INT PRIMARY KEY,
    system_name VARCHAR(100) NOT NULL,
    description TEXT
);

CREATE TABLE integration_mapping (
    project_id INT,
    system_id INT,
    mapping_details TEXT,
    FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE,
    FOREIGN KEY (system_id) REFERENCES external_systems(system_id) ON DELETE CASCADE
);

CREATE TABLE user_subscriptions (
    subscription_id INT PRIMARY KEY,
    user_id INT,
    subscription_type VARCHAR(50),
    subscription_cost DECIMAL(10,2),
    start_date DATE,
    end_date DATE,
    auto_renew BOOLEAN,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE content_modification (
    modification_id INT PRIMARY KEY,
    user_id INT,
    content_type VARCHAR(20),
    content_id INT,
    action VARCHAR(20),
    moderation_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
