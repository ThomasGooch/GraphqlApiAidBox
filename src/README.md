# GraphqlApiAidBox

This is a .NET 9 Web API passthrough service for forwarding GraphQL requests to an Aidbox instance. It exposes a POST endpoint `/passthrough` that accepts a JSON body with `query` (string) and `variables` (object). The service supports dynamic variable interpolation for hardcoded GraphQL arguments.

## Configuration

### Aidbox Endpoint Secret
The Aidbox GraphQL endpoint URL is stored securely using [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets). To set it:

```pwsh
dotnet user-secrets set AidboxGraphqlUrl "https://your-aidbox-instance.com/$graphql"
```

> **Note:** Do not store secrets in `appsettings.json` for production.

## Usage

POST `/passthrough`

Request body:
```json
{
  "query": "query { PatientList(given: \"{{given}}\", family: \"{{family}}\") { id name { family given } } }",
  "variables": { "given": "Zach", "family": "Brown" }
}
```

- Use `{{variable}}` placeholders in your query string. The API will replace them with values from the `variables` object before forwarding to Aidbox.

## Run the API

```pwsh
dotnet run
```

The API will be available at `http://localhost:5000/passthrough` (or the port shown in the console).

## Example Scenarios
- Get all patients by name using variables
- Gather all consents for all patients with a given name by chaining requests

See the `.http` file for ready-to-use request examples.
