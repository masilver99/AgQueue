# GitHub Copilot Instructions for AgQueue

## Project Overview

AgQueue is a simple, opinionated, transactional work queue system built in C# with .NET 5. It transforms any supported database (currently SQLite and PostgreSQL) into a full-fledged work queue with advanced messaging capabilities.

### Key Features
- **Transactional**: Transactions are mandatory with expiry timespans and can be extended
- **Priority Support**: Messages returned by priority, then FIFO
- **Durable Storage**: Messages reliably stored to disk using SQLite/PostgreSQL
- **Multiple Queues**: Support for multiple named queues
- **Built-in Retry**: Automatic message retry with configurable delays
- **gRPC Communication**: Primary communication via gRPC server
- **Message Metadata**: Custom data storage for messages with tags and correlation IDs

## Architecture

### Project Structure
- **AgQueue.Server.Common**: Core internal API and storage interfaces
- **AgQueue.Common**: Client interfaces and common models
- **AgQueue.GrpcServer**: gRPC server implementation
- **AgQueue.Sqlite**: SQLite storage implementation
- **AgQueue.Postgres**: PostgreSQL storage implementation  
- **AgQueue.Models**: Shared data models and protobuf definitions
- **Benchmarks**: Performance benchmarking project
- **tests/**: Integration and unit tests

### Key Components
1. **InternalApi**: Core business logic for queue operations
2. **IStorage/IStorageTransaction**: Storage abstraction layer
3. **AgQueueService**: gRPC service implementation
4. **Transaction Management**: Two-level transaction system (queue + database)
5. **protobuf/gRPC**: Protocol definitions in `AgQueue.Models/AgQueue.proto`

### gRPC API Structure
The main service `QueueApi` provides these operations:
- **Queue Management**: CreateQueue, DeleteQueue, GetQueueInfo
- **Transaction Control**: StartTransaction, CommitTransaction, RollbackTransaction  
- **Message Operations**: QueueMessage, DequeueMessage, PeekMessage
- **Storage Management**: InitializeStorage

## Development Guidelines

### Code Standards
- Follow StyleCop Analyzers recommendations (configured in stylecop.json)
- **Zero warnings policy**: Pull requests must have no compiler warnings
- Use XML documentation (`///`) for all public methods and properties
- Company name: "Michael Silver" (configured in stylecop.json)
- Copyright: "Copyright (c) Michael Silver. All rights reserved."

### Coding Conventions
- Use int64 for primary keys (application-generated, not database-generated)
- Queue names are case sensitive
- Prefer explicit error handling over exceptions where appropriate
- Use `ResultCode` enum for internal API responses (maps to gRPC status codes)

### Key Patterns

#### Transaction Handling
```csharp
// Two types of transactions to be aware of:
// 1. Database transactions (IStorageTransaction) - for SQL atomicity
// 2. Queue transactions (stored in Transaction table) - for message handling
```

#### Storage Implementation
- Implement `IStorage` interface for new database providers
- Reference `StorageSqlite` class as the primary example
- Handle connection management and transaction lifecycle properly

#### Error Handling
- Use `ApiResult<T>` for internal API returns
- Map `ResultCode` to appropriate gRPC status codes
- Implement proper exception interceptors for gRPC calls

### Build and Testing
- Target Framework: .NET 5.0 (note: end-of-life, may require upgrade to newer .NET version)
- Build with: `dotnet build --configuration Release`
- Run tests: `dotnet test` (requires .NET 5.0 runtime)
- No warnings should be present in successful builds
- Integration tests use in-process gRPC server on port 10043
- Current build shows package vulnerabilities (MessagePack, Newtonsoft.Json, Npgsql)
- **Note**: Tests may fail on systems without .NET 5.0 runtime installed

### Dependencies and Packages
- **gRPC**: Primary communication protocol (.proto files in AgQueue.Models)
- **SQLite/PostgreSQL**: Supported database backends  
- **StyleCop.Analyzers**: Code analysis and formatting
- **MSTest**: Testing framework for integration tests
- **BenchmarkDotNet**: Performance benchmarking
- **MessagePack**: Serialization (note: has known vulnerability in current version)
- **Newtonsoft.Json**: JSON handling (note: has known vulnerability in current version)

## Common Development Tasks

### Adding New Queue Operations
1. Add method to `InternalApi` class
2. Implement corresponding gRPC service method in `AgQueueService`
3. Update storage interface (`IStorage`) if needed
4. Implement in storage providers (SQLite/Postgres)
5. Add integration tests
6. Update documentation

### Adding New Storage Provider
1. Create new project (e.g., `AgQueue.Redis`)
2. Implement `IStorage` and `IStorageTransaction` interfaces
3. Follow patterns from `StorageSqlite` implementation
4. Handle primary key generation consistently
5. Add appropriate error handling and resource disposal

### Message Workflows
1. **Add Message**: CreateQueue → StartTransaction → QueueMessage → CommitTransaction
2. **Pull Message**: StartTransaction → DequeueMessage → (Process) → CommitTransaction/RollbackTransaction  
3. **Peek Message**: PeekMessageByQueue/PeekMessageById (no transaction required)

### Message Properties
- **Payload**: Binary message data (bytes)
- **Priority**: Lower number = higher priority (int32)
- **MaxAttempts**: Retry limit before message expiration
- **ExpiryInMinutes**: Message lifetime (0 = no expiration)
- **CorrelationId**: Optional application-defined correlation (int32)
- **GroupName**: Optional grouping string
- **MetaData**: Optional descriptive string data

### Message States
- **Active**: Available for processing
- **InTransaction**: Currently being processed
- **Processed**: Successfully completed
- **Expired**: Exceeded expiry time
- **AttemptsExceeded**: Exceeded max retry attempts

### Testing Guidelines
- Use `[DoNotParallelize]` attribute for integration tests
- Initialize storage with `DeleteExistingData = true` for clean tests
- Clean up resources in `TestCleanup` methods
- Test both success and error scenarios

## Important Notes

### Transaction Complexity
The codebase has two transaction concepts that can be confusing:
- **Database transactions**: SQL-level atomicity (IStorageTransaction)
- **Queue transactions**: Business-level message handling (stored in Transaction table)

### Performance Considerations
- Int64 primary keys chosen over GUIDs for better database performance
- Single-process limitation due to application-generated primary keys
- SQLite chosen for durability over raw speed

### Migration Notes
- Project originally planned TCP and REST servers
- Consolidated to gRPC-only for better performance and feature completeness
- Some legacy documentation may reference old architecture

## StyleCop Configuration

The project uses StyleCop Analyzers with custom rules:
- SA1200 (using directives placement) is disabled
- SA1413 (trailing commas) is disabled
- Company name and copyright are automatically applied
- License type: MIT

Always ensure your changes conform to the configured StyleCop rules and maintain zero warnings.