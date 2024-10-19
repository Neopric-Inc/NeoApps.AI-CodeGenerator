import streamlit as st
import json
import os
import requests
from streamlit_lottie import st_lottie
import subprocess
import logging
import mysql.connector
from mysql.connector import Error
import asyncio
import socket
# Configure logging
logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger(__name__)

# Function to load existing data from JSON file
def load_data(filename):
    if os.path.exists(filename):
        with open(filename, 'r') as f:
            return json.load(f)
    return ""

# Function to save data to JSON file
def save_data(filename, data):
    with open(filename, 'w') as f:
        json.dump(data, f)

# Function to make GPT API call
def gpt_call(api_key, prompt, content):
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json"
    }
    data = {
        "model": "gpt-3.5-turbo",
        "messages": [
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": f"{prompt}\n\nContent: {content}"}
        ]
    }
    response = requests.post("https://api.openai.com/v1/chat/completions", headers=headers, json=data)
    if response.status_code == 200:
        return response.json()['choices'][0]['message']['content']
    else:
        return f"Error: {response.status_code}, {response.text}"

# Function to load Lottie animation
def load_lottie_url(url: str):
    r = requests.get(url)
    if r.status_code != 200:
        return None
    return r.json()

# Database functions
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

# Docker functions
def check_deployment_status(container_name):
        try:
            result = subprocess.run(f"docker inspect -f '{{{{.State.Status}}}}' {container_name}", 
                                    shell=True, capture_output=True, text=True, check=True)
            status = result.stdout.strip()
            return "Deployed" if status == "running" else "Not Deployed"
        except subprocess.CalledProcessError:
            return "Not Deployed"
def update_status(container_name, status_placeholder):
        status = check_deployment_status(container_name)
        status_placeholder.markdown(f"Status: {'🟢' if status == 'Deployed' else '🔴'} {status}")

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
    modify_file('Dockerfile', '{projectName}', project_name)
    run_docker_command(f"docker build -t backend-api -f Dockerfile .")
    run_docker_command(f"docker run -d --name backend-api -p 5001:5001 -e projectName={project_name} backend-api")
    
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
        modify_file(env_file, f'REACT_APP_IS_DND_ON=.*', f'REACT_APP_IS_DND_ON="{dnd_value}"')
    else:
        logger.error(f".env file not found in {current_dir}")
        st.error(f".env file not found in {current_dir}")
        return

    dockerfile_path = os.path.join(current_dir, 'Dockerfile')
    if os.path.exists(dockerfile_path):
        run_docker_command(f"docker build -t {image_name} -f {dockerfile_path} {current_dir}")
        
        if is_dnd:
            run_docker_command(f"docker run -d --name {container_name} -e REACT_APP_IS_DND_ON={dnd_value} -p {port}:3000 {image_name}")
        else:
            run_docker_command(f"docker run -d --name {container_name} -p {port}:3000 {image_name}")
        
        st.success(f"Frontend Docker image {image_name} built and container {container_name} started on port {port}")
    else:
        logger.error(f"Dockerfile not found in {current_dir}")
        st.error(f"Dockerfile not found in {current_dir}")

def build_and_run_node_red():
    run_docker_command("docker build -t node-red -f Dockerfile-node .")
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

# Execution functions
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

        parent_dir = os.path.dirname(cwd)
        logger.debug(f"Parent directory (NeoApps.AI-CodeGenerator): {parent_dir}")

        project_root = next(d for d in os.listdir(parent_dir) if d.split('_')[0].isdigit())
        project_path = os.path.join(parent_dir, project_root)
        logger.debug(f"Project root path: {project_path}")

        if not os.path.isdir(project_path):
            logger.error(f"Project directory not found: {project_path}")
            st.error(f"Project directory not found: {project_path}")
            return

        project_specific_path = os.path.join(project_path, project_name)
        if not os.path.isdir(project_specific_path):
            logger.error(f"{project_name} directory not found in {project_path}")
            st.error(f"{project_name} directory not found in {project_path}")
            return

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
            build_frontend(False, os.getcwd())  # Build DND0
            build_frontend(True, os.getcwd())   # Build DND1
            st.success("Frontend Docker images built successfully.")

    except Exception as e:
        logger.error(f"An error occurred while executing commands: {str(e)}")
        st.error(f"An error occurred while executing commands: {str(e)}")
    finally:
        os.chdir(cwd)
        logger.debug(f"Returned to original directory: {os.getcwd()}")

