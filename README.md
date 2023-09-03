# Wallet Service

## Overview
This service is built using .NET 6.0 and offers an API for managing wallets, including operations like wallet creation, updating, and retrieval. 

## Features

- **Retry Policy for Concurrency Exception**: Uses a Polly-based retry policy for handling concurrency exceptions with 3 hardcoded retries.
  
- **Entity Framework Core with Fluent API**: Utilizes Entity Framework Core and its Fluent API for data access.
  
- **Unit Testing using xUnit**: Unit tests are written under the `WalletService.UnitTest` project using xUnit.
  
- **Integration Testing using SpecFlow**: Integration tests are defined in the `WalletService.IntegrationTest` project using SpecFlow.

- **Health Checks**: Includes health checks that can be accessed for system status.

## Handling Add and Remove Funds Operations
The operation for adding or removing funds from a wallet is determined by the value of the `Amount` in the update request. 

- If the `Amount` value is positive (`+ve`), funds will be added to the wallet.
- If the `Amount` value is negative (`-ve`), funds will be removed from the wallet.

For example, to add $50 to a wallet, send an `UpdateWalletRequest` with an `Amount` of `50`. To remove $50, send an `UpdateWalletRequest` with an `Amount` of `-50`.
## Pre-requisites

- .NET 6.0 SDK
- SQL Server (LocalDB or standalone SQL Server instance)

## Database Configuration
1. Update `appsettings.json` or `appsettings.Development.json` with your SQL Server information.
  
#### Using LocalDB
To use LocalDB, simply update the server name in the connection string. The default LocalDB instance name should suffice in most cases.

## Running the Project

1. Restore NuGet packages:
    ```
    dotnet restore
    ```
  
2. Build the solution:
    ```
    dotnet build
    ```
  
3. Run the project:
    ```
    dotnet run
    ```
  
You should now be able to access the API at `https://localhost:7221/`.
- If you want a Swagger UI, you can access it through `https://localhost:7221/swagger/index.html`.
- If you want to check the health of the API, you can hit this endpoint `https://localhost:7221/health`.

## Testing

The solution contains two test projects:
  
- **WalletService.UnitTest**
- **WalletService.IntegrationTest**
  
Update the `appsettings.Test.json` in these projects with your test database connection details and run the tests:

1. Navigate to the test project directory:
    ```
    cd WalletService.UnitTest (or WalletService.IntegrationTest)
    ```
  
2. Run the tests:
    ```
    dotnet test
    ```

**Note**: The retry count for handling `DbUpdateConcurrencyException` is hardcoded to 3 retries.

---