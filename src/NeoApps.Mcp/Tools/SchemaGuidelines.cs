namespace NeoApps.Mcp.Tools;

internal static class SchemaGuidelines
{
    public const string Text = """
        NeoApps generates a .NET 10 REST API (one controller per table) and a React admin UI from a MySQL schema.
        Write ONE MySQL 8 script that follows these rules:

        1. Tables only: CREATE TABLE statements (plus optional INSERT seed rows). No CREATE DATABASE / USE,
           no DELIMITER blocks, no stored procedures.
        2. Every table has a single-column INT AUTO_INCREMENT PRIMARY KEY named <singular>_id
           (e.g. task_id in table tasks).
        3. Every table ends with these audit columns, exactly as written:
             createdBy VARCHAR(255) NOT NULL,
             modifiedBy VARCHAR(255) NOT NULL,
             createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
             modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
             isActive TINYINT(1) NOT NULL DEFAULT '1'
           createdBy/modifiedBy hold the workspace owner. The generator adds created_by_user / modified_by_user (who
           really created or changed the row) to every table itself: don't declare them.
        4. Use lowercase snake_case table and column names. Do NOT reuse a table name the generator creates itself:
           roles, users, entities, permissionmatrix (login and role-based access), messagequeue, s3bucket,
           s3bucket_folders, dnd_ui_versions, project_dnd_ui_versions, workflow, workflows, workflows_projects,
           workflow_builds, workflow_runs, workflow_deployments, workflow_triggers, workflow_trigger_conditions,
           ai_files, ai_chunks, ai_conversations, ai_messages, ai_runs (AI assistant), versioninfo (migrations), user_tenants (tenancy), login_attempts, user_security (sign-in protection), number_sequences (numbering), background_jobs, background_job_events (background jobs).
           Don't model chat threads/messages/citations or file text yourself: the built-in assistant provides them.
           For app-level people use e.g. app_users or members; for automations use e.g. automation_runs.
        5. Declare relations with FOREIGN KEY (...) REFERENCES parent(parent_id). Create parent tables before
           the tables that reference them. No trailing comma before the closing parenthesis.
        5b. Column names become C# properties: avoid SQL and C# keywords (order, key, condition, operator, default,
           event, object, string, class...). Use e.g. operator_name, sort_order.
        6. Prefer simple types: INT, DECIMAL(12,2), VARCHAR(n), TEXT, DATE, DATETIME, TINYINT(1).
           Model enums as VARCHAR(50).
        7. Keep it focused: 3-15 tables covering the app's core entities.

        Example:
          CREATE TABLE categories (
            category_id INT AUTO_INCREMENT PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            createdBy VARCHAR(255) NOT NULL,
            modifiedBy VARCHAR(255) NOT NULL,
            createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            isActive TINYINT(1) NOT NULL DEFAULT '1'
          );
          CREATE TABLE tasks (
            task_id INT AUTO_INCREMENT PRIMARY KEY,
            category_id INT,
            title VARCHAR(255) NOT NULL,
            status VARCHAR(50) NOT NULL DEFAULT 'todo',
            due_date DATE,
            createdBy VARCHAR(255) NOT NULL,
            modifiedBy VARCHAR(255) NOT NULL,
            createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            modifiedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            isActive TINYINT(1) NOT NULL DEFAULT '1',
            FOREIGN KEY (category_id) REFERENCES categories(category_id)
          );

        For status/type/category columns, keep them VARCHAR and list their options in the UI config instead (see
        neoapps_ui_config_guide); pass that config as uiConfig to neoapps_create_app together with roles.

        Typical flow: neoapps_check_environment -> neoapps_create_app (schema + uiConfig) -> neoapps_deploy_app
        -> open the web URL and log in with the admin credentials it returns.
        """;
}