async def execute_nodered_instructions(script_output, project_name, cwd):
    try:
        logger.debug(f"Starting directory (CodeGenerator): {cwd}")

        parent_dir = os.path.dirname(cwd)
        logger.debug(f"Parent directory (NeoApps.AI-CodeGenerator): {parent_dir}")

        project_root = next(d for d in os.listdir(parent_dir) if d.split('_')[0].isdigit())
        project_path = os.path.join(parent_dir, project_root)
        logger.debug(f"Project root path: {project_path}")

        if not os.path.isdir(project_path):
            logger.error(f"Project directory not found: {project_path}")
            st.error(f"Project directory not found: {project_path}")
            return

        project_specific_path = os.path.join(project_path, project_name)
        if not os.path.isdir(project_specific_path):
            logger.error(f"{project_name} directory not found in {project_path}")
            st.error(f"{project_name} directory not found in {project_path}")
            return

        reactts_path = os.path.join(project_specific_path, "ReactTs_Output4")
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
                    build_and_run_node_red()
                    check_container_health("node-red")

    except Exception as e:
        logger.error(f"An error occurred while providing Node-RED instructions: {str(e)}")
        st.error(f"An error occurred while providing Node-RED instructions: {str(e)}")
    finally:
        os.chdir(cwd)
        logger.debug(f"Returned to original directory: {os.getcwd()}")

# Set up the Streamlit app
st.set_page_config(layout="wide", page_title="ProjectPilot AI")

# Custom CSS for improved design
st.markdown("""
    <style>
    :root {
        --color-white: #ffffff;
        --color-black: #000000;
        --color-purple: #7042b2;
        --color-navy: #0f0a32;
    }

    .main .block-container {
        padding-top: 2rem;
        padding-bottom: 2rem;
    }

    h1, h2, h3 {
        color: var(--color-purple);
    }

    .stButton>button {
        background-color: var(--color-purple);
        color: var(--color-white);
    }

    .stTextInput>div>div>input, .stTextArea>div>div>textarea {
        border-color: var(--color-purple);
    }

    /* Sidebar styling */
    .css-1d391kg {
        background-color: var(--color-navy);
    }

    .css-1d391kg .streamlit-expanderHeader {
        color: var(--color-white);
    }

    /* Main area background */
    .css-18e3th9 {
        background-color: var(--color-white);
    }

    /* Dropdown and selectbox styling */
    .stSelectbox>div>div>div {
        background-color: var(--color-white);
        color: var(--color-black);
    }

    /* Slider color */
    .stSlider>div>div>div>div {
        background-color: var(--color-purple);
    }

    @media print {
        .stApp > header, .stApp > footer, .stApp [data-testid="stToolbar"], 
        .stApp [data-testid="stDecoration"], .stApp [data-testid="stStatusWidget"], 
        .stApp [data-testid="stHeader"], .stApp [data-testid="stSidebar"] {
            display: none !important;
        }
        .main .block-container {
            max-width: 100% !important;
            padding-top: 0 !important;
        }
    }
    </style>
    """, unsafe_allow_html=True)

# Initialize session state for API key and stages
if 'api_key' not in st.session_state:
    st.session_state.api_key = load_data("api_key.json")

if 'stages' not in st.session_state:
    st.session_state.stages = [
        ("Vision", "Describe your project idea and goals."),
        ("Features", "List the key features and functionalities you need."),
        ("User Perspective", "Describe how users will interact with your product."),
        ("User Journey", "Map out the step-by-step experiences users will have."),
        ("Business Rules", "Outline the important guidelines and logic for your project."),
        ("Data Blueprint", "Specify how your project's information will be organized."),
        ("Database Schema", "Generate a MySQL database schema based on your project requirements.")
  
    ]

# Sidebar
st.sidebar.title("Navigation")
selected_tab = st.sidebar.radio("Select a Stage", 
    ["Welcome", "Settings"] + 
    [stage[0] for stage in st.session_state.stages] + 
    [ "Summary","Generate, Build and Deploy", "Help and Support", "Documentation", "Templates"]
)

# Add a section to edit the AI prompts
if st.sidebar.checkbox("Customize AI Prompts"):
    st.sidebar.subheader("Customize AI Prompts")
    for stage, _ in st.session_state.stages:
        prompt_filename = f"{stage.lower().replace(' ', '_')}_prompt.json"
        current_prompt = load_data(prompt_filename)
        new_prompt = st.sidebar.text_area(f"{stage} Prompt:", value=current_prompt, height=100, key=f"prompt_{stage}")
        if st.sidebar.button(f"Update {stage} Prompt"):
            save_data(prompt_filename, new_prompt)
            st.sidebar.success(f"{stage} prompt updated successfully")

