import streamlit as st
import json

# Streamlit app title
st.title("Project Configuration Form")

# Form to collect the configuration parameters
with st.form(key='config_form'):
    project_id = st.text_input("Project ID", "1")
    server = st.text_input("Server", "localhost")
    uid = st.text_input("User ID", "1")
    username = st.text_input("Database Username", "root")
    password = st.text_input("Database Password", "")
    database_name = st.text_input("Database Name", "splitthebill")
    script = st.text_input("Script URL", "http://localhost/split_app_script.sql")
    status_of_generation = st.text_input("Status of Generation", "null")
    project_name = st.text_input("Project Name", "ContentPlannerTest")
    db_exists = st.selectbox("DB Exists", ["No", "Yes"])
    port = st.text_input("Port", "3306")
    rabbitmq_conn = st.text_input("RabbitMQ Connection URL", "amqp://user12345:12345@localhost:5672/")
    redis_conn = st.text_input("Redis Connection String", "localhost:6379")
    redis_password = st.text_input("Redis Password", "12345")
    apiflowurl = st.text_input("API Flow URL", "")
    fronttemplateurl = st.text_input("Frontend Template URL", "")
    technology_frontend = st.text_input("Frontend Technology", "")
    backend_technology = st.text_input("Backend Technology", "dotnet")
    button_clicked = st.text_input("Button Clicked", "generate")
    project_type = st.text_input("Project Type", "")
    swgurl = st.text_input("Swagger URL", "")
    noderedurl = st.text_input("Node-RED URL", "null")

    # Submit button
    submit_button = st.form_submit_button(label='Submit')

# Display the JSON output after submission
if submit_button:
    parameters = {
        "project_id": project_id,
        "server": server,
        "uid": uid,
        "username": username,
        "password": password,
        "databaseName": database_name,
        "script": script,
        "statusOfGeneration": status_of_generation,
        "projectName": project_name,
        "DBexists": db_exists,
        "port": port,
        "rabbitMQConn": rabbitmq_conn,
        "redisConn": redis_conn,
        "password": redis_password,
        "apiflowurl": apiflowurl,
        "fronttemplateurl": fronttemplateurl,
        "Technology_Frontend": technology_frontend,
        "Backend_technology": backend_technology,
        "buttonClicked": button_clicked,
        "projectType": project_type,
        "swgurl": swgurl,
        "noderedurl": noderedurl
    }

    # Format the parameters as a JSON string
    formatted_parameters = json.dumps(parameters)

    st.write("### Submitted Parameters")
    st.write(f'"PARAMETER": {formatted_parameters}')
