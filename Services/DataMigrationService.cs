using ApolloMigration.Models;
using ApolloMigration.Repositories;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json;

namespace ApolloMigration.Services;

public class DataMigrationService : IDataMigrationService
{
    private readonly IDataRepository _repository;
    private readonly ILogger<DataMigrationService> _logger;
    private readonly Dictionary<string, IConversionRule> _conversionRules;

    public DataMigrationService(IDataRepository repository, ILogger<DataMigrationService> logger)
    {
        _repository = repository;
        _logger = logger;
        _conversionRules = LoadConversionRules();
    }

    public async Task<ConversionResponse> ConvertDataAsync()
    {
        var startTime = DateTime.UtcNow;
        var response = new ConversionResponse();
        var errors = new List<string>();

        try
        {
            _logger.LogInformation("Starting data conversion from Bookings collection");

            // Get total count for progress tracking
            var totalCount = await _repository.GetCountAsync();
            _logger.LogInformation("Found {Count} documents to convert", totalCount);

            if (totalCount == 0)
            {
                response.Success = true;
                response.Message = "No documents found to convert";
                response.ProcessingTime = DateTime.UtcNow - startTime;
                return response;
            }

            // Process in batches
            var processedCount = 0;
            var errorCount = 0;
            var offset = 0;
            var batchSize = 15;

            while (offset < totalCount)
            {
                var batch = await GetBatchAsync(offset, batchSize);

                try
                {
                    await ConvertGroupDocument(batch);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    var errorMessage = $"Error converting document {ex.Message}";
                    errors.Add(errorMessage);
                    _logger.LogError(ex, "Error converting document batch");
                }

                offset += batchSize;
                _logger.LogInformation("Processed {Processed}/{Total} documents", processedCount, totalCount);
            }

            response.Success = errorCount == 0;
            response.ProcessedCount = processedCount;
            response.ErrorCount = errorCount;
            response.Errors = errors;
            //response.Message = request.DryRun 
            //    ? $"Dry run completed. Would convert {processedCount} documents with {errorCount} errors."
            //    : $"Conversion completed. Processed {processedCount} documents with {errorCount} errors.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data conversion");
            response.Success = false;
            response.Message = $"Conversion failed: {ex.Message}";
            errors.Add(ex.Message);
        }
        finally
        {
            response.ProcessingTime = DateTime.UtcNow - startTime;
        }

        return response;
    }

    public async Task<ConversionResponse> ConvertSingleDocumentAsync(string documentId, string targetDocumentType)
    {
        var startTime = DateTime.UtcNow;
        var response = new ConversionResponse();

        try
        {
            var document = await _repository.GetByIdAsync<BaseModel>(documentId);
            if (document == null)
            {
                response.Success = false;
                response.Message = $"Document with id {documentId} not found";
                return response;
            }

            await ConvertSingleDocumentInternalAsync(document, targetDocumentType);
            
            response.Success = true;
            response.Message = "Document converted successfully";
            response.ProcessedCount = 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting single document {DocumentId}", documentId);
            response.Success = false;
            response.Message = $"Conversion failed: {ex.Message}";
            response.Errors.Add(ex.Message);
        }
        finally
        {
            response.ProcessingTime = DateTime.UtcNow - startTime;
        }

        return response;
    }

    public async Task<IEnumerable<IConversionRule>> GetAvailableConversionRules()
    {
        return await Task.FromResult(_conversionRules.Values);
    }

    public async Task<bool> ValidateConversionRule(string sourceType, string targetType)
    {
        var ruleKey = $"{sourceType}->{targetType}";
        return await Task.FromResult(_conversionRules.ContainsKey(ruleKey));
    }

    private async Task<IEnumerable<Dictionary<string, object>>> GetBatchAsync(int offset, int limit)
    {
        // This is a simplified implementation. In a real scenario, you might need to implement
        // proper pagination with Couchbase N1QL queries
        var allDocuments = await _repository.GetAllAsync(offset, limit);
        return allDocuments;
    }

