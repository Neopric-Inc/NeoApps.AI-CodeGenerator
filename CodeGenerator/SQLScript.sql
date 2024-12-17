CREATE TABLE user_accounts(
  user_id INT AUTO_INCREMENT PRIMARY KEY, 
  email VARCHAR(255) UNIQUE NOT NULL, 
  password_hash VARCHAR(255) NOT NULL, 
  role VARCHAR(50) NOT NULL, 
  full_name VARCHAR(255) NOT NULL, 
  phone_number VARCHAR(20), 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL
);
-- Table: folders
CREATE TABLE folders(
  folder_id INT AUTO_INCREMENT PRIMARY KEY, 
  folder_name VARCHAR(255) NOT NULL, 
  parent_folder_id INT, 
  created_by INT NOT NULL, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(created_by) REFERENCES user_accounts(user_id) ON DELETE CASCADE
);
-- Table: documents
CREATE TABLE documents(
  document_id INT AUTO_INCREMENT PRIMARY KEY, 
  file_name VARCHAR(255) NOT NULL, 
  file_path VARCHAR(255) NOT NULL, 
  folder_id INT, 
  uploaded_by INT, 
  VERSION INT DEFAULT 1, 
  is_archived BOOLEAN DEFAULT FALSE, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(folder_id) REFERENCES folders(folder_id) ON DELETE 
  SET 
    NULL, 
    FOREIGN KEY(uploaded_by) REFERENCES user_accounts(user_id) ON DELETE CASCADE
);
-- Table: document_metadata
CREATE TABLE document_metadata(
  metadata_id INT AUTO_INCREMENT PRIMARY KEY, 
  document_id INT NOT NULL, 
  meta_key VARCHAR(255) NOT NULL, 
  meta_value TEXT, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(document_id) REFERENCES documents(document_id) ON DELETE CASCADE
);
-- Table: activity_logs
CREATE TABLE activity_logs(
  log_id INT AUTO_INCREMENT PRIMARY KEY, 
  user_id INT, 
  activity_type VARCHAR(100) NOT NULL, 
  document_id INT, 
  folder_id INT, 
  details TEXT, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(user_id) REFERENCES user_accounts(user_id) ON DELETE CASCADE, 
  FOREIGN KEY(document_id) REFERENCES documents(document_id) ON DELETE CASCADE, 
  FOREIGN KEY(folder_id) REFERENCES folders(folder_id) ON DELETE CASCADE
);
-- Table: comments
CREATE TABLE comments(
  comment_id INT AUTO_INCREMENT PRIMARY KEY, 
  document_id INT, 
  user_id INT, 
  comment_text TEXT NOT NULL, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(document_id) REFERENCES documents(document_id) ON DELETE CASCADE, 
  FOREIGN KEY(user_id) REFERENCES user_accounts(user_id) ON DELETE CASCADE
);
-- Table: tags
CREATE TABLE tags(
  tag_id INT AUTO_INCREMENT PRIMARY KEY, 
  tag_name VARCHAR(100) NOT NULL, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL
);
-- Table: document_tags
CREATE TABLE document_tags(
  document_id INT, 
  tag_id INT, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  PRIMARY KEY(document_id, tag_id), 
  FOREIGN KEY(document_id) REFERENCES documents(document_id) ON DELETE CASCADE, 
  FOREIGN KEY(tag_id) REFERENCES tags(tag_id) ON DELETE CASCADE
);
-- Table: tasks
CREATE TABLE tasks(
  task_id INT AUTO_INCREMENT PRIMARY KEY, 
  document_id INT, 
  assigned_to INT, 
  task_description TEXT NOT NULL, 
  STATUS VARCHAR (50) DEFAULT 'pending', 
  due_date DATETIME, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(document_id) REFERENCES documents(document_id) ON DELETE CASCADE, 
  FOREIGN KEY(assigned_to) REFERENCES user_accounts(user_id) ON DELETE CASCADE
);
-- Table: checklists
CREATE TABLE checklists(
  checklist_id INT AUTO_INCREMENT PRIMARY KEY, 
  dataroom_id INT, 
  checklist_name VARCHAR(255) NOT NULL, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL
);
-- Table: checklist_sections
CREATE TABLE checklist_sections(
  section_id INT AUTO_INCREMENT PRIMARY KEY, 
  checklist_id INT, 
  section_name VARCHAR(255) NOT NULL, 
  POSITION INT NOT NULL, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(checklist_id) REFERENCES checklists(checklist_id) ON DELETE CASCADE
);
-- Table: checklist_items
CREATE TABLE checklist_items(
  item_id INT AUTO_INCREMENT PRIMARY KEY, 
  section_id INT, 
  item_name VARCHAR(255) NOT NULL, 
  description TEXT, 
  STATUS VARCHAR (50) DEFAULT 'pending', 
  linked_document_id INT, 
  due_date DATETIME, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(section_id) REFERENCES checklist_sections(section_id) ON DELETE CASCADE, 
  FOREIGN KEY(linked_document_id) REFERENCES documents(document_id) ON DELETE 
  SET 
    NULL
);
-- Table: checklist_comments
CREATE TABLE checklist_comments(
  comment_id INT AUTO_INCREMENT PRIMARY KEY, 
  item_id INT, 
  user_id INT, 
  comment_text TEXT NOT NULL, 
  isActive TINYINT(1) NOT NULL, 
  createdBy VARCHAR(255) NOT NULL, 
  modifiedBy VARCHAR(255) NOT NULL, 
  createdAt DATETIME NOT NULL, 
  modifiedAt DATETIME NOT NULL, 
  FOREIGN KEY(item_id) REFERENCES checklist_items(item_id) ON DELETE CASCADE, 
  FOREIGN KEY(user_id) REFERENCES user_accounts(user_id) ON DELETE CASCADE
);
