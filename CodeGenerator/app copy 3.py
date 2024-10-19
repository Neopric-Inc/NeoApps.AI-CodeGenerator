import streamlit as st
import subprocess
import os
import logging
import mysql.connector
from mysql.connector import Error
import asyncio

# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger(__name__)

def create_database_if_not_exists(host, user, password, database):
    try:
        connection = mysql.connector.connect(
            host=host,
            user=user,
            password=password
        )
        if connection.is_connected():
            cursor = connection.cursor()
            cursor.execute(f"CREATE DATABASE IF NOT EXISTS `{database}`")
            st.success(f"Database '{database}' created or already exists.")
            cursor.close()
            connection.close()
    except Error as e:
        st.error(f"Error while creating database: {e}")

def connect_to_database(host, user, password, database):
    try:
        connection = mysql.connector.connect(
            host=host,
            user=user,
            password=password,
            database=database
        )
        if connection.is_connected():
            return connection
    except Error as e:
        st.error(f"Error while connecting to MySQL: {e}")
    return None

def drop_all_tables(connection):
    try:
        cursor = connection.cursor()
        cursor.execute("SET FOREIGN_KEY_CHECKS = 0")
        cursor.execute("SHOW TABLES")
        tables = cursor.fetchall()
        for table in tables:
            table_name = table[0]
            cursor.execute(f"DROP TABLE IF EXISTS `{table_name}`")
        cursor.execute("SET FOREIGN_KEY_CHECKS = 1")
        connection.commit()
        st.success("All tables dropped successfully.")
    except Error as e:
        st.error(f"Error while dropping tables: {e}")
    finally:
        if connection.is_connected():
            cursor.close()
            connection.close()

def modify_file(file_path, old_string, new_string):
    try:
        with open(file_path, 'r') as file:
            file_content = file.read()
        
        modified_content = file_content.replace(old_string, new_string)
        
        with open(file_path, 'w') as file:
            file.write(modified_content)
        
        logger.info(f"Successfully modified {file_path}")
    except Exception as e:
        logger.error(f"Error modifying {file_path}: {str(e)}")
        st.error(f"Error modifying {file_path}: {str(e)}")

def run_docker_command(command):
    try:
        result = subprocess.run(command, shell=True, capture_output=True, text=True, check=True)
        st.success(f"Docker command executed successfully: {command}")
        st.code(result.stdout)
        logger.info(f"Docker command executed: {command}")
        logger.debug(f"Docker command output: {result.stdout}")
    except subprocess.CalledProcessError as e:
        st.error(f"Error executing Docker command: {command}")
        st.code(e.stderr)
        logger.error(f"Error executing Docker command: {command}")
        logger.error(f"Error output: {e.stderr}")

def build_and_run_backend(project_name):
    # Build and run Backend API
    modify_file('Dockerfile', '{projectName}', project_name)
    run_docker_command(f"docker build -t backend-api -f Dockerfile .")
    run_docker_command(f"docker run -d --name backend-api -p 5001:5001 -e projectName={project_name} backend-api")
    
    # Build and run Backend Consumer
    modify_file('Dockerfile-Consumer', '{projectName}', project_name)
    run_docker_command(f"docker build -t backend-consumer -f Dockerfile-Consumer .")
    run_docker_command(f"docker run -d --name backend-consumer -p 8080:80 backend-consumer")

def build_frontend(is_dnd, current_dir):
    dnd_value = "1" if is_dnd else "0"
    image_name = f"frontend-dnd{dnd_value}"
    container_name = f"frontend-dnd{dnd_value}"
    port = "3001" if is_dnd else "3000"

    env_file = os.path.join(current_dir, '.env')
    if os.path.exists(env_file):
        # Modify .env file
        modify_file(env_file, f'REACT_APP_IS_DND_ON=.*', f'REACT_APP_IS_DND_ON="{dnd_value}"')
    else:
        logger.error(f".env file not found in {current_dir}")
        st.error(f".env file not found in {current_dir}")
        return

    dockerfile_path = os.path.join(current_dir, 'Dockerfile')
    if os.path.exists(dockerfile_path):
        # Build Docker image
        run_docker_command(f"docker build -t {image_name} -f {dockerfile_path} {current_dir}")
        
        # Run Docker container
        if is_dnd:
            run_docker_command(f"docker run -d --name {container_name} -e REACT_APP_IS_DND_ON={dnd_value} -p {port}:3000 {image_name}")
        else:
            run_docker_command(f"docker run -d --name {container_name} -p {port}:3000 {image_name}")
        
        st.success(f"Frontend Docker image {image_name} built and container {container_name} started on port {port}")
    else:
        logger.error(f"Dockerfile not found in {current_dir}")
        st.error(f"Dockerfile not found in {current_dir}")

