# E-Commerce Microservices Architecture Case Study

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?style=flat&logo=rabbitmq)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=flat&logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker)

A production-grade, distributed e-commerce backend built with .NET Core, demonstrating advanced microservices patterns for consistency and reliability. This project serves as a comprehensive case study for handling complex distributed transaction scenarios.

> **Read in Turkish**: [Türkçe Oku](./README.tr.md)

## 🚀 Features & Patterns

*   **Microservices Architecture**: Separate loosely coupled services for `Order`, `Stock`, and `Notification`.
*   **Event-Driven Communication**: Asynchronous messaging using **MassTransit** over **RabbitMQ**.
*   **Data Consistency**:
    *   **Outbox Pattern**: Atomicity between database operations and message publishing.
    *   **Optimistic Concurrency**: Handling overselling scenarios using `RowVersion`.
    *   **Idempotency**: Preventing duplicate message processing with custom filters.
    *   **Database per Service**: Isolated data stores (PostgreSQL for transactional data: Order, Stock, Notification; MongoDB for logs).
    *   **Automated Global Logging**: MassTransit Filters (`MongoLogPublishFilter`, `MongoLogConsumeFilter`) automatically log every message with `RequestId` propagation, removing the need for manual logging.
    *   **Retry Policy**: Exponential backoff for handling optimistic concurrency conflicts in Stock.API.

## 🛠 Technology Stack

*   **Backend**: .NET 10.0 Web API
*   **Message Broker**: RabbitMQ (MassTransit Abstraction)
*   **Databases**: PostgreSQL (TF: Entity Framework Core), MongoDB (Logging)
*   **Containerization**: Docker & Docker Compose
*   **Testing**: xUnit, Moq (Unit & Integration Tests)

## 🏃 Getting Started

### Prerequisites
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)
*   [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (for local development/debugging)

### Running the Application (Easy Way)

The project includes a startup script to launch all infrastructure and services at once. We recommend killing duplicate ports first to avoid conflicts.

1.  **Clone the repository**
    ```bash
    git clone https://github.com/your-username/beymen-case-study.git
    cd beymen-case-study
    ```

2.  **Run the Startup Script**
    ```bash
    chmod +x run_services.sh kill_ports.sh
    ./kill_ports.sh && ./run_services.sh
    ```
    *This script will:*
    *   Start Docker containers (Postgres, Mongo, RabbitMQ)
    *   Launch Order, Stock, and Notification APIs
    *   Launch the React Client

3.  **Access the Application**
    *   Go to **[http://localhost:5173](http://localhost:5173)** to use the Client.

### 🔌 Endpoints & Swagger

| Service | Port | Swagger UI | Description |
| :--- | :--- | :--- | :--- |
| **Order API** | `5001` | [http://localhost:5001/swagger](http://localhost:5001/swagger) | Manages orders. POST `/api/orders` to create an order. |
| **Stock API** | `5002` | [http://localhost:5002/swagger](http://localhost:5002/swagger) | Manages stock reservations (Consumer). |
| **Notification API** | `5003` | [http://localhost:5003/swagger](http://localhost:5003/swagger) | Real-time notifications via SignalR. |
| **Client App** | `5173` | [http://localhost:5173](http://localhost:5173) | Frontend for manual testing. |

### 4. Manual Startup (Alternative)
If you prefer not to use the script:
    ```bash
    docker-compose up -d
    dotnet run --project src/Order.API --urls "http://localhost:5001"
    dotnet run --project src/Stock.API --urls "http://localhost:5002"
    dotnet run --project src/Notification.API --urls "http://localhost:5003"
    cd src/client && npm run dev
    ```

### 5. Client Application (Manual Testing)
The project includes a frontend client for manually managing and testing operations:
```bash
cd src/client
npm install
npm run dev
```
> **Note**: The client application is designed for manual interaction with the APIs to simulate user behaviors and edge cases.

### 🧪 Running Tests
To verify the system logic and consistency checks (e.g., Stock Concurrency):
```bash
dotnet test
```

## 🗺 Roadmap

- [x] **Core Services**: Order, Stock, Notification APIs implementation.
- [x] **Reliability**: Outbox, Idempotency, and Retry policies.
- [x] **Testing**: Initial Unit Tests for Stock Reservation logic.
- [ ] **API Gateway**: Implementing Ocelot or YARP for unified entry point.
- [ ] **Identity Server**: Centralized authentication/authorization.
- [ ] **Monitoring**: Integrating Prometheus & Grafana dashboards.

## 📄 License
This project is open-sourced under the MIT license.
