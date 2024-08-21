# NeoApps.AI- Code Generator
# Project Setup and App Generation Guide

## Prerequisites

Before running the project, ensure you have the following installed:

1. **Visual Studio** or **Visual Studio Code**
2. **Docker**
3. **XAMPP**
4. **Redis, RabbitMQ, and MinIO (S3)**

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
{
  "project_id": 1,
  "server": "localhost",
  "uid": 1,
  "username": "root",
  "password": "",
  "databaseName": "splitthebill",
  "script": "http://localhost/split_app_script.sql",
  "statusOfGeneration": null,
  "projectName": "SplitTheBill",
  "DBexists": "No",
  "port": 3306,
  "rabbitMQConn": "amqp://user:password@localhost:5672/",
  "redisConn": "localhost:6379,password=REDIS_PASSWORD=yourredispassword",
  "apiflowurl": "",
  "fronttemplateurl": "",
  "Technology_Frontend": "",
  "Backend_technology": "dotnet",
  "buttonClicked": "generate",
  "projectType": "",
  "swgurl": "",
  "noderedurl": null
}
```

Update `launchSettings.json` with the following parameters for frontend generation:

```json
{
    "project_id": 1,
    "server": "localhost",
    "uid": 1,
    "username": "root",
    "password": "",
    "databaseName": "splitthebill",
    "script": "http://localhost/split_app_script.sql",
    "statusOfGeneration": "",
    "projectName": "ContentPlannerTest",
    "DBexists": "Yes",
    "port": 3306,
    "rabbitMQConn": "https://localhost:5001/v1/api/",
    "redisConn": "localhost:6379",
    "password": "12345",
    "apiflowurl": "",
    "fronttemplateurl": "",
    "Technology_Frontend": "reactts",
    "Backend_technology": "",
    "buttonClicked": "generate",
    "projectType": "dnd",
    "swgurl": "",
    "noderedurl": ""
}

```
Configuration Parameters
Below is a list of configuration parameters used in the project setup along with their explanations:

project_id:
The unique identifier for the project. You can use any number. 
Example: 1

server:
The server where the database is hosted. This will be database server (mysql). Do not use production database server here.
Example: localhost

uid:
The unique identifier for the user. Dont change this one.
Example: 1

username:
The database username. Make sure its admin user. 
Example: root

password:
The password for the database user.
Example: "" (empty string)

databaseName:
The name of the database to be used for the project.
Example: splitthebill

script:
The URL or path to the SQL script used to initialize the database. So if you notice the file is hosted in webserver in xampp htdocs.
Example: http://localhost/split_app_script.sql

statusOfGeneration:
The status of the project generation process. Ignore it but dont change it.
Example: "" (empty string, to be updated during the process)

projectName:
The name of the project. Use the ProjectName without number and special character in order to run it smoothly.
Example: ContentPlannerTest

DBexists:
Indicates whether the database already exists. For backend generation this will be NO and it will reacreate the database tables. For Frontend It will be YES.
Example: "Yes"

port:
The port number on which the database server is listening.
Example: 3306

rabbitMQConn:
The connection URL will be used for rabbitMQ connection when generating backend and As API connection url while generating frontend.
Example: https://localhost:5001/v1/api/

redisConn:
The connection string for Redis, typically used for caching.
Example: localhost:6379

password:
The password for Redis connection.
Example: 12345

apiflowurl:
The URL for the API flow and this will be explained in future docs and videos.
Example: "" (empty, to be defined based on your setup)

fronttemplateurl:
The URL for the frontend template, used for project scaffolding. Future roadmap.
Example: "" (empty, to be defined based on your setup)

Technology_Frontend:
The technology stack used for the frontend development. Dont change for now.
Example: reactts (React with TypeScript)

Backend_technology:
The technology stack used for backend development. Dont change for now and it wll be different for backend.
Example: "" (empty, to be specified based on your project)

buttonClicked:
Indicates the action taken by the user, such as generating the project. Dont change for now. This will be use to controll  generate,build and deploy.
Example: "generate"

projectType:
The type of project being generated, e.g., drag-and-drop (DND).
Example: "dnd"

swgurl:
The URL for Swagger, used for API documentation.
Example: "" (empty, to be filled after project setup)

noderedurl:
The URL for Node-RED, a flow-based development tool.
Example: "" (empty, to be filled based on setup)





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

- **[Tutorials](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/README-Setup-Guide):** Hands-on tutorials to help you setup project , generate code , build code and host locally.

This guide includes step-by-step instructions and a video tutorial to help you through the entire process.

To help you get started and make the most of NeoApps.ai, we have prepared comprehensive documentation:

- **[Tutorials](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/tutorials.md):** Hands-on tutorials to help you build and deploy your first app.