def build_and_run_node_red():
    run_docker_command("docker build -t node-red -f dockerfile-node .")
    run_docker_command("docker run -d --name node-red -p 1880:1880 node-red")

def check_container_health(container_name):
    try:
        result = subprocess.run(f"docker inspect --format='{{{{.State.Health.Status}}}}' {container_name}",
                                shell=True, capture_output=True, text=True, check=True)
        health_status = result.stdout.strip()
        if health_status == "healthy":
            st.success(f"Container {container_name} is healthy")
        else:
            st.warning(f"Container {container_name} health status: {health_status}")
    except subprocess.CalledProcessError as e:
        st.error(f"Error checking health of container {container_name}: {e}")

def cleanup_docker_resources():
    containers = ["backend-api", "backend-consumer", "node-red"]
    images = ["backend-api", "backend-consumer", "frontend-dnd0", "frontend-dnd1", "node-red"]
    
    for container in containers:
        try:
            subprocess.run(f"docker stop {container}", shell=True, check=True)
            subprocess.run(f"docker rm {container}", shell=True, check=True)
            st.success(f"Stopped and removed container: {container}")
        except subprocess.CalledProcessError as e:
            st.error(f"Error cleaning up container {container}: {e}")
    
    for image in images:
        try:
            subprocess.run(f"docker rmi {image}", shell=True, check=True)
            st.success(f"Removed image: {image}")
        except subprocess.CalledProcessError as e:
            st.error(f"Error removing image {image}: {e}")

async def execute_backend_instructions(script_output, project_name, cwd):
    try:
        output_path = os.path.dirname(script_output.strip().split('\n')[-1])
        logger.debug(f"Extracted output path: {output_path}")

        if not os.path.exists(output_path):
            logger.error(f"Directory does not exist: {output_path}")
            st.error(f"Directory does not exist: {output_path}")
            return

        os.chdir(output_path)
        logger.debug(f"Changed working directory to: {os.getcwd()}")
        logger.debug(f"Directory contents: {os.listdir()}")

        if 'solution' in os.listdir():
            os.chdir('solution')
            logger.debug(f"Changed working directory to: {os.getcwd()}")
            logger.debug(f"Solution directory contents: {os.listdir()}")

            if project_name in os.listdir():
                os.chdir(project_name)
                logger.debug(f"Changed working directory to: {os.getcwd()}")
                logger.debug(f"Project directory contents: {os.listdir()}") 

                if build_docker:
                    build_and_run_backend(project_name)
                    check_container_health("backend-api")
                    check_container_health("backend-consumer")

                api_folder = f"{project_name}.API"
                if api_folder in os.listdir():
                    os.chdir(api_folder)
                    logger.debug(f"Changed working directory to: {os.getcwd()}")
                    logger.debug(f"API directory contents: {os.listdir()}")

                    st.success(f"Backend setup completed successfully. Working directory: {os.getcwd()}")
                    logger.info(f"Backend setup completed. Working directory: {os.getcwd()}")
                else:
                    logger.error(f"No {api_folder} folder found in {os.getcwd()}")
                    st.error(f"No {api_folder} folder found in {os.getcwd()}")
            else:
                logger.error(f"No {project_name} folder found in {os.getcwd()}")
                st.error(f"No {project_name} folder found in {os.getcwd()}")
        else:
            logger.error(f"No 'solution' folder found in {os.getcwd()}")
            st.error(f"No 'solution' folder found in {os.getcwd()}")

    except Exception as e:
        logger.error(f"An error occurred while executing commands: {str(e)}")
        st.error(f"An error occurred while executing commands: {str(e)}")
    finally:
        os.chdir(cwd)
        logger.debug(f"Returned to original directory: {os.getcwd()}")

