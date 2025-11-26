using ApolloMigration.Models;

namespace ApolloMigration.Services;

public interface IDataMigrationService
{
    Task<ConversionResponse> ConvertDataAsync();
    Task<ConversionResponse> ConvertSingleDocumentAsync(string documentId, string targetDocumentType);
    Task<IEnumerable<IConversionRule>> GetAvailableConversionRules();
    Task<bool> ValidateConversionRule(string sourceType, string targetType);
}