    private async Task ConvertSingleDocumentInternalAsync(BaseModel sourceDocument, string targetDocumentType)
    {
        var ruleKey = $"{sourceDocument.DocumentType}->{targetDocumentType}";
        
        if (!_conversionRules.TryGetValue(ruleKey, out var rule))
        {
            throw new InvalidOperationException($"No conversion rule found for {ruleKey}");
        }

        var convertedDocument = rule.Convert(sourceDocument);
        convertedDocument.DocumentType = targetDocumentType;
        convertedDocument.Id = sourceDocument.Id; // Preserve the original ID

        await _repository.UpdateAsync(convertedDocument);
    }

    private async Task ConvertGroupDocument(IEnumerable<Dictionary<string, object>> records)
    {
        foreach (var record in records)
        {
            try
            {
                Booking convertedItem = new Booking()
                {
                    Id = record.ContainsKey("id") ? Convert.ToUInt32(record["id"]) : 0,
                    Status = record.ContainsKey("status") ? Enum.Parse<BookingStatus>(record["status"]?.ToString() ?? "REQUEST") : BookingStatus.REQUEST,
                    UpdatedBy = record.ContainsKey("updatedBy") ? record["updatedBy"]?.ToString() ?? string.Empty : string.Empty,
                    UpdateTime = DateTime.UtcNow,
                    CreatedBy = record.ContainsKey("createdBy") ? record["createdBy"]?.ToString() ?? string.Empty : string.Empty,
                    CreateTime = DateTime.UtcNow,
                    Salary = record.ContainsKey("salary") ? Convert.ToDecimal(record["salary"]) : 0,
                    GroupId = record.ContainsKey("groupId") ? record["groupId"]?.ToString() ?? string.Empty : string.Empty,
                    GrossRoomPrice = record.ContainsKey("grossRoomPrice") ? Convert.ToDecimal(record["grossRoomPrice"]) : 0,
                    NetRoomPrice = record.ContainsKey("netRoomPrice") ? Convert.ToDecimal(record["netRoomPrice"]) : 0,
                    SegmentId = record.ContainsKey("segmentId") ? Convert.ToUInt32(record["segmentId"]) : 0,
                    SubSegmentId = record.ContainsKey("subSegmentId") ? Convert.ToUInt32(record["subSegmentId"]) : 0,
                    Users = DeserializeList<BookingUser>(record, "users"),
                    Passengers = DeserializeList<Passenger>(record, "passengers"),
                    Products = record.ContainsKey("products") ? 
                        (record["products"] is List<Product> products ? products : new List<Product>()) : 
                        new List<Product>(),
                    Notes = DeserializeList<BookingNote>(record, "notes"),
                    History = DeserializeList<HistoryNote>(record, "history"),
                    FlightDetails = DeserializeObject<FlightDetails>(record, "flightDetails") ?? new FlightDetails(),
                    CcTransactionData = DeserializeObject<CcTransactionData>(record, "ccTransactionData") ?? new CcTransactionData(),
                    SubsidyComment = record.ContainsKey("subsidyComment") ? record["subsidyComment"]?.ToString() ?? string.Empty : string.Empty,
                    ExternalOrderId = record.ContainsKey("externalOrderId") ? record["externalOrderId"]?.ToString() ?? string.Empty : string.Empty,
                    Period = record.ContainsKey("period") ? record["period"]?.ToString() ?? string.Empty : string.Empty,
                    Category = record.ContainsKey("category") ? record["category"]?.ToString() ?? string.Empty : string.Empty,
                    Tmura = record.ContainsKey("tmura") ? record["tmura"] : null,
                    CcPayments = record.ContainsKey("ccPayments") ? Convert.ToInt32(record["ccPayments"]) : 0,
                    SalaryPayments = record.ContainsKey("salaryPayments") ? Convert.ToInt32(record["salaryPayments"]) : 0,
                    BookingTags = record.ContainsKey("bookingTags") ? (List<string>)record["bookingTags"] : new List<string>(),
                    PeriodEntitledDays = record.ContainsKey("periodEntitledDays") ? Convert.ToUInt32(record["periodEntitledDays"]) : 0,
                };

                Console.WriteLine(convertedItem);

                List<Product> bookingProducts = new List<Product>();
                Product hotelProduct = new Product();
                hotelProduct.Name = string.Empty;
                hotelProduct.ProductDetails = new HotelDetails()
                {
                    Type = ProductTypeEnum.Hotel,
                    AtlantisHotelId = record.ContainsKey("hotelId") ? Convert.ToUInt32(record["hotelId"]) : 0,
                    Pax = record.ContainsKey("pax") && record["pax"] != null ? record["pax"] : new object(),
                    Start = record.ContainsKey("start") ? ParseDateOnlyOrDefault(record["start"]) : default,
                    End = record.ContainsKey("end") ? ParseDateOnlyOrDefault(record["end"]) : default,
                    RoomLabel = record.ContainsKey("roomLabel") ? record["roomLabel"]?.ToString() ?? string.Empty : string.Empty
                };
                Console.WriteLine(record.ContainsKey("pax") && record["pax"] != null ? record["pax"] : new object());
                hotelProduct.ProductDetails.Type = ProductTypeEnum.Hotel;
                bookingProducts.Add(hotelProduct);

                List<Activities> userActivities = new List<Activities>();

                foreach (var user in convertedItem.Users)
                {
                    userActivities = user.Activities != null ?
                        user.Activities : new List<Activities>();
                    userActivities.ForEach(activity =>
                    {
                        Product activityProduct = new Product();
                        activityProduct.Name = activity.Activity;
                        activityProduct.ProductDetails = new Activities()
                        {
                            Activity = activity.Activity,
                            Option = activity.Option,
                            Price = activity.Price,
                            Type = ProductTypeEnum.Activity,
                            UserKey = user.Key,
                            PassengerKey = ""
                        };
                        bookingProducts.Add(activityProduct);
                    });
                }

                foreach (var user in convertedItem.Passengers)
                {
                    userActivities = user.Activities != null ?
                        user.Activities : new List<Activities>();
                    
                    // Generate UUID v4 for UserKey if it's empty
                    var userKey = string.IsNullOrEmpty(user.Key) 
                        ? Guid.NewGuid().ToString() 
                        : user.Key;
                    user.Key = userKey;
                    userActivities.ForEach(activity =>
                    {
                        Product activityProduct = new Product();
                        activityProduct.Name = activity.Activity;
                        activityProduct.ProductDetails = new Activities()
                        {
                            Activity = activity.Activity,
                            Option = activity.Option,
                            Price = activity.Price,
                            Type = ProductTypeEnum.Activity,
                            UserKey = user.UserKey,
                            PassengerKey = user.Key
                        };
                        bookingProducts.Add(activityProduct);
                    });
                }

                // Remove Activities from users after extracting them to products
                foreach (var user in convertedItem.Users)
                {
                    user.Activities = new List<Activities>();
                }

                // Remove Activities from passengers after extracting them to products
                foreach (var passenger in convertedItem.Passengers)
                {
                    passenger.Activities = new List<Activities>();
                }

                convertedItem.Products = bookingProducts;

                Console.WriteLine(convertedItem);
                // Save the converted booking to Bookings3 collection
                await _repository.CreateBooking3Async(convertedItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting booking record: {Record}", string.Join(", ", record.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                throw;
            }
        }
    }

    private static List<T> DeserializeList<T>(Dictionary<string, object> record, string key)
    {
        if (!record.ContainsKey(key) || record[key] == null)
        {
            return new List<T>();
        }

        try
        {
            // If it's already the correct type, return it
            if (record[key] is List<T> typedList)
            {
                return typedList;
            }

            // Try to deserialize from JSON string
            if (record[key] is string jsonString)
            {
                var deserialized = JsonConvert.DeserializeObject<List<T>>(jsonString);
                return deserialized ?? new List<T>();
            }

            // If it's a list of dictionaries, deserialize each item
            if (record[key] is List<object> objectList)
            {
                var result = new List<T>();
                foreach (var item in objectList)
                {
                    if (item is T typedItem)
                    {
                        result.Add(typedItem);
                    }
                    else if (item is Dictionary<string, object> dict)
                    {
                        var json = JsonConvert.SerializeObject(dict);
                        var deserialized = JsonConvert.DeserializeObject<T>(json);
                        if (deserialized != null)
                        {
                            result.Add(deserialized);
                        }
                    }
                    else
                    {
                        var json = JsonConvert.SerializeObject(item);
                        var deserialized = JsonConvert.DeserializeObject<T>(json);
                        if (deserialized != null)
                        {
                            result.Add(deserialized);
                        }
                    }
                }
                return result;
            }

            // Try to serialize and deserialize as a last resort
            var serialized = JsonConvert.SerializeObject(record[key]);
            var deserializedList = JsonConvert.DeserializeObject<List<T>>(serialized);
            return deserializedList ?? new List<T>();
        }
        catch (Exception)
        {
            return new List<T>();
        }
    }

    private static T? DeserializeObject<T>(Dictionary<string, object> record, string key) where T : class
    {
        if (!record.ContainsKey(key) || record[key] == null)
        {
            return null;
        }

        try
        {
            // If it's already the correct type, return it
            if (record[key] is T typedObject)
            {
                return typedObject;
            }

            // Try to deserialize from JSON string
            if (record[key] is string jsonString)
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }

            // If it's a dictionary, serialize and deserialize
            if (record[key] is Dictionary<string, object> dict)
            {
                var json = JsonConvert.SerializeObject(dict);
                return JsonConvert.DeserializeObject<T>(json);
            }

            // Try to serialize and deserialize as a last resort
            var serialized = JsonConvert.SerializeObject(record[key]);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static DateOnly ParseDateOnlyOrDefault(object? value)
    {
        if (value == null)
        {
            return default;
        }

        switch (value)
        {
            case DateOnly d:
                return d;
            case DateTime dt:
                return DateOnly.FromDateTime(dt);
            case long unix:
                // Interpret as Unix seconds when value looks like seconds, or milliseconds if too large
                try
                {
                    var asSeconds = unix > 10_000_000_000 ? unix / 1000 : unix;
                    var dateTime = DateTimeOffset.FromUnixTimeSeconds(asSeconds).UtcDateTime;
                    return DateOnly.FromDateTime(dateTime);
                }
                catch
                {
                    return default;
                }
            case string s:
                if (string.IsNullOrWhiteSpace(s))
                {
                    return default;
                }

                // Try exact formats first
                string[] formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "yyyyMMdd" };
                if (DateOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dExact))
                {
                    return dExact;
                }
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dtParsed))
                {
                    return DateOnly.FromDateTime(dtParsed);
                }
                return default;
            default:
                try
                {
                    var asString = value.ToString();
                    if (!string.IsNullOrWhiteSpace(asString))
                    {
                        return ParseDateOnlyOrDefault(asString);
                    }
                }
                catch
                {
                    // ignored
                }
                return default;
        }
    }

    private Dictionary<string, IConversionRule> LoadConversionRules()
    {
        var rules = new Dictionary<string, IConversionRule>();
        
        // Load conversion rules from assembly
        var ruleTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IConversionRule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var ruleType in ruleTypes)
        {
            try
            {
                var rule = Activator.CreateInstance(ruleType) as IConversionRule;
                if (rule != null)
                {
                    var ruleKey = $"{rule.GetSourceDocumentType()}->{rule.GetTargetDocumentType()}";
                    rules[ruleKey] = rule;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversion rule {RuleType}", ruleType.Name);
            }
        }

        return rules;
    }
}
