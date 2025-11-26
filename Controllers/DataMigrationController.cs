using Microsoft.AspNetCore.Mvc;
using ApolloMigration.Models;
using ApolloMigration.Services;
using FluentValidation;

namespace ApolloMigration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataMigrationController : ControllerBase
{
    private readonly IDataMigrationService _migrationService;
    private readonly ILogger<DataMigrationController> _logger;

    public DataMigrationController(
        IDataMigrationService migrationService, 
        ILogger<DataMigrationController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    /// <summary>
    /// Convert data from one model to another
    /// </summary>
    /// <param name="request">Conversion request parameters</param>
    /// <returns>Conversion result</returns>
    [HttpPost("convert")]
    public async Task<ActionResult<ConversionResponse>> ConvertData()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Received conversion request: {SourceType} -> {TargetType}");

            var result = await _migrationService.ConvertDataAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing conversion request");
            return StatusCode(500, new ConversionResponse
            {
                Success = false,
                Message = "Internal server error occurred during conversion",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Convert a single document by ID
    /// </summary>
    /// <param name="documentId">ID of the document to convert</param>
    /// <param name="targetDocumentType">Target document type</param>
    /// <returns>Conversion result</returns>
    [HttpPost("convert/{documentId}")]
    public async Task<ActionResult<ConversionResponse>> ConvertSingleDocument(
        string documentId, 
        [FromQuery] string targetDocumentType)
    {
        try
        {
            if (string.IsNullOrEmpty(documentId) || string.IsNullOrEmpty(targetDocumentType))
            {
                return BadRequest("Document ID and target document type are required");
            }

            _logger.LogInformation("Converting single document {DocumentId} to {TargetType}", 
                documentId, targetDocumentType);

            var result = await _migrationService.ConvertSingleDocumentAsync(documentId, targetDocumentType);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting single document {DocumentId}", documentId);
            return StatusCode(500, new ConversionResponse
            {
                Success = false,
                Message = "Internal server error occurred during conversion",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get available conversion rules
    /// </summary>
    /// <returns>List of available conversion rules</returns>
    [HttpGet("rules")]
    public async Task<ActionResult<IEnumerable<object>>> GetConversionRules()
    {
        try
        {
            var rules = await _migrationService.GetAvailableConversionRules();
            var ruleInfo = rules.Select(rule => new
            {
                RuleType = rule.GetType().Name,
                TargetDocumentType = rule.GetTargetDocumentType()
            });

            return Ok(ruleInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversion rules");
            return StatusCode(500, "Internal server error occurred while retrieving conversion rules");
        }
    }

    /// <summary>
    /// Validate if a conversion rule exists
    /// </summary>
    /// <param name="sourceType">Source document type</param>
    /// <param name="targetType">Target document type</param>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    public async Task<ActionResult<bool>> ValidateConversionRule(
        [FromQuery] string sourceType, 
        [FromQuery] string targetType)
    {
        try
        {
            if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            {
                return BadRequest("Source type and target type are required");
            }

            var isValid = await _migrationService.ValidateConversionRule(sourceType, targetType);
            return Ok(new { IsValid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating conversion rule");
            return StatusCode(500, "Internal server error occurred while validating conversion rule");
        }
    }
}
