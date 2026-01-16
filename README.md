# Photo Storage App - Backend 🖼️⚡

![C#](https://img.shields.io/badge/C%23-239120?style=flat&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=flat&logo=dotnet&logoColor=white)
![Azure](https://img.shields.io/badge/Azure-0078D4?style=flat&logo=microsoft-azure&logoColor=white)
![Cosmos DB](https://img.shields.io/badge/CosmosDB-0089D6?style=flat&logo=microsoftazure&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=flat&logo=json-web-tokens&logoColor=white)

This is the **backend** for the Photo Storage App, built with **C# .NET**. It handles:

- User authentication (JWT-based)  
- Photo uploads, retrieval, and management  
- Azure Cosmos DB integration  
- R2 cloud storage for photos  
- Email notifications via Mailersend  

It’s designed to work seamlessly with the **frontend app** for a full photo storage solution.

---

## Features ✨

- User registration & login  
- Secure JWT authentication  
- Photo upload/download & storage  
- Database operations with Azure Cosmos DB  
- Cloud storage with R2  
- Email notifications for user activity  
- Image processing with **ImageSharp**

---

## Tech Stack & Packages 🛠️

- **Language:** C#  
- **Framework:** .NET Web Application  
- **Database:** Azure Cosmos DB  
- **Image Processing:** SixLabors.ImageSharp  
- **Cloud Storage:** R2  
- **Email:** Mailersend  

**NuGet Packages Required:**

```bash
dotnet add package dotenv.net
dotnet add package Azure.Cosmos
dotnet add package SixLabors.ImageSharp
dotnet add package Azure.Core
```
---

## Environment Variables

Create a `.env` file in the root directory with the following variables:

```env
# Cosmos DB
COSMOS_DB_ENDPOINT=
COSMOS_DB_KEY=
COSMOS_DB_DATABASE_ID=
COSMOS_USERS_CONTAINER=
COSMOS_PHOTOS_CONTAINER=

# JWT Authentication
JWT_SECRET=

# R2 Cloud Storage
R2_API_TOKEN=
R2_BUCKET_NAME=
R2_SERVICE_URL=
R2_SECRET_KEY=
R2_ACCESS_KEY=

# Email Notifications
MAILERSEND_API_KEY=
MAILERSEND_FROM_EMAIL=
MAILERSEND_TO_EMAIL=
```
# Install required packages via NuGet:
```
dotnet add package dotenv.net
dotnet add package Azure.Cosmos
dotnet add package SixLabors.ImageSharp
dotnet add package Azure.Core
```
# Build and run the project:
```
dotnet build
dotnet run
```

# License
This project is open-source under the MIT License.
