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
  -e REDIS_PASSWORD=${REDIS_PASSWORD} \
  -v redis-data:/data redis:latest \
  redis-server --requirepass ${REDIS_PASSWORD} --appendonly yes
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

To help you get started and make the most of NeoApps.ai, we have prepared comprehensive documentation:

- **[Tutorials](https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator/blob/main/tutorials.md):** Hands-on tutorials to help you build and deploy your first app.



