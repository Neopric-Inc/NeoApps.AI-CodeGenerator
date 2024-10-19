import streamlit as st
import json
import os
import requests
from streamlit_lottie import st_lottie

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

# Set up the Streamlit app
st.set_page_config(layout="wide", page_title="ConceptToCode")

# Custom CSS for improved design
st.markdown("""
    <style>
    .main .block-container {
        padding-top: 2rem;
        padding-bottom: 2rem;
    }
    h1, h2, h3 {
        color: #3366cc;
    }
    .stButton>button {
        background-color: #3366cc;
        color: white;
    }
    .stTextInput>div>div>input, .stTextArea>div>div>textarea {
        border-color: #3366cc;
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
        ("Data Blueprint", "Specify how your project's information will be organized.")
    ]

# Sidebar
st.sidebar.title("Navigation")
selected_tab = st.sidebar.radio("Select a Stage", 
    ["Welcome", "Settings", "Summary"] + 
    [stage[0] for stage in st.session_state.stages] + 
    ["Generate, Build and Deploy", "Help and Support", "Documentation", "Templates"]
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

# Main content area
if selected_tab == "Welcome":
    st.title("Welcome to ConceptToCode")
    col1, col2 = st.columns([2,1])
    with col1:
        st.write("""
        Transform your ideas into reality with ConceptToCode powered by NeoApps.AI and Neopric Inc.! 
        
        Whether you're an entrepreneur with a groundbreaking app idea, a manager planning a new business process, 
        or anyone with a vision for a digital project, ConceptToCode is here to guide you every step of the way.
        
        Here's how ConceptToCode works:
        1. **Vision**: Start by sharing your project idea.
        2. **Features**: List what your project needs to do.
        3. **User Perspective**: Describe how people will use your product.
        4. **User Journey**: Map out the user's experience step-by-step.
        5. **Business Rules**: Outline the important guidelines for your project.
        6. **Data Blueprint**: Organize how your project will handle information.
        
        At each stage, our AI assistant will help you refine and expand your ideas, 
        bringing you closer to a comprehensive project plan.
        
        Ready to turn your concept into code? Let's get started!
        """)
    with col2:
        st_lottie(lottie_coding, height=300, key="coding")
    
    st.markdown("---")
    st.subheader("Take Your Concept to the Next Level")
    st.write("""
    Once you've clarified your concept using ConceptToCode, you're ready to bring your vision to life! 
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
       - Create apps similar to ConceptToCode quickly and easily
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
    st.title("ConceptToCode Settings")
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
    st.write("This section will be implemented later.")
    # Placeholder for future implementation

elif selected_tab == "Help and Support":
    st.title("Help and Support")
    st.write("If you need assistance, please send a support request to the email address below:")
    st.info("support@neoapps.ai")

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
    
    # Text area for input
    user_input = st.text_area("Share your thoughts here:", value=existing_data, height=200)
    
    # Submit button
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

    # Display generated data for the last stage
    if current_stage[0] == st.session_state.stages[-1][0]:
        st.subheader("Generated Data for Data Blueprint:")
        generated_data = load_data(f"{current_stage[0].lower().replace(' ', '_')}.json")
        st.write(generated_data)

st.sidebar.info("To print or save your project summary as PDF, go to the 'Summary' page and use Ctrl+P (or Cmd+P on Mac).")