using System.ComponentModel.DataAnnotations;

namespace ApolloMigration.Models;

public class ConversionRequest
{
    [Required]
    public string SourceDocumentType { get; set; } = string.Empty;
    
    [Required]
    public string TargetDocumentType { get; set; } = string.Empty;
    
    public string? FilterExpression { get; set; }
    
    public int BatchSize { get; set; } = 100;
    
    public bool DryRun { get; set; } = false;
}
