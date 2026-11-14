# Event Ticketing in ASP.NET!

A small school project for learning how to build an event-ticketing
application with ASP.NET, Blazor, and MySQL.

## Technologies

-   **Backend:** ASP.NET Core Web API (C#)
-   **Frontend:** Blazor WebAssembly (C# WASM)
-   **Database:** MySQL
-   **Database connection:** SSH tunnel
-   **Authentication:** JWT with BCrypt password hashing

## Required Software and Dependencies

Install the following before running the project:

-   .NET SDK compatible with the solution's target framework
-   MySQL server and a MySQL client (for running the SQL scripts)
-   OpenSSH client (for creating the SSH tunnel)
-   Git
-   Visual Studio or Visual Studio Code (recommended)

### Backend packages

Run these commands from the `Backend/` folder:

``` powershell
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.11
dotnet add package BCrypt.Net-Next
```

### Frontend packages

Run these commands from the `Frontend/` folder:

``` powershell
dotnet add package Microsoft.AspNetCore.Components.Authorization
dotnet add package Microsoft.Extensions.Http
```

> If the packages are already listed in the project files, restoring the
> solution with `dotnet restore` may be all that is needed.

## Setup and Configuration

### 1. Clone the repository

Clone or pull the latest version of the repository's `main` branch.

### 2. Configure JWT

From the `Backend/` folder, initialize User Secrets and add the JWT
settings:

``` powershell
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "GENERATE-A-LONG-RANDOM-SECRET-KEY"
dotnet user-secrets set "Jwt:Issuer" "EventTicketingAPI"
dotnet user-secrets set "Jwt:Audience" "EventTicketingBlazor"
```

### 3. Connect to MySQL through SSH

Open a separate PowerShell window and start the SSH tunnel:

``` powershell
ssh -p 10000 -L 10000:127.0.0.1:3306 linux-user@SECRET-SERVER-IP
```

Replace `linux-user` and `SECRET-SERVER-IP` with the database server's SSH
login details. Keep this PowerShell window open while using the
application.

Make sure the backend's MySQL connection string is configured to connect
through the local tunnel (typically `127.0.0.1:10000`).

## Database Initialization

If the database or schema needs to be created, connect to MySQL through
the SSH tunnel and run the SQL scripts in this order:

1.  `Users.sql`
2.  `Data.sql`

These scripts create/populate the database objects used by the project.
Run them against the correct database.

## Starting the Application

1.  Make sure the SSH tunnel is running.

2.  Open PowerShell in the repository's root folder.

3.  Run the build/start script:

    ``` powershell
    .\Build.ps1
    ```

4.  Open the frontend in your browser:

    **http://localhost:5079/**

If the application does not start, check that the SSH tunnel is
connected, the database settings are correct, and the required .NET
packages are restored.

## HTTP API

  ----------------------------------------------------------------------------------
  Method                  Endpoint                           Description
  ----------------------- ---------------------------------- -----------------------
  `POST`                  `/login`                           Authenticates a user
                                                             and returns a login
                                                             response/token.

  `POST`                  `/Purchase{event-id}+{quantity}`   Purchases the requested
                                                             quantity of tickets for
                                                             an event.

  `GET`                   `/search`                          Returns the list of
                                                             events.

  `GET`                   `/search/{id}`                     Returns details for one
                                                             event.

  `GET`                   `/organizer/dashboard`             Organizer dashboard
                                                             data.

  `GET`                   `/admin/dashboard`                 Admin dashboard data.
  
  ----------------------------------------------------------------------------------

## Demo Account

Use this sample account to test the application:

User: `joey`      
Password: `1234`
Role: `Customer`


## Known Limitations

-   The Admin and Customer workflows are still in progress.
-   Some features may be incomplete and may not be available for
    evaluation.
-   Running the application requires access to the configured MySQL
    server through the SSH tunnel.