# Load Lottie animation
lottie_coding = load_lottie_url("https://assets5.lottiefiles.com/packages/lf20_fcfjwiyb.json")

def get_ip_addresses():
    ip_addresses = {
        'subnet': '127.0.0.1',
        'public': '127.0.0.1'
    }

    # Try to get the subnet IP
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip_addresses['subnet'] = s.getsockname()[0]
        s.close()
    except Exception:
        pass

    # Try to get the public IP
    try:
        public_ip = requests.get('https://api.ipify.org').text
        ip_addresses['public'] = public_ip
    except Exception:
        pass

    # If you have a static IP for your VM, you can set it here
    STATIC_IP = os.environ.get('STATIC_IP')
    if STATIC_IP:
        ip_addresses['static'] = STATIC_IP

    return ip_addresses

# New function to save schema to public folder
def save_schema_to_public(schema_content):
    public_folder = "wwwroot"
    if not os.path.exists(public_folder):
        os.makedirs(public_folder)
    
    file_path = os.path.join(public_folder, "app.sql")
    with open(file_path, "w") as f:
        f.write(schema_content)
    
    return file_path


# Main content area
if selected_tab == "Welcome":
    st.title("Welcome to ProjectPilot AI")
    col1, col2 = st.columns([2,1])
    with col1:
        st.write("""
        Transform your ideas into reality with ProjectPilot AI powered by NeoApps.AI and Neopric Inc.! 
        
        Whether you're an entrepreneur with a groundbreaking app idea, a manager planning a new business process, 
        or anyone with a vision for a digital project, ProjectPilot AI is here to guide you every step of the way.
        
        Here's how ProjectPilot AI works:
        1. **Vision**: Start by sharing your project idea.
        2. **Features**: List the key features and functionalities you need.
        3. **User Perspective**: Describe how users will interact with your product.
        4. **User Journey**: Map out the step-by-step experiences users will have.
        5. **Business Rules**: Outline the important guidelines and logic for your project.
        6. **Data Blueprint**: Specify how your project's information will be organized.
        7. **Database Schema**: Our AI assistant will create a database schema based on your requirements and data blueprint.
        8. **Summary**: Review a comprehensive summary of your project, which you can print or save as a document.
        9. **Generate, Build and Deploy**: We'll generate the code for you, creating a workable application ready for customization.
           - Use our Drag and Drop designer for frontend customization
           - Utilize Node-RED based no-code/low-code workflow designer for backend processes
        10. **Templates**: Explore a variety of templates to jumpstart your project, including:
            - Project Management Tool
            - Timesheet Application
            - Inventory Management System
            - And many more!
        
        At each stage, our AI assistant will help you refine and expand your ideas, 
        bringing you closer to a comprehensive project plan.
        
        Ready to turn your idea into workable appliication? Let's get started!
        """)
    with col2:
        st_lottie(lottie_coding, height=300, key="coding")
    
    st.markdown("---")
    st.subheader("Take Your Concept to the Next Level")
    st.write("""
    Once you've clarified your concept using ProjectPilot AI, you're ready to bring your vision to life! 
    Here are some exciting next steps:
    
    1. **Build Your Web App with NeoApps.ai**
       - It's an open-source web app builder. Its complete open source under MIT licence.
       - Begin for free and create your application
    
    2. **Need Additional Support?**
       - Not sure how to proceed? Want everything up and running quickly?
       - Book a demo or get expert help to convert your idea to reality
       - Find the "Book a Demo" form on the NeoApps.ai home screen
    
    3. **Flexible Options**
       - DIY: Use NeoApps.ai to build your application independently
       - Consultation: Get expert guidance from our team
    
    4. **Quick App Creation with NeoApps.ai**
       - Create apps similar to ProjectPilot AI quickly and easily
       - Develop specialized applications like:
         * OCR apps for your PDFs
         * Data-driven apps
         * AI chatbots
         * RAG (Retrieval-Augmented Generation) apps
       - Book a demo now for a discovery call and explore the possibilities!
    
    Ready to take the next step? Click the button below to visit NeoApps.ai and book a demo for a discovery call 
    to bring your concept to life quickly and efficiently!
    """)

    # Create a button that acts as a hyperlink
    if st.button("Visit NeoApps.AI"):
        js = f"window.open('https://neoapps.ai/')"
        html = f'<img src onerror="{js}">'
        st.components.v1.html(html, height=0, width=0)
        st.success("Opening NeoApps.AI in a new tab!")

    st.write("""
    Enjoy the process of bringing your concept to life, and best of luck with your project!
    
    Remember, whether you choose to build it yourself or seek our expertise, 
    we're here to support you in turning your concept into a fully functional web application.
    """)

