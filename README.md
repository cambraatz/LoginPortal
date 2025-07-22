# LoginPortal Microservice

## Overview

The `LoginPortal` microservice serves as the singular entry point for user authentication and authorization across our distributed microservices architecture (Delivery Manager, Admin Portal, etc.). It provides a secure login interface, validates user credentials against an internal database, and establishes robust, stateful user sessions to manage access, prevent data conflicts, and ensure a seamless user experience.

Unlike traditional stateless JWT implementations, this service incorporates a sophisticated session management system that provides fine-grained control over active user sessions, addressing common challenges in distributed environments.

## Key Features

* **Secure User Authentication:** Presents a username and password input field, validating credentials against a secure, internal user database.
* **JWT Token Generation & Management:** Upon successful authentication, the server generates industry-standard JSON Web Tokens (JWTs) (both access and refresh tokens) to facilitate secure and efficient authorization across microservices.
* **Secure Cookie Handling:** Access and refresh tokens are securely stored in HttpOnly cookies, ensuring persistence across your distributed microservices while mitigating client-side scripting attacks.
* **Comprehensive Session Management (Stateful JWT Enhancement):**
    * **SSO & Conflict Prevention:** Augments the stateless nature of JWTs by maintaining a dedicated `SESSIONS` database on the server. This database stores critical session data (login time, last activity, associated delivery data like Power Unit and Manifest Date).
    * **Data Integrity:** Prevents conflicting access or data corruption by ensuring only one authorized user can actively edit the same delivery manifest at a time.
    * **Automatic Session Invalidation:** Robust mechanisms are in place to automatically terminate sessions based on:
        * **Token Expiry:** JWT tokens have a defined lifetime.
        * **Idle Timeout:** Sessions that persist longer than a configured inactivity period (e.g., 30 minutes) are automatically invalidated by a background cleanup service.
        * **Invalid Signouts:** Addresses scenarios where users close their browser without formally logging out, preventing "lockouts" by abandoned sessions.
    * **Manual Session Termination:** Provides server-side control to administrators to manually invalidate and terminate specific user sessions.
* **Dynamic User Onboarding & Service Selection:** After successful login, users are presented with prompts to select the specific company and service they intend to use. These options are dynamically rendered based on the user's initialized permissions within the internal `USERS` table.
* **Admin Portal Integration:** User permissions, which dictate available company/service options, are exclusively controlled and managed through the `Admin Portal` by users with administrative privileges, ensuring centralized and secure access control.
* **Protection of Sensitive Assets:** Serves as a critical security layer, guarding access to sensitive business data residing in various backend databases.

## Technology Stack

* **Framework:** .NET 8
* **Language:** C#
* **Web Framework:** ASP.NET Core
* **Database:** SQL Server (for `SESSIONS` table and internal user database interactions)
* **Logging:** Serilog
* **Authentication:** JWT Bearer Authentication
* **Communication:** HTTP/HTTPS

## Architecture & Data Flow

1.  **User Credentials Input:** Frontend (e.g., Delivery Manager Portal, Admin Portal) sends username and password to `/v1/sessions/login`.
2.  **Credential Validation:** `LoginPortal` queries the internal user database (accessed via `IUserService`) to verify credentials.
3.  **JWT Generation:** On successful validation, `LoginPortal` generates a short-lived Access Token and a longer-lived Refresh Token.
4.  **Cookie Storage:** These tokens are embedded in secure HttpOnly cookies (`access_token`, `refresh_token`) and sent back to the client.
5.  **Session Database Update:** Concurrently, `LoginPortal` interacts with the `ISessionService` to add or update a record in the `dbo.SESSIONS` table. This record includes `USERNAME`, `ACCESSTOKEN`, `REFRESHTOKEN`, `EXPIRYTIME` (of the access token), `LOGINTIME`, `LASTACTIVITY`, and optionally `POWERUNIT` / `MFSTDATE` (if selected during login or subsequently updated).
6.  **Persistent Session:** For subsequent requests to any microservice, the browser automatically sends the secure cookies. The receiving microservice validates the JWT and, if necessary, interacts with the `LoginPortal` (e.g., for refresh token validation or session state checks like `check-manifest-access`).
7.  **Dynamic Prompting:** After initial login, the user is redirected to a page where `LoginPortal` fetches the user's permissions from the `USERS` table and dynamically renders available company/service options.
8.  **Continuous Session Management:** A background `SessionCleanupHostedService` periodically queries the `dbo.SESSIONS` table, removing sessions that are explicitly expired or have exceeded their idle timeout based on `LASTACTIVITY` timestamps. This also releases any associated `POWERUNIT` / `MFSTDATE` locks.

## Database Schema (SESSIONS Table)

The `LoginPortal` primarily interacts with the `dbo.SESSIONS` table for comprehensive session management. While internal user details are in a separate table (often shared or managed by the Admin Portal), the `SESSIONS` table is central to this service's core functionality.

```sql
-- Example structure of the dbo.SESSIONS table
CREATE TABLE dbo.SESSIONS (
    ID           INT IDENTITY(1,1) PRIMARY KEY,
    USERNAME     NVARCHAR(255) NOT NULL UNIQUE, -- Ensures one active session per user by username
    ACCESSTOKEN  NVARCHAR(MAX) NOT NULL,
    REFRESHTOKEN NVARCHAR(MAX) NOT NULL,
    EXPIRYTIME   DATETIME2     NOT NULL,      -- Expiration of the Access Token
    LOGINTIME    DATETIME2     NOT NULL,      -- When the user first logged in
    LASTACTIVITY DATETIME2     NOT NULL,      -- Timestamp of the last user interaction
    POWERUNIT    NVARCHAR(50)  NULL,          -- For resource locking
    MFSTDATE     DATE          NULL,          -- For resource locking
    -- IsActive BIT NOT NULL DEFAULT 1 -- Optional: if you prefer soft-deletes/marking inactive
);