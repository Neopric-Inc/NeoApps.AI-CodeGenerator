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

async def execute_backend_instructions(script_output, project_name):
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

                api_folder = f"{project_name}.API"
                if api_folder in os.listdir():
                    os.chdir(api_folder)
                    logger.debug(f"Changed working directory to: {os.getcwd()}")
                    logger.debug(f"API directory contents: {os.listdir()}")

                    # Inform user to run `dotnet run` manually
                    st.success(f"Backend setup completed successfully. Please navigate to the following directory in your terminal and run `dotnet run`:\n\n{os.getcwd()}")
                    logger.info(f"User should navigate to {os.getcwd()} and run `dotnet run`.")
                    
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

async def execute_frontend_instructions(script_output, project_name):
    try:
        # Instead of reading the last line, provide instructions to the user
        st.success(f"Frontend setup completed successfully. Your React app is located beside the API as `ReacTs_Output1`. Please navigate to the following directory in your terminal and run:\n\n`npm install --legacy-peer-deps`\n\nThen run:\n\n`npm run dev`\n\n")
        logger.info(f"User should navigate beside API folder to `ReacTs_Output1` and run `npm install` and then `npm run dev`.")

    except Exception as e:
        logger.error(f"An error occurred while executing commands: {str(e)}")
        st.error(f"An error occurred while executing commands: {str(e)}")

st.title("Project Configuration Form")

# Create tabs for Backend and Frontend
tab1, tab2 = st.tabs(["Backend API", "Frontend"])

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

            # Call the async function to inform the user to manually run `dotnet run`
            asyncio.run(execute_backend_instructions(result.stdout, project_name))

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
        db_exists = st.selectbox("DB Exists", ["No", "Yes"], index=1, key='frontend_db_exists')  # Changed index to 1 for "Yes"
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

            # Call the async function to inform the user to manually run frontend commands
            asyncio.run(execute_frontend_instructions(result.stdout, project_name))

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