elif selected_tab == "Settings":
    st.title("ProjectPilot AI Settings")
    st.write("Set up your project environment here.")
    
    # API Key input
    new_api_key = st.text_input("Enter your API Key:", value=st.session_state.api_key, type="password")
    if st.button("Save API Key"):
        if new_api_key != st.session_state.api_key:
            st.session_state.api_key = new_api_key
            save_data("api_key.json", new_api_key)
            st.success("API Key saved successfully")
        else:
            st.info("No changes made to the API Key")

elif selected_tab == "Summary":
    st.title("Project Summary")
    st.write("Here's a summary of your project. Use Ctrl+P (or Cmd+P on Mac) to print or save as PDF.")
    
    for stage, description in st.session_state.stages:
        st.header(stage)
        st.write(description)
        filename = f"{stage.lower().replace(' ', '_')}.json"
        content = load_data(filename)
        st.write(content)
        st.markdown("---")

elif selected_tab == "Generate, Build and Deploy":
    st.title("Generate, Build and Deploy")
    
    def get_public_ip():
        try:
            response = requests.get('https://api.ipify.org')
            return response.text
        except:
            return None

    def replace_localhost(text, ip):
        return text.replace('localhost', ip)

    def update_text_inputs(ip):
        for key in st.session_state.keys():
            if isinstance(st.session_state[key], str) and 'localhost' in st.session_state[key]:
                st.session_state[key] = replace_localhost(st.session_state[key], ip)

    
    def update_status(container_name, status_placeholder):
        status = check_deployment_status(container_name)
        status_placeholder.markdown(f"Status: {'🟢' if status == 'Deployed' else '🔴'} {status}")

    def trigger_tab_actions():
        # Trigger Backend API actions
        st.session_state['backend_create_db'] = True
        st.session_state['backend_drop_tables'] = True
        st.session_state['backend_build_docker'] = True
        submit_backend_button = st.session_state.get('submit_backend_button')
        if submit_backend_button:
            submit_backend_button.click()

        # Trigger Frontend actions
        st.session_state['frontend_build_docker'] = True
        submit_frontend_button = st.session_state.get('submit_frontend_button')
        if submit_frontend_button:
            submit_frontend_button.click()

        # Trigger Node-RED actions
        st.session_state['nodered_build_docker'] = True
        submit_nodered_button = st.session_state.get('submit_nodered_button')
        if submit_nodered_button:
            submit_nodered_button.click()

    if 'deployment_initiated' not in st.session_state:
        st.session_state.deployment_initiated = False

    if 'status_placeholders' not in st.session_state:
        st.session_state.status_placeholders = {
            'frontend_dnd': st.empty(),
            'frontend': st.empty(),
            'backend': st.empty(),
            'nodered': st.empty()
        }

    if st.button("Generate, Build and Deploy"):
        st.session_state.deployment_initiated = True

    if st.session_state.deployment_initiated:
        with st.spinner("Initializing deployment process..."):
            # Get public IP
            public_ip = get_public_ip()
            if public_ip:
                # Replace localhost with public IP
                update_text_inputs(public_ip)
                st.success(f"Public IP {public_ip} detected and applied.")
            else:
                st.warning("Couldn't detect public IP. Using localhost.")
                public_ip = 'localhost'

            # Trigger actions for all tabs
            trigger_tab_actions()

            # Update status for all components
            update_status("frontend-dnd1", st.session_state.status_placeholders['frontend_dnd'])
            update_status("frontend-dnd0", st.session_state.status_placeholders['frontend'])
            update_status("backend-api", st.session_state.status_placeholders['backend'])
            update_status("node-red", st.session_state.status_placeholders['nodered'])

        st.success("Deployment process completed!")
        st.session_state.deployment_initiated = False

    st.subheader("Application URLs and Deployment Status")
    col1, col2, col3, col4 = st.columns(4)

    ip_to_display = get_public_ip() or 'localhost'

    with col1:
        st.markdown("**Frontend Drag and Drop Designer**")
        st.text("http://localhost:3001")
        status = check_deployment_status("frontend-dnd1")
        st.markdown(f"Status: {'🟢' if status == 'Deployed' else '🔴'} {status}")

    with col2:
        st.markdown("**Frontend Application**")
        st.text("http://localhost:3000")
        status = check_deployment_status("frontend-dnd0")
        st.markdown(f"Status: {'🟢' if status == 'Deployed' else '🔴'} {status}")

    with col3:
        st.markdown("**Backend API (Swagger OpenAPI)**")
        st.text("http://localhost:5001/swagger")
        status = check_deployment_status("backend-api")
        st.markdown(f"Status: {'🟢' if status == 'Deployed' else '🔴'} {status}")

    with col4:
        st.markdown("**Node-RED Workflow Designer**")
        st.text("http://localhost:1880")
        status = check_deployment_status("node-red")
        st.markdown(f"Status: {'🟢' if status == 'Deployed' else '🔴'} {status}")

    st.markdown("---")
    
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
            st.session_state['submit_backend_button'] = submit_backend_button

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

            
            build_docker = st.checkbox("Build Docker images for Frontend")

            submit_frontend_button = st.form_submit_button(label='Submit Frontend')
            st.session_state['submit_frontend_button'] = submit_frontend_button

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

            
            build_docker = st.checkbox("Build and run Docker container for Node-RED")

            submit_nodered_button = st.form_submit_button(label='Submit Node-RED')
            st.session_state['submit_nodered_button'] = submit_nodered_button

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

    # Add a cleanup button at the end of the "Generate, Build and Deploy" section
    if st.button("Cleanup Docker Resources"):
        cleanup_docker_resources()