async def execute_frontend_instructions(script_output, project_name, cwd):
    try:
        logger.debug(f"Starting directory (CodeGenerator): {cwd}")

        # Move up to NeoApps.AI-CodeGenerator
        parent_dir = os.path.dirname(cwd)
        logger.debug(f"Parent directory (NeoApps.AI-CodeGenerator): {parent_dir}")

        # Move into the project-specific folder (starts with a number)
        project_root = next(d for d in os.listdir(parent_dir) if d.split('_')[0].isdigit())
        project_path = os.path.join(parent_dir, project_root)
        logger.debug(f"Project root path: {project_path}")

        if not os.path.isdir(project_path):
            logger.error(f"Project directory not found: {project_path}")
            st.error(f"Project directory not found: {project_path}")
            return

        # Move into the project-specific directory (using project_name)
        project_specific_path = os.path.join(project_path, project_name)
        if not os.path.isdir(project_specific_path):
            logger.error(f"{project_name} directory not found in {project_path}")
            st.error(f"{project_name} directory not found in {project_path}")
            return

        # Move into the ReactTs_Output1 directory
        reactts_path = os.path.join(project_specific_path, "ReactTs_Output1")
        if not os.path.isdir(reactts_path):
            logger.error(f"ReactTs_Output1 directory not found in {project_specific_path}")
            st.error(f"ReactTs_Output1 directory not found in {project_specific_path}")
            return

        os.chdir(reactts_path)
        logger.debug(f"Changed working directory to: {os.getcwd()}")
        logger.debug(f"ReactTs_Output1 directory contents: {os.listdir()}")

        st.success(f"Frontend setup completed successfully. Your React app is located at: {os.getcwd()}")
        logger.info(f"Frontend setup completed. Working directory: {os.getcwd()}")

        if build_docker:
            build_frontend(False,os.getcwd())  # Build DND0
            build_frontend(True,os.getcwd())   # Build DND1
            st.success("Frontend Docker images built successfully.")

    except Exception as e:
        logger.error(f"An error occurred while executing commands: {str(e)}")
        st.error(f"An error occurred while executing commands: {str(e)}")
    finally:
        os.chdir(cwd)
        logger.debug(f"Returned to original directory: {os.getcwd()}")
async def execute_nodered_instructions(script_output, project_name, cwd):
    try:
        st.success(f"Node-RED setup completed successfully. Please follow these steps:\n\n"
                   f"1. Ensure Node-RED is installed on your system.\n"
                   f"2. Open a terminal and navigate to your project directory.\n"
                   f"3. Start Node-RED by running `node-red`.\n"
                   f"4. Open a web browser and go to `http://localhost:1880` to access the Node-RED editor.\n"
                   f"5. In the Node-RED editor, you can start creating your flow or import an existing one.\n\n"
                   f"Your project name is: {project_name}")
        
        logger.info(f"User should start Node-RED and access it at http://localhost:1880. Project name: {project_name}")

    except Exception as e:
        logger.error(f"An error occurred while providing Node-RED instructions: {str(e)}")
        st.error(f"An error occurred while providing Node-RED instructions: {str(e)}")
    finally:
        os.chdir(cwd)
        logger.debug(f"Returned to original directory: {os.getcwd()}")

st.title("Project Configuration Form")

# Create tabs for Backend, Frontend, and Node-RED
tab1, tab2, tab3 = st.tabs(["Backend API", "Frontend", "Node-RED"])

with tab1:
    st.header("Backend API Setup")
    with st.form(key='backend_form'):
        project_id = st.text_input("Project ID", "1")
        server = st.text_input("Server", "localhost")
        uid = st.text_input("User ID", "1")
        username = st.text_input("Database Username", "root")
        password = st.text_input("Database Password", "", type="password")
        database_name = st.text_input("Database Name", "myAppDb")
        script = st.text_input("Script URL", "http://localhost/app.sql")
        status_of_generation = st.text_input("Status of Generation", "null")
        project_name = st.text_input("Project Name", "ContentPlannerTest")
        db_exists = st.selectbox("DB Exists", ["No", "Yes"], index=0)
        port = st.text_input("Port", "3306")
        rabbitmq_conn = st.text_input("RabbitMQ Connection URL", "amqp://user:password@localhost:5672/")
        redis_conn = st.text_input("Redis Connection String", "localhost:6379")
        redis_password = st.text_input("Redis Password", "yourredispassword")
        apiflowurl = st.text_input("API Flow URL", "")
        fronttemplateurl = st.text_input("Frontend Template URL", "")
        technology_frontend = st.text_input("Frontend Technology", "")
        backend_technology = st.text_input("Backend Technology", "dotnet")
        button_clicked = st.text_input("Button Clicked", "generate")
        project_type = st.text_input("Project Type", "")
        swgurl = st.text_input("Swagger URL", "")
        noderedurl = st.text_input("Node-RED URL", "null")

        create_db = st.checkbox("Create database if not exists")
        drop_tables = st.checkbox("Drop all tables in the database")
        build_docker = st.checkbox("Build and run Docker containers for Backend")

        submit_backend_button = st.form_submit_button(label='Submit Backend')

    if submit_backend_button:
        formatted_parameters = f"{{project_id:{project_id},server:{server},uid:{uid},username:{username},password:{password},databaseName:{database_name},script:{script},statusOfGeneration:{status_of_generation},projectName:{project_name},DBexists:{db_exists},port:{port},rabbitMQConn:{rabbitmq_conn},redisConn:{redis_conn},password={redis_password},apiflowurl:{apiflowurl},fronttemplateurl:{fronttemplateurl},Technology_Frontend:{technology_frontend},Backend_technology:{backend_technology},buttonClicked:{button_clicked},projectType:{project_type},swgurl:{swgurl},noderedurl:{noderedurl}}}"

        st.write("### Submitted Parameters for Backend")
        st.code(f'"PARAMETER": "{formatted_parameters}"', language="json")

        if create_db:
            create_database_if_not_exists(server, username, password, database_name)

        if drop_tables:
            connection = connect_to_database(server, username, password, database_name)
            if connection:
                drop_all_tables(connection)

        script_path = os.path.join(os.path.dirname(__file__), 'form_submission_script.sh')

        logger.debug(f"Script path: {script_path}")
        logger.debug(f"Current working directory: {os.getcwd()}")

        if os.path.exists(script_path):
            logger.debug("Script file exists.")
        else:
            logger.error(f"Script file not found at {script_path}")
            st.error(f"Script file not found at {script_path}")

        try:
            command = f"{script_path} '{formatted_parameters}'"
            result = subprocess.run(command, shell=True, capture_output=True, text=True, check=True)
            st.success("Form submitted and script executed successfully!")
            st.text("Script output:")
            st.code(result.stdout)

            asyncio.run(execute_backend_instructions(result.stdout, project_name, os.getcwd()))

            
        except subprocess.CalledProcessError as e:
            logger.error(f"An error occurred while executing the script: {e}")
            st.error(f"An error occurred while executing the script: {e}")
            st.text("Script output:")
            st.code(e.stdout)
            st.text("Error output:")
            st.code(e.stderr)
        except Exception as e:
            logger.error(f"An unexpected error occurred: {str(e)}")
            st.error(f"An unexpected error occurred: {str(e)}")

