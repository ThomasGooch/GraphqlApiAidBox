# GraphqlApiAidBox

This is a .NET 9 Web API passthrough service for forwarding GraphQL requests to an Aidbox instance. It exposes a POST endpoint `/passthrough` that accepts a JSON body with `query` (string) and `variables` (object). The service supports both direct GraphQL queries and automatic query selection based on resource types.

## Features

- ✅ **Direct GraphQL Queries**: Send any GraphQL query with variable substitution
- ✅ **Automatic Query Selection**: Resource-based query file selection
- ✅ **Case-Insensitive Variables**: Handles mixed case in resource names and variables
- ✅ **Variable Interpolation**: Replace `{placeholder}` patterns in GraphQL queries
- ✅ **Comprehensive Testing**: HTTP files with organized test suites

## Configuration

### Aidbox Endpoint Secret
The Aidbox GraphQL endpoint URL is stored securely using [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets). To set it:

```pwsh
dotnet user-secrets set AidboxGraphqlUrl "https://your-aidbox-instance.com/$graphql"
```

> **Note:** Do not store secrets in `appsettings.json` for production.

### Query Files Path
GraphQL query files are stored in `src/Graphql/Queries/`. Override with:
```pwsh
dotnet user-secrets set QueriesPath "/custom/path/to/queries"
```

## Usage

### Direct Query Mode
POST `/passthrough`

```json
{
  "query": "query GetConsentDetails { ConsentList(_id: \"{id}\") { id status } }",
  "variables": { "id": "consent-123" }
}
```

### Resource-Based Query Selection
POST `/passthrough`

```json
{
  "Resource": "consent",
  "variables": { "id": "consent-123" }
}
```

**Automatic Query Selection Rules:**
- `Resource: "consent"` + `id` → `GetConsentDetailsQuery.graphql`
- `Resource: "documentreference"` + `subject` + `related` → `GetDocumentReferenceUrlQuery.graphql`
- `Resource: "provenance"` + `id` → `GetConsentHistoryQuery.graphql`
- Any other case → `GetAllConsentsQuery.graphql` (default)

### Variable Substitution
- Use `{placeholder}` patterns in GraphQL files
- Case-insensitive matching: `{id}`, `{ID}`, `{Id}` all work
- Variables: `"id"`, `"ID"`, `"Id"` all match placeholders

## Testing

### Quick Test Script
```bash
./test-api.sh
```

### HTTP Request Files
- **`GraphqlApiAidBox.http`**: Main test suite with organized test categories
- **`GraphqlApiAidBox.dev.http`**: Development and debugging tests
- **`HTTP_TESTING.md`**: Comprehensive testing documentation

### Running Individual Tests
Open HTTP files in VS Code with REST Client extension and click "Send Request".

## Run the API

```pwsh
dotnet run
```

The API will be available at `http://localhost:5225/passthrough` (or the port shown in the console).

## Project Structure

```
src/
├── Controllers/
│   └── PassthroughController.cs    # Main API controller
├── Graphql/
│   └── Queries/                    # GraphQL query files
│       ├── GetAllConsentsQuery.graphql
│       ├── GetConsentDetailsQuery.graphql
│       ├── GetConsentHistoryQuery.graphql
│       └── GetDocumentReferenceUrlQuery.graphql
└── README.md                       # This file

Tests/                              # Unit tests
GraphqlApiAidBox.http              # Main HTTP test suite
GraphqlApiAidBox.dev.http          # Development tests
HTTP_TESTING.md                    # Testing documentation
test-api.sh                        # Automated test script
```

## Example Scenarios

1. **Get consent details**: `Resource: "consent"` with `id`
2. **Get all consents**: No Resource or variables
3. **Get document references**: `Resource: "documentreference"` with `subject` and `related`
4. **Get consent history**: `Resource: "provenance"` with `id`
5. **Custom queries**: Direct GraphQL with any variables

See the HTTP files for ready-to-use request examples covering all scenarios.