elif selected_tab == "Help and Support":
    st.title("Help and Support")
    st.write("If you need assistance, please send a support request to the email address below:")
    st.info("info@neopric.com")

elif selected_tab == "Documentation":
    st.title("Documentation")
    st.write("For detailed documentation, please visit our documentation site:")
    if st.button("View Documentation"):
        js = "window.open('https://docs.neoapps.ai/')"
        html = f'<img src onerror="{js}">'
        st.components.v1.html(html, height=0, width=0)
        st.success("Opening documentation in a new tab!")

elif selected_tab == "Templates":
    st.title("Templates")
    
    with st.expander("Requirements Document"):
        st.subheader("Requirements Document Template")
        requirements_doc = """
        1. Project Overview
        2. Functional Requirements
        3. Non-Functional Requirements
        4. User Stories
        5. Use Cases
        6. System Architecture
        7. Data Requirements
        8. Interface Requirements
        9. Performance Requirements
        10. Security Requirements
        11. Constraints
        12. Assumptions and Dependencies
        13. Acceptance Criteria
        14. Glossary
        """
        st.code(requirements_doc, language="markdown")
    
    with st.expander("Database Schema"):
        st.subheader("Sample Database Schema")
        db_schema = """
        CREATE TABLE Users (
            user_id INT PRIMARY KEY,
            username VARCHAR(50) UNIQUE NOT NULL,
            email VARCHAR(100) UNIQUE NOT NULL,
            password_hash VARCHAR(255) NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE Products (
            product_id INT PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            description TEXT,
            price DECIMAL(10, 2) NOT NULL,
            stock_quantity INT NOT NULL
        );

        CREATE TABLE Orders (
            order_id INT PRIMARY KEY,
            user_id INT,
            order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            total_amount DECIMAL(10, 2) NOT NULL,
            FOREIGN KEY (user_id) REFERENCES Users(user_id)
        );
        """
        st.code(db_schema, language="sql")
    
    with st.expander("Frontend Configuration Schema"):
        st.subheader("Sample Frontend Configuration Schema")
        frontend_schema = """
        {
          "app": {
            "name": "MyApp",
            "version": "1.0.0",
            "theme": {
              "primaryColor": "#3366cc",
              "secondaryColor": "#f0f0f0",
              "fontFamily": "Arial, sans-serif"
            }
          },
          "pages": [
            {
              "name": "Home",
              "route": "/",
              "components": [
                {
                  "type": "header",
                  "content": "Welcome to MyApp"
                },
                {
                  "type": "paragraph",
                  "content": "This is the home page of MyApp."
                }
              ]
            },
            {
              "name": "About",
              "route": "/about",
              "components": [
                {
                  "type": "header",
                  "content": "About Us"
                },
                {
                  "type": "paragraph",
                  "content": "Learn more about our company and mission."
                }
              ]
            }
          ]
        }
        """
        st.code(frontend_schema, language="json")

