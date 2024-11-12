<p align="center">
    <img src="https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/logo.png" alt="NeoApps.AI Logo" width="500">
</p>

# NeoApps.AI- Code Generator

# Project Setup and App Generation Guide

## Demo

<details>
<summary><kbd>Watch the demo - Manual Setup!</kbd></summary>

[![Watch the video](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/neoapps_ai_thumbnail.png)](https://www.youtube.com/watch?v=i2SOFnlYknU)

</details>

<details>
<summary><kbd>Watch the demo - Simplified Version -Streamlit App!</kbd></summary>

[![Watch the video](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/neoapps_ai_thumbnail.png)](https://youtu.be/F5bmjPtciLw)

</details>

<details>
<summary><kbd>Watch the demo - NeoApps.AI App Builder Platform  Walkthrough </kbd></summary>

[![Watch the video](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/neoapps_ai_thumbnail.png)](https://youtu.be/auRU5O11HyY?si=a7rAUD7iKuAOr2UV)

</details>

<details>
<summary><kbd>Watch the demo - BrainDump using ChatGPT & NeoApps.AI </kbd></summary>

[![Watch the video](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/neoapps_ai_thumbnail.png)](https://youtu.be/f67EsCN4rMY?si=KI5C4qpa19xEyxEw)

</details>

## YouTube Videos

For additional tutorials and in-depth guides on using NeoApps.AI, check out these YouTube videos:

- **[Using NeoApps.AI Drag-and-Drop Designer](https://youtu.be/Xa59gzPzNPI?si=UZP-qYhb553kEftu)**: A complete guide to designing and previewing your Brain Dump application with NeoApps.AI’s drag-and-drop designer.

- **[Building a Timesheet Tracker App](https://youtu.be/iVvRGH0gfI8?si=LpPRQ6l__wlRUotq)**: A step-by-step walkthrough of creating a robust Timesheet Tracker application.

## Prerequisites

Before running the project, ensure you have the following installed:

1. **Visual Studio** or **Visual Studio Code**
2. **.NET SDK**
3. **Docker**
4. **XAMPP**
5. **Redis, RabbitMQ, and MinIO (S3)**
6. **Node**
7. **Python 3.x installed and pip3 or pip (Python package installer).**

# Simplified GUI Version with Streamlit App

1. **Pull the repository** and check the prerequisites.
2. **Set up the Claude project** according to the instructions.
3. **Generate the requirements** for your project.
4. **Save the database file** as `app.sql`.
5. **Drop the database file** into the `htdocs` folder of XAMPP. Ensure that **XAMPP** is running and **Docker** is up and running.
6. Navigate to the `CodeGenerator` folder and **run `pip install -r requirements.txt` and then **run `./codegenerator_script.sh`\*\*.
7. The **Streamlit app** will open.
8. **Review the inputs**: You can change the project name or leave it as is. Experiment with other settings once you're comfortable.
9. **Hit submit** and scroll down to see the results.
10. Go to the newly generated folder and **execute the command `dotnet run`**.
11. After execution, **copy the API URL**.
12. **Terminate the Streamlit app** with `Ctrl+C`.
13. **Re-run the app** using `./codegenerator_script.sh`.
14. When the app reopens, **click on Frontend** and update the API URL you copied (replace `localhost` and the port number in the URL as needed, e.g., `https://localhost:5001/v1/api/`).
15. **Submit and experiment** further if needed by following the instructions at the bottom of the page.
16. A new project will be generated in the same folder named `ReactTsOutput1`.
17. **Open the newly generated React project**.
18. **Install the necessary Node packages**.
19. **Run the app**.
20. This will open the **login window**.
21. Go to the database and **copy the username and password** (note: the password is not hashed for development purposes, but you can apply your own hashing algorithm).
22. **Login to the app** and design your pages.
23. Click **Update UI** to save your changes, **Release UI** to finalize it, and **Preview UI** to see a preview.
24. Once satisfied, **disable the DnD (drag and drop designer)** by updating the `.env` file.
25. **Re-run the project** using `npm run dev`, and it will open the newly designed app directly.
26. **Congratulations!** You’ve created your first app without writing any code, but still built a custom-developed application.
27. **Deploy the app** on a Docker container or **contact us on Discord** if you need help with deployment.

# Manual Setup --------------------------

### Docker Setup for Redis, RabbitMQ, and MinIO (S3)

Use the following Docker commands to set up Redis, RabbitMQ, and MinIO (S3):

- **MinIO**

  ```bash
  docker run -d --name minio -p 9000:9000 --env-file .env \
  -e MINIO_ROOT_USER=${MINIO_ROOT_USER} \
  -e MINIO_ROOT_PASSWORD=${MINIO_ROOT_PASSWORD} \
  -v minio-data:/data minio/minio:latest server /data
  ```

- **Redis**

  ```bash
  docker run -d --name redis -p 6379:6379 --env-file .env \
  -v redis-data:/data redis:latest \
  redis-server --requirepass "$(grep REDIS_PASSWORD .env | cut -d '=' -f2)" --appendonly yes

  ```

- **RabbitMQ**
  ```bash
  docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 --env-file .env \
  -v rabbitmq-data:/var/lib/rabbitmq rabbitmq:management
  ```

### Configuration

Add the following configurations to `appsettings.json` for MinIO (S3), Redis, and RabbitMQ:

```json
{
  "MinIO": {
    "AccessKey": "${MINIO_ROOT_USER}",
    "SecretKey": "${MINIO_ROOT_PASSWORD}",
    "Endpoint": "http://localhost:9000"
  },
  "Redis": {
    "Connection": "localhost:6379,password=${REDIS_PASSWORD}"
  },
  "RabbitMQ": {
    "Connection": "amqp://${RABBITMQ_USER}:${RABBITMQ_PASSWORD}@localhost:5672/"
  }
}
```

## Steps to Generate the Project

### 1. Database Schema Design

Ensure your database schema follows the guidelines provided by NeoApps.AI. You can find the guidelines [here](https://docs.neoapps.ai/User%20Guide/Project%20Creation/databaseguidelines).

### 2. Save the Script

Save your database script in `.sql` format. Example scripts are available on the guidelines page.

### 3. Upload Script to XAMPP

Upload your script to XAMPP by placing it in the `htdocs` directory. This will allow it to be accessible via the XAMPP server.

### 4. Setup MySQL Database

Set up your MySQL database with the following parameters:

- **Database Name**: Your chosen database name
- **Username**: `root` (or your configured username)
- **Password**: (leave empty if not set)
- **Port**: `3306`

### 5. Update Launch Settings

Update `launchSettings.json` with the following parameters for backend generation:

```json
"PARAMETER": "{project_id:1,server:localhost,uid:1,username:root,password:,databaseName:splitthebill,script:http://localhost/split_app_script.sql,statusOfGeneration:null,projectName:ContentPlannerTest,DBexists:No,port:3306,rabbitMQConn:amqp://user12345:12345@localhost:5672/,redisConn:localhost:6379,password=12345,apiflowurl:,fronttemplateurl:,Technology_Frontend:,Backend_technology:dotnet,buttonClicked:generate,projectType:,swgurl:,noderedurl:null}"
```

Update `launchSettings.json` with the following parameters for frontend generation:

```json
"PARAMETER": "{project_id:1,server:localhost,uid:1,username:root,password:,databaseName:splitthebill,script:http://localhost/split_app_script.sql,statusOfGeneration:,projectName:ContentPlannerTest,DBexists:Yes,port:3306,rabbitMQConn:amqp://user12345:12345@localhost:5672/,redisConn:localhost:6379,password=12345,apiflowurl:,fronttemplateurl:,Technology_Frontend:reactts,Backend_technology:,buttonClicked:generate,projectType:dnd,swgurl:,noderedurl:}"

```

# **Configuration Parameters**

Below is a list of configuration parameters used in the project setup, along with their explanations:

##### `project_id`

- **Description:** The unique identifier for the project. You can use any number.
- **Example:** `1`

### `server`

- **Description:** The server where the database is hosted. This will be the database server (MySQL). Do not use a production database server here.
- **Example:** `localhost`

### `uid`

- **Description:** The unique identifier for the user. Don't change this one.
- **Example:** `1`

### `username`

- **Description:** The database username. Make sure it's an admin user.
- **Example:** `root`

### `password`

- **Description:** The password for the database user.
- **Example:** `""` (empty string)

### `databaseName`

- **Description:** The name of the database to be used for the project.
- **Example:** `splitthebill`

### `script`

- **Description:** The URL or path to the SQL script used to initialize the database. This file is typically hosted on a web server in the XAMPP `htdocs` directory.
- **Example:** `http://localhost/split_app_script.sql`

### `statusOfGeneration`

- **Description:** The status of the project generation process. Ignore this, but don't change it.
- **Example:** `""` (empty string, to be updated during the process)

### `projectName`

- **Description:** The name of the project. Use the project name without numbers or special characters to ensure smooth operation.
- **Example:** `ContentPlannerTest`

### `DBexists`

- **Description:** Indicates whether the database already exists. For backend generation, this will be `NO`, and it will recreate the database tables. For frontend generation, it will be `YES`.
- **Example:** `"Yes"`

### `port`

- **Description:** The port number on which the database server is listening.
- **Example:** `3306`

### `rabbitMQConn`

- **Description:** The connection URL for RabbitMQ, used when generating the backend and as an API connection URL while generating the frontend.
- **Example:** `https://localhost:5001/v1/api/`

### `redisConn`

- **Description:** The connection string for Redis, typically used for caching.
- **Example:** `localhost:6379`

### `password` _(for Redis)_

- **Description:** The password for Redis connection.
- **Example:** `1234

### `apiflowurl`

- **Description:** The URL for the API flow. This will be explained in future documentation and videos.
- **Example:** `""` (empty, to be defined based on your setup)

### `fronttemplateurl`

- **Description:** The URL for the frontend template, used for project scaffolding. Future roadmap.
- **Example:** `""` (empty, to be defined based on your setup)

### `Technology_Frontend`

- **Description:** The technology stack used for frontend development. Don't change this for now.
- **Example:** `reactts` (React with TypeScript)

### `Backend_technology`

- **Description:** The technology stack used for backend development. Don't change this for now. It will be different for the backend.
- **Example:** `""` (empty, to be specified based on your project)

### `buttonClicked`

- **Description:** Indicates the action taken by the user, such as generating the project. Don't change this for now. This will be used to control generate, build, and deploy actions.
- **Example:** `"generate"`

### `projectType`

- **Description:** The type of project being generated, e.g., drag-and-drop (DND). No need to change anything here.
- **Example:** `"dnd"`

### `swgurl`

- **Description:** The URL for Swagger, used for API documentation.
- **Example:** `""` (empty, to be filled after project setup). No need to change anything here for now.

### `noderedurl`

- **Description:** The URL for Node-RED, a flow-based development tool. No need to change anything here for now.
- **Example:** `""` (empty, to be filled based on setup)

### 6. Generate Backend

Once you generate the project, the generated code will be available in the `bin/debug` folder.

### 7. Save the Generated Code

Copy the generated code folder and place it into your repository or preferred directory.

### 8. Run the Project

Open the project in Visual Studio or Visual Studio Code and run it. If you encounter any errors, check your database schema for issues.

### 9. Generate Frontend

Generate the frontend code and ensure both the frontend and backend projects are configured correctly. Copy them into your repository or preferred directory.

### 10. Regenerate Project

If you need to regenerate the project or make changes to the database schema, drop the existing database tables and rerun the project.

### 11. Run the .NET API

After copying the projects, run the .NET API as needed.

---

Follow these steps, and you should be able to run the project and generate the apps without any issues.

## Documentation

Setup and Installation Guide
For a detailed guide on setting up the project, generating the API, and hosting it locally, please refer to the Setup and Installation Guide.

- **[Tutorials](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/README-Setup-Guide.md):** Hands-on tutorials to help you setup project , generate code , build code and host locally.

This guide includes step-by-step instructions and a video tutorial to help you through the entire process.

To help you get started and make the most of NeoApps.ai, we have prepared comprehensive documentation:

- **[Tutorials](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/tutorials.md):** Hands-on tutorials to help you build and deploy your first app.
