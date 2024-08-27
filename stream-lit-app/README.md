
# Project Configuration Form

This is a simple Streamlit application that allows users to input various project configuration parameters through a form and displays the submitted data in JSON format.

## Features

- Input fields for various project configuration parameters like project ID, server, user ID, database credentials, etc.
- Form submission displays the parameters in a JSON format.

## Getting Started

### Prerequisites

- Docker installed on your machine.
- Basic knowledge of running Docker containers.

### Installation

1. Clone the repository or download the source code.

2. Navigate to the project directory.

3. Build the Docker image:

   ```bash
   docker build -t my-streamlit-app .
   ```

### Usage

1. Run the Docker container:

   ```bash
   docker run -p 8501:8501 my-streamlit-app
   ```

2. Open your web browser and go to `http://localhost:8501`.

3. Fill out the form with the necessary configuration parameters.

4. Submit the form to view the parameters in JSON format.

### File Structure

- `app.py`: The main Streamlit application file containing the form and logic to display submitted data.
- `Dockerfile`: The Dockerfile used to create a Docker image for the application.
- `requirements.txt`: A file listing the Python dependencies needed for the application.

### Dependencies

- Streamlit

### Built With

- [Streamlit](https://streamlit.io) - The framework used for building the web application.

### Acknowledgments

- Streamlit Documentation
- Docker Documentation
