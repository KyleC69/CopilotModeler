# DataModeler

## Overview

**DataModeler** is an AI-powered coding agent model builder designed to demonstrate the steps involved in the creation of your very own AI. It automates the extraction, analysis, and modeling of source code from GitHub repositories. The application orchestrates data mining, code analysis, database persistence, and machine learning model training, leveraging modern .NET technologies and cloud-based APIs.

**Key Features:** is designed to outline the architecture and functionality of the data mining and model training required to build models for a personal coding assistant and a stand alone or integrated chat interface. These models have been limited to Csharp code specifically to save data and model space. You can widen the scope of the data mining be setting a single property. If your coding expertise is in a certain area simply change the search term for the repositories you are mining for model training. 

---

## Features

- Automated retrieval of C# repositories from GitHub using the Octokit API.
- Static code analysis with Roslyn to extract AST, CFG, DFG, and code metrics.
- Data persistence using Entity Framework Core with SQL Server.
- Machine learning model training and evaluation using ML.NET.
- Configurable via `appsettings.json` and environment variables.
- Dependency injection and logging via Microsoft.Extensions libraries.

---

## Technology Stack

| Technology / Library                | Purpose                                              |
|-------------------------------------|------------------------------------------------------|
| **.NET 10**                         | Core runtime and language platform                   |
| **C# 13**                           | Primary programming language                         |
| **Microsoft.CodeAnalysis (Roslyn)** | Code parsing and static analysis                     |
| **Entity Framework Core**           | ORM for SQL Server persistence                       |
| **ML.NET**                          | Machine learning model training and evaluation       |
| **Octokit**                         | GitHub API client for repository mining              |
| **OpenAI**                          | (Optional) Integration for AI-powered features       |
| **Microsoft.Extensions.Hosting**    | Application hosting and dependency injection         |
| **Microsoft.Extensions.Logging**    | Structured logging                                   |
| **Microsoft.Extensions.Configuration** | Configuration management                        |

---

## Project Structure

- `Program.cs` – Main orchestration logic, DI setup, configuration, and workflow.
- `Data/` – Entity Framework Core data models and context.
- `DataExtraction/` – GitHub data mining and extraction logic.
- `Services/` – Code analysis and supporting services.
- `Training/` – ML.NET model training and evaluation logic.
- `appsettings.json` – Application configuration (connection strings, API keys, etc.).

---

## Getting Started

1. **Prerequisites**
   - .NET 10 SDK
   - SQL Server instance (local or remote)
   - GitHub Personal Access Token (PAT) for API access

2. **Configuration**
   - Update `appsettings.json` with your database connection string and GitHub PAT.
   - Optionally, set environment variables for sensitive data like API keys.
   - Application accepts a Command argument and optional parameters to control the mining and model training process.

   DataModeler supports the following commands:
1. `extract` - Start the data mining process.
    - 'searchTerm' - Specify the term to search for in GitHub repositories to narrow down the focus of the Model usage. This can be very specific and follows Github's search syntax. For example, to search for repositories related to "machine learning" in C#, you can use `searchTerm: "machine learning language:csharp"'. To limit by last updated date, you can use `searchTerm: "machine learning language:csharp pushed:>2023-01-01"`. See [GitHub Search Syntax](https://docs.github.com/en/search-github/searching-on-github/searching-issues-and-pull-requests#search-by-commit-date) for more details.
    - 'minStars' - Specify the minimum number of stars a repository must have to be included in the mining process.
    - 'reposPerPage' - Specify the number of repositories to retrieve per page from GitHub.
    -  'maxPages' - Specify the maximum number of pages to retrieve from GitHub.
2. `train` - Initiate model training using the mined data.
    - 'epochs' - Specify the number of training epochs for the model.          NOTE: Not implemented yet.
    - 'batchSize' - Specify the number of samples per gradient update.         NOTE: Not implemented yet.
    - 'learningRate' - Specify the learning rate for the training process.     NOTE: Not implemented yet.

3. **Build and Run**

1. **Build the application** using the .NET CLI:
   ```bash
   dotnet build
   ```
2. **Run the application** with the desired command, for example:
   ```bash
   dotnet run extract "machine learning" 1000 50 3
   ```
3. Check the logs for insights into the mining and training processes. Easily accessed via the console or configured logging providers. Can integrate with Azure Application Insights or other logging services.

---

## How It Works

1. **Configuration & DI Setup:**  
Loads settings from `appsettings.json` and environment variables. Sets up dependency injection for all services.

2. **Database Migration:**  
Ensures the SQL Server database is created and up-to-date with the latest schema.

3. **GitHub Mining:**  
Uses Octokit to search and retrieve C# repositories based on configurable criteria.

4. **Code Analysis:**  
Analyzes downloaded repositories using Roslyn to extract code features and metrics.

5. **Data Persistence:**  
Stores extracted data in SQL Server via Entity Framework Core.

6. **Model Training:**  
Trains machine learning models on the collected data using ML.NET.

---

## License

This project is licensed under the MIT License.

Summary of Technology Used:
•	.NET 10 and C# 13 for modern language features and performance.
•	Roslyn for deep static code analysis.
•	Entity Framework Core for robust data access.
•	ML.NET for integrated machine learning.
•	Octokit for seamless GitHub integration.
•	OpenAI for optional AI enhancements.
•	Microsoft.Extensions libraries for configuration, logging, and DI.
