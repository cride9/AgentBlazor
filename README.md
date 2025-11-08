# AgentBlazor (EVA Agent)

**AgentBlazor** is a web-based AI assistant, "EVA", built with Blazor and the .NET AI Agent framework. Unlike traditional chatbots, EVA is an **action-oriented agent** that can perform real-world tasks like creating files, running terminal commands, searching the web, and writing code—all through natural conversation and within a secure, sandboxed environment.

**Repo Link**: `https://github.com/cride9/AgentBlazor`

---

## ✨ Features

-   **AI Agent with Tools**: Interacts with the local file system (read, write, list) and executes shell commands.
-   **Web Capabilities**: Performs Google searches and scrapes content from web pages and PDFs.
-   **Stateful Conversations**: Chat history and agent memory are saved to a database, allowing sessions to be resumed.
-   **Real-time Interaction**: Agent responses are streamed token-by-token.
-   **Transparent Tool Usage**: The UI clearly shows which tools the agent is using and their real-time status.
-   **Modern UI**: A responsive interface built with Blazor and styled with Tailwind CSS, featuring dark/light modes and markdown rendering.
-   **Sandboxed Sessions**: Each agent session operates in its own isolated directory for safety.

## 🛠️ Tech Stack

-   **Backend**: .NET 9, ASP.NET Core, Entity Framework Core
-   **AI**: `Microsoft.Agents.AI`, `Microsoft.Extensions.AI`
-   **Frontend**: Blazor Server, Tailwind CSS
-   **Database**: SQL Server (MS-SQL)
-   **LLM Backend**: OpenAI-compatible API (configured for Ollama by default)

---

## 🚀 Getting Started

Follow these steps to set up and run the project locally.

### Prerequisites

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Node.js and npm](https://nodejs.org/)
-   [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Express, LocalDB, or other)
-   [Ollama](https://ollama.com/) (or another OpenAI-compatible LLM server) running locally.

### 1. Clone the Repository

```bash
git clone https://github.com/cride9/AgentBlazor.git
cd AgentBlazor
```

### 2. Configure Environment Variables

You need to set up API keys for the web search functionality. You can set these as system environment variables or create a `.env` file in the project root.

```
# .env file content
google_api="YOUR_GOOGLE_CUSTOM_SEARCH_API_KEY"
google_engine="YOUR_GOOGLE_PROGRAMMABLE_SEARCH_ENGINE_ID"
DEEPSEEK="YOUR_DEEPSEEK_API_KEY" # or any other API key you want to use, make sure you update the code to use your ENV name 
```
> **Note**: The api key value can be any non-empty string (e.g., "ollama") when using a local Ollama instance that doesn't require an API key.

### 3. Set up the Database

1.  Open `appsettings.json` and update the `DefaultConnection` string to point to your SQL Server instance.
2.  Run the Entity Framework migrations to create the database schema:
    ```bash
    dotnet ef database update
    ```

### 4. Install Frontend Dependencies

Install the required npm packages for Tailwind CSS.

```bash
npm install
```

### 5. Run the Application

You'll need two terminal windows for development.

1.  **In the first terminal**, start the Tailwind CSS compiler in watch mode:
    ```bash
    npm run css
    ```

2.  **In the second terminal**, run the Blazor application:
    ```bash
    dotnet run
    ```

### 6. Set up the LLM

1.  Ensure your Ollama server is running.
2.  Pull the required model specified in `Program.cs` (or change it to one you have):
    ```bash
    ollama pull qwen3:4b-instruct-2507-q8_0
    ```
3.  Open the application in your browser (e.g., `https://localhost:####`) and start chatting with EVA