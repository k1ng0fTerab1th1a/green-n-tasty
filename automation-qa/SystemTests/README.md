# System Tests

System tests run against a real deployed API using HTTP requests and real infrastructure.

## Safety

- These tests are opt-in and are skipped by default.
- They require `RUN_SYSTEM_TESTS=true`.
- They refuse to run if `SYSTEM_BASE_URL` looks like production (contains `prod`).
- They create unique test users and attempt cleanup in Cognito and DynamoDB.

## Required environment variables

- `RUN_SYSTEM_TESTS=true`
- `SYSTEM_BASE_URL=https://your-test-api.example.com`
- `SYSTEM_AWS_REGION=eu-central-1`
- `SYSTEM_COGNITO_USER_POOL_ID=eu-central-1_XXXXXX`

## Optional environment variables

- `SYSTEM_TEST_PASSWORD=Pass12345!A`
- `SYSTEM_USERS_TABLE=Users`

## Run only system tests

```powershell
dotnet test .\SystemTests\Restaurant.SystemTests\Restaurant.SystemTests.csproj
```

