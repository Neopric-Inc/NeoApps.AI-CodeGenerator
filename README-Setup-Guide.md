
# Project Setup and First-Time Usage

## Before you start, you may want to watch this video that explains the entire setup process, from generating the project to hosting it locally:

Full Setup Video Explanation: Link to video (Replace # with your video link)

## 1. Drop the Database Tables
For first-time or re-initializing usage, start by dropping the database tables.

## 2. Update Launch Settings
Change the launch settings to backend mode to ensure proper project configuration.

## 3. Re-Run the Project
After adjusting the settings, re-run the project to regenerate the database tables and the API.

## 4. Verify Configuration
Once the project regenerates:
- Navigate to the project directory.
- Open it with Visual Studio Code.
- Check the `appsettings.json` file to confirm that all configurations are correct:
  - RabbitMQ
  - Redis
  - Database connection string
  - S3 (Minio keys)

## 5. Run the API
Open a terminal in the `.API` project directory and run `dotnet run`.
- If you encounter errors, check the database creation guidelines.
- Once the API runs successfully, copy your endpoint, which should be something like `https://localhost:5001/v1/api/`.
- You can also access Swagger at `https://localhost:5001/swagger`.

## 6. Understanding the API
If you want to learn how to use the generated API using the Swagger GUI, we will add a video here shortly.

## 7. User Authentication
Once the API is generated, go to the database tables and note down the username and password hash from the `users` table.
- The system currently uses direct authentication with `password_hash`.
- You have the flexibility to change the authentication logic and choose any hash function for encryption and decryption.
- We will add a video explaining how to do this, along with authentication and authorization library support for open source.

## 8. API Use Cases
If you want to understand each API and its use case, we will add short videos explaining each API, its usage, and the future roadmap of API types integration.

# Frontend App Setup

## 1. Connect to the API
After generating and hosting the API locally, proceed with frontend app generation.
- Copy the API Connection URL and update it in the launch settings.
- Save the launch settings and re-run the project with `dotnet run`.

## 2. Build and Run the Frontend Project
After the backend is running:
- Open the frontend project directory in a terminal or command prompt.
- Build and run the project using `dotnet build` and `dotnet run`.

## 3. Install Dependencies
Once the project is generated:
- Navigate to the project directory in Visual Studio Code’s new instance.
- Install the npm dependencies with `npm peer deps`.
- If regenerating the project, you may remove the `node_modules` folder; otherwise, it’s fine to keep it.

## 4. Environment Configuration
Ensure the `.env` file contains the correct URLs for the version of the project you want to run.

## 5. Run Development Mode
Run `npm run dev` in the terminal for development mode.
- Once the project is up and running, it will open the React app login window.
- Use the username and password that you copied from the above step.

## 6. Regenerating the Project
If you need to regenerate the project, ensure that you are logged out so the token is removed from local storage.

## 7. Using the Drag-and-Drop Designer
After logging in, you can use the drag-and-drop designer to design the application with its database table components.
- We will add a video explaining different ways to design the pages, including a current limitation of achieving breadcrumbs after designing the pages.
- Check the roadmap for the feature release date.

## 8. Customizing UI Design
We will add a video on how to make your own changes in code to achieve AWESOME UI design with minimal intervention using MUI.

## 9. Deployment Settings
The drag-and-drop designer is one side of the project, and you might need to flip the setting in order to deploy the application.
- Change the variable `REACT_APP_IS_DND_ON` from `"1"` to `"0"`.
- This way, when you run the project, it will use the designed version of the application and be ready to use.

## 10. Exporting Configuration
If you are satisfied with your design before release and don’t want to make any changes in the database for the data model, you can export the configuration and save it.
- Ensure this works with the version you have downloaded for code generation and that only the generated application is used.

# Docker Integration

## 1. Running the Project Locally
You can use the Dockerfile to run the project locally.

## 2. Hosting the Project
You can push the generated project to Docker Hub or your private repository.
- You can host the project in the way you prefer, whether for frontend or backend.
- All applications have default Docker integration for deployment in containers.

## 3. Deployment Support
We support deployment on Azure and AWS.
- If you want to learn more about deployment, we will add a video shortly.
- To understand the true potential of this project, we will add another video to explain the use cases and potential.