with tab2:
    st.header("Frontend Setup")
    with st.form(key='frontend_form'):
        project_id = st.text_input("Project ID", "1", key='frontend_project_id')
        server = st.text_input("Server", "localhost", key='frontend_server')
        uid = st.text_input("User ID", "1", key='frontend_uid')
        username = st.text_input("Database Username", "root", key='frontend_username')
        password = st.text_input("Database Password", "", type="password", key='frontend_password')
        database_name = st.text_input("Database Name", "myAppDb", key='frontend_database_name')
        script = st.text_input("Script URL", "http://localhost/app.sql", key='frontend_script')
        status_of_generation = st.text_input("Status of Generation", "null", key='frontend_status_of_generation')
        project_name = st.text_input("Project Name", "ContentPlannerTest", key='frontend_project_name')
        db_exists = st.selectbox("DB Exists", ["No", "Yes"], index=1, key='frontend_db_exists')
        port = st.text_input("Port", "3306", key='frontend_port')
        rabbitmq_conn = st.text_input("API Connection URL", "https://localhost:5001/v1/api/", key='frontend_rabbitmq_conn')
        redis_conn = st.text_input("Redis Connection String", "localhost:6379", key='frontend_redis_conn')
        redis_password = st.text_input("Redis Password", "12345", key='frontend_redis_password')
        apiflowurl = st.text_input("API Flow URL", "", key='frontend_apiflowurl')
        fronttemplateurl = st.text_input("Frontend Template URL", "", key='frontend_fronttemplateurl')
        technology_frontend = st.text_input("Frontend Technology", "reactts", key='frontend_technology_frontend')
        backend_technology = st.text_input("Backend Technology", "", key='frontend_backend_technology')
        button_clicked = st.text_input("Button Clicked", "generate", key='frontend_button_clicked')
        project_type = st.text_input("Project Type", "dnd", key='frontend_project_type')
        swgurl = st.text_input("Swagger URL", "", key='frontend_swgurl')
        noderedurl = st.text_input("Node-RED URL", "https://localhost:5001/v1/api/", key='frontend_noderedurl')

        create_db = st.checkbox("Create database if not exists", key='frontend_create_db')
        drop_tables = st.checkbox("Drop all tables in the database", key='frontend_drop_tables')
        build_docker = st.checkbox("Build Docker images for Frontend")

        submit_frontend_button = st.form_submit_button(label='Submit Frontend')

    if submit_frontend_button:
        formatted_parameters = f"{{project_id:{project_id},server:{server},uid:{uid},username:{username},password:{password},databaseName:{database_name},script:{script},statusOfGeneration:{status_of_generation},projectName:{project_name},DBexists:{db_exists},port:{port},rabbitMQConn:{rabbitmq_conn},redisConn:{redis_conn},password={redis_password},apiflowurl:{apiflowurl},fronttemplateurl:{fronttemplateurl},Technology_Frontend:{technology_frontend},Backend_technology:{backend_technology},buttonClicked:{button_clicked},projectType:{project_type},swgurl:{swgurl},noderedurl:{noderedurl}}}"

        st.write("### Submitted Parameters for Frontend")
        st.code(f'"PARAMETER": "{formatted_parameters}"', language="json")

        if create_db:
            create_database_if_not_exists(server, username, password, database_name)

        if drop_tables:
            connection = connect_to_database(server, username, password, database_name)
            if connection:
                drop_all_tables(connection)

        script_path = os.path.join(os.path.dirname(__file__), 'form_submission_script.sh')

        logger.debug(f"Script path: {script_path}")
        logger.debug(f"Current working directory: {os.getcwd()}")

        if os.path.exists(script_path):
            logger.debug("Script file exists.")
        else:
            logger.error(f"Script file not found at {script_path}")
            st.error(f"Script file not found at {script_path}")

        try:
            command = f"{script_path} '{formatted_parameters}'"
            result = subprocess.run(command, shell=True, capture_output=True, text=True, check=True)
            st.success("Form submitted and script executed successfully!")
            st.text("Script output:")
            st.code(result.stdout)

            asyncio.run(execute_frontend_instructions(result.stdout, project_name, os.getcwd()))

            

        except subprocess.CalledProcessError as e:
            logger.error(f"An error occurred while executing the script: {e}")
            st.error(f"An error occurred while executing the script: {e}")
            st.text("Script output:")
            st.code(e.stdout)
            st.text("Error output:")
            st.code(e.stderr)
        except Exception as e:
            logger.error(f"An unexpected error occurred: {str(e)}")
            st.error(f"An unexpected error occurred: {str(e)}")