else:
    # Find the current stage
    current_stage = next(stage for stage in st.session_state.stages if stage[0] == selected_tab)
    
    st.title(f"{current_stage[0]}")
    st.write(current_stage[1])

    # Load existing data for the current tab
    filename = f"{current_stage[0].lower().replace(' ', '_')}.json"
    existing_data = load_data(filename)
    
    # Load prompt for the current stage
    prompt_filename = f"{current_stage[0].lower().replace(' ', '_')}_prompt.json"
    prompt = load_data(prompt_filename)
    
    if current_stage[0] == "Database Schema":
        if 'schema_input' not in st.session_state:
            st.session_state.schema_input = existing_data

        st.session_state.schema_input = st.text_area("Database Schema:", value=st.session_state.schema_input, height=400)
        
        col1, col2, col3 = st.columns(3)
        
        with col1:
            if st.button("Generate Schema"):
                if st.session_state.api_key and prompt:
                    data_blueprint = load_data("data_blueprint.json")
                    accumulated_content = f"Data Blueprint:\n{data_blueprint}\n\nDatabase Schema Requirements:\n{st.session_state.schema_input}"
                    
                    with st.spinner("Generating MySQL Database Schema..."):
                        generated_schema = gpt_call(st.session_state.api_key, prompt, accumulated_content)
                    
                    st.session_state.schema_input = generated_schema
                    st.experimental_rerun()
                else:
                    if not st.session_state.api_key:
                        st.warning("Please set up your API key in the Settings tab to use our AI assistant.")
                    if not prompt:
                        st.warning("The AI prompt for this stage is missing. Please contact support.")
        
        with col2:
            if st.button("Save Schema"):
                # Save the schema to the public folder
                file_path = save_schema_to_public(st.session_state.schema_input)
                
                # Get the current IP and port
                ip_address = get_ip_addresses()
                port = 8501  # Default Streamlit port, you might need to adjust this
                
                # Create the public URL
                public_url = f"http://{ip_address}:{port}/{os.path.basename(file_path)}"
                
                st.success(f"Schema saved successfully! You can access it at: {public_url}")
                
                # Also save it to the JSON file for persistence
                save_data(filename, st.session_state.schema_input)

        with col3:
            if st.button("Copy Schema"):
                st.write('<textarea id="schema-text" style="position: absolute; left: -9999px;">' + st.session_state.schema_input + '</textarea>', unsafe_allow_html=True)
                st.write('<script>const text = document.getElementById("schema-text"); text.select(); document.execCommand("copy");</script>', unsafe_allow_html=True)
                st.success("Schema copied to clipboard!")

        st.subheader("Current MySQL Database Schema:")
        st.code(st.session_state.schema_input, language="sql")

    else:
        # For other stages, keep the existing logic
        user_input = st.text_area("Share your thoughts here:", value=existing_data, height=200)
        
        if st.button("Save and Generate Next Steps"):
            save_data(filename, user_input)
            st.success(f"Great job! Your {current_stage[0].lower()} has been saved.")
            
            if st.session_state.api_key and prompt:
                current_index = [stage[0] for stage in st.session_state.stages].index(current_stage[0])
                if current_index < len(st.session_state.stages) - 1:
                    next_stage = st.session_state.stages[current_index + 1]
                    next_filename = f"{next_stage[0].lower().replace(' ', '_')}.json"
                    
                    # Accumulate content from previous stages
                    accumulated_content = ""
                    for i in range(current_index + 1):
                        stage_filename = f"{st.session_state.stages[i][0].lower().replace(' ', '_')}.json"
                        stage_content = load_data(stage_filename)
                        accumulated_content += f"{st.session_state.stages[i][0]}:\n{stage_content}\n\n"
                    
                    with st.spinner(f"Our AI is working on your {next_stage[0].lower()}..."):
                        generated_content = gpt_call(st.session_state.api_key, prompt, accumulated_content)
                    st.subheader(f"Here's a draft for your {next_stage[0].lower()}:")
                    st.write(generated_content)
                    
                    # Save generated content for the next stage
                    save_data(next_filename, generated_content)
            else:
                if not st.session_state.api_key:
                    st.warning("Please set up your API key in the Settings tab to use our AI assistant.")
                if not prompt:
                    st.warning("The AI prompt for this stage is missing. Please contact support.")

        # Display current content
        st.subheader("Your Current Progress:")
        st.write(user_input)