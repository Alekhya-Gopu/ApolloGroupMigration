# Apollo Migration API

A C# Web API for migrating data between different models in Couchbase with proper layered architecture.

## Architecture

This project follows a clean architecture pattern with the following layers:

- **Controllers**: Handle HTTP requests, validation, and response formatting
- **Services**: Contain business logic and orchestrate data operations
- **Repositories**: Handle data access and Couchbase operations
- **Models**: Define data structures and conversion rules

## Features

- Dynamic data conversion between different models
- Batch processing with configurable batch sizes
- Dry run capability for testing conversions
- Comprehensive error handling and logging
- Validation using FluentValidation
- Swagger/OpenAPI documentation

## Configuration

Update `appsettings.json` with your Couchbase connection details:

```json
{
  "Couchbase": {
    "ConnectionString": "couchbase://localhost",
    "Username": "Administrator",
    "Password": "password",
    "BucketName": "default"
  }
}
```

## API Endpoints

### Convert Data
```
POST /api/datamigration/convert
```

Request body:
```json
{
  "sourceDocumentType": "UserV1",
  "targetDocumentType": "UserV2",
  "filterExpression": "Age > 18",
  "batchSize": 100,
  "dryRun": false
}
```

### Convert Single Document
```
POST /api/datamigration/convert/{documentId}?targetDocumentType=UserV2
```

### Get Available Conversion Rules
```
GET /api/datamigration/rules
```

### Validate Conversion Rule
```
GET /api/datamigration/validate?sourceType=UserV1&targetType=UserV2
```

## Adding New Conversion Rules

1. Create a new model class inheriting from `BaseModel`
2. Implement `IConversionRule<TSource, TTarget>` interface
3. Add the conversion logic in the `Convert` method
4. The rule will be automatically discovered and registered

Example:
```csharp
public class MyConversionRule : IConversionRule<SourceModel, TargetModel>
{
    public TargetModel Convert(SourceModel source)
    {
        // Your conversion logic here
        return new TargetModel { /* ... */ };
    }

    public bool CanConvert(BaseModel source)
    {
        return source is SourceModel;
    }

    public string GetTargetDocumentType()
    {
        return "TargetModel";
    }
}
```

## Running the Application

1. Install dependencies:
   ```bash
   dotnet restore
   ```

2. Update Couchbase configuration in `appsettings.json`

3. Run the application:
   ```bash
   dotnet run
   ```

4. Access Swagger UI at: `https://localhost:5001/swagger`

## Error Handling

The API provides comprehensive error handling:
- Input validation errors return 400 Bad Request
- Business logic errors return 400 Bad Request with details
- System errors return 500 Internal Server Error
- All errors are logged for debugging

## Logging

The application uses structured logging with different levels:
- Information: Normal operations and progress
- Warning: Non-critical issues
- Error: Exceptions and failures
- Debug: Detailed debugging information