with tab3:
    st.header("Node-RED Setup")
    with st.form(key='nodered_form'):
        project_id = st.text_input("Project ID", "1", key='nodered_project_id')
        server = st.text_input("Server", "localhost", key='nodered_server')
        uid = st.text_input("User ID", "1", key='nodered_uid')
        username = st.text_input("Database Username", "root", key='nodered_username')
        password = st.text_input("Database Password", "", type="password", key='nodered_password')
        database_name = st.text_input("Database Name", "myAppDb", key='nodered_database_name')
        script = st.text_input("Script URL", "http://localhost/app.sql", key='nodered_script')
        status_of_generation = st.text_input("Status of Generation", "null", key='nodered_status_of_generation')
        project_name = st.text_input("Project Name", "ContentPlannerTest", key='nodered_project_name')
        db_exists = st.selectbox("DB Exists", ["No", "Yes"], index=1, key='nodered_db_exists')
        port = st.text_input("Port", "3306", key='nodered_port')
        rabbitmq_conn = st.text_input("API Connection URL", "https://localhost:5001/v1/api/", key='nodered_rabbitmq_conn')
        redis_conn = st.text_input("Redis Connection String", "localhost:6379", key='nodered_redis_conn')
        redis_password = st.text_input("Redis Password", "12345", key='nodered_redis_password')
        apiflowurl = st.text_input("API Flow URL", "", key='nodered_apiflowurl')
        fronttemplateurl = st.text_input("Frontend Template URL", "", key='nodered_fronttemplateurl')
        technology_frontend = st.text_input("Frontend Technology", "reactts", key='nodered_technology_frontend')
        backend_technology = st.text_input("Backend Technology", "", key='nodered_backend_technology')
        button_clicked = st.text_input("Button Clicked", "generate", key='nodered_button_clicked')
        project_type = st.text_input("Project Type", "nodered", key='nodered_project_type')
        swgurl = st.text_input("Swagger URL", "", key='nodered_swgurl')
        noderedurl = st.text_input("Node-RED URL", "https://localhost:5001/v1/api/", key='nodered_noderedurl')

        create_db = st.checkbox("Create database if not exists", key='nodered_create_db')
        drop_tables = st.checkbox("Drop all tables in the database", key='nodered_drop_tables')
        build_docker = st.checkbox("Build and run Docker container for Node-RED")

        submit_nodered_button = st.form_submit_button(label='Submit Node-RED')

    if submit_nodered_button:
        formatted_parameters = f"{{project_id:{project_id},server:{server},uid:{uid},username:{username},password:{password},databaseName:{database_name},script:{script},statusOfGeneration:{status_of_generation},projectName:{project_name},DBexists:{db_exists},port:{port},rabbitMQConn:{rabbitmq_conn},redisConn:{redis_conn},password={redis_password},apiflowurl:{apiflowurl},fronttemplateurl:{fronttemplateurl},Technology_Frontend:{technology_frontend},Backend_technology:{backend_technology},buttonClicked:{button_clicked},projectType:{project_type},swgurl:{swgurl},noderedurl:{noderedurl}}}"

        st.write("### Submitted Parameters for Node-RED")
        st.code(f'"PARAMETER": "{formatted_parameters}"', language="json")

        if create_db:
            create_database_if_not_exists(server, username, password, database_name)

        if drop_tables:
            connection = connect_to_database(server, username, password, database_name)
            if connection:
                drop_all_tables(connection)

        script_path = os.path.join(os.path.dirname(__file__), 'form_submission_script.sh')

        logger.debug(f"Script path: {script_path}")
        logger.debug(f"Current working directory: {os.getcwd()}")

        if os.path.exists(script_path):
            logger.debug("Script file exists.")
        else:
            logger.error(f"Script file not found at {script_path}")
            st.error(f"Script file not found at {script_path}")

        try:
            command = f"{script_path} '{formatted_parameters}'"
            result = subprocess.run(command, shell=True, capture_output=True, text=True, check=True)
            st.success("Form submitted and script executed successfully!")
            st.text("Script output:")
            st.code(result.stdout)

            asyncio.run(execute_nodered_instructions(result.stdout, project_name, os.getcwd()))

            if build_docker:
                build_and_run_node_red()
                check_container_health("node-red")

        except subprocess.CalledProcessError as e:
            logger.error(f"An error occurred while executing the script: {e}")
            st.error(f"An error occurred while executing the script: {e}")
            st.text("Script output:")
            st.code(e.stdout)
            st.text("Error output:")
            st.code(e.stderr)
        except Exception as e:
            logger.error(f"An unexpected error occurred: {str(e)}")
            st.error(f"An unexpected error occurred: {str(e)}")

# Add a cleanup button at the end of your Streamlit app
if st.button("Cleanup Docker Resources"):
    cleanup_docker_resources()