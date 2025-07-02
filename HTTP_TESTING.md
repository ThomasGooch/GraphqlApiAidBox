# HTTP Request Testing Guide

This project includes comprehensive HTTP request files for testing the GraphQL API passthrough functionality.

## Files Overview

### `GraphqlApiAidBox.http`
**Main test suite** - Contains organized test cases covering all functionality:

- **Error Handling Tests**: Invalid resources, missing files
- **Resource-Based Query Selection**: Tests automatic query selection based on Resource type
- **Direct Query Tests**: Tests for direct GraphQL query submission
- **Edge Cases**: Empty/null values, missing variables
- **Case Sensitivity Tests**: Mixed case resource names and variables

### `GraphqlApiAidBox.dev.http`
**Development and debugging** - Contains tests for development scenarios:

- **Health Checks**: Basic API availability tests
- **Performance Testing**: Large payloads, multiple variables
- **Error Simulation**: Malformed requests, invalid data types
- **Configuration Testing**: Different environments, authentication

## How to Use

### Prerequisites
1. Start the API server: `dotnet run` (from the `src` directory)
2. Ensure Aidbox is running (if testing with real backend)
3. Open HTTP files in VS Code with REST Client extension

### Running Tests

1. **Individual Tests**: Click "Send Request" above any `###` section
2. **All Tests**: Use "Send All Requests" from VS Code command palette
3. **Environment Variables**: Modify `@GraphqlApiAidBox_HostAddress` at the top of files

### Test Categories Explained

#### Resource-Based Query Selection
These tests verify that the API correctly selects GraphQL query files based on the `Resource` field:

- `consent` + `id` → `GetConsentDetailsQuery.graphql`
- `documentreference` + `subject` + `related` → `GetDocumentReferenceUrlQuery.graphql`
- `provenance` + `id` → `GetConsentHistoryQuery.graphql`
- No match → `GetAllConsentsQuery.graphql` (default)

#### Direct Query Tests
These tests follow the API specification for accepting direct GraphQL queries:

```json
{
  "query": "query GetConsentDetails { ConsentList(_id: \"{id}\") { id } }",
  "variables": { "id": "test-123" }
}
```

The API will substitute `{id}` with `test-123` before sending to the backend.

#### Case Sensitivity Features
The API handles various casing scenarios:

- **Resource names**: `"consent"`, `"CONSENT"`, `"ConSenT"` all work
- **Variable names**: `"id"`, `"ID"`, `"Id"` all work
- **Placeholder matching**: `{id}`, `{ID}`, `{Id}` in GraphQL files all match

## Expected Responses

### Successful Response
```json
{
  "data": {
    "ConsentList": [
      {
        "id": "consent-123",
        "status": "active"
      }
    ]
  }
}
```

### Error Response (404)
```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Query file 'DoesNotExist.graphql' not found."
}
```

### Backend Error (when Aidbox is unavailable)
```json
{
  "errors": [
    {
      "message": "Connection refused"
    }
  ]
}
```

## Configuration

### Environment Variables
- `AidboxGraphqlUrl`: Backend GraphQL endpoint (default: configured in appsettings)
- `QueriesPath`: Path to GraphQL query files (default: `Graphql/Queries`)

### Customizing Host Address
Change the host address at the top of any HTTP file:
```
@GraphqlApiAidBox_HostAddress = http://localhost:5225
```

## Troubleshooting

### Common Issues

1. **404 Errors for valid resources**
   - Check that corresponding `.graphql` files exist in `src/Graphql/Queries/`
   - Verify file naming matches expected pattern (e.g., `GetConsentDetailsQuery.graphql`)

2. **Variables not substituting**
   - Ensure variable names match placeholders in GraphQL files
   - Check for typos in variable names (case-insensitive matching should help)

3. **Connection errors**
   - Verify API is running: `dotnet run`
   - Check host address matches running port
   - Ensure Aidbox backend is accessible (if using real backend)

### Debug Mode
Enable detailed logging by setting environment variable:
```bash
export ASPNETCORE_ENVIRONMENT=Development
```

This will provide more detailed error messages and request/response logging.

## Adding New Tests

When adding new GraphQL query files or functionality:

1. Add corresponding test cases to `GraphqlApiAidBox.http`
2. Include both positive and negative test cases
3. Test with various casing scenarios
4. Add edge cases to verify error handling
5. Update this documentation if new features are added

## Integration with CI/CD

These HTTP files can be automated using tools like:
- Newman (Postman CLI)
- REST Client CLI
- Custom scripts using curl/httpie

Example curl equivalent for a test:
```bash
curl -X POST http://localhost:5225/passthrough \
  -H "Content-Type: application/json" \
  -d '{"Resource": "consent", "variables": {"id": "test-123"}}'
```
