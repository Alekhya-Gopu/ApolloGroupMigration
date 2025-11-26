using Couchbase;
using Couchbase.KeyValue;
using Couchbase.Query;
using Microsoft.Extensions.Options;
using ApolloMigration.Models;
using Newtonsoft.Json;

namespace ApolloMigration.Repositories;

public class CouchbaseDataRepository : IDataRepository
{
    private readonly ICluster _cluster;
    private readonly IBucket _bucket;
    private readonly ILogger<CouchbaseDataRepository> _logger;
    private readonly string BOOKINGS = "GroupBookings";
    private readonly string BUCKET = "Apollo";
    private readonly string ESHET = "Eshet";

    public CouchbaseDataRepository(IOptions<CouchbaseConfig> config, ILogger<CouchbaseDataRepository> logger)
    {
        _logger = logger;
        
        var clusterOptions = new ClusterOptions()
            .WithConnectionString(config.Value.ConnectionString)
            .WithCredentials(config.Value.Username, config.Value.Password);

        _cluster = Cluster.ConnectAsync(clusterOptions).GetAwaiter().GetResult();
        _bucket = _cluster.BucketAsync(config.Value.BucketName).GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<Dictionary<string, object>>> GetAllAsync(int offset, int limit)
    {
        try
        {
            var query = $"select `{BOOKINGS}`.* from `{BUCKET}`.`{ESHET}`.`{BOOKINGS}` offset {offset} limit {limit}";

            var result = await _cluster.QueryAsync<Dictionary<string, object>>(query);
            return await result.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents from Bookings collection");
            throw;
        }
    }

    public async Task<T?> GetByIdAsync<T>(string id) where T : BaseModel
    {
        try
        {
            var result = await _bucket.DefaultCollection().GetAsync(id);
            return result.ContentAs<T>();
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document with id {Id}", id);
            throw;
        }
    }

    public async Task<bool> CreateAsync<T>(T document) where T : BaseModel
    {
        try
        {
            document.CreatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
            
            await _bucket.DefaultCollection().InsertAsync(document.Id, document);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document with id {Id}", document.Id);
            return false;
        }
    }

    public async Task<bool> UpdateAsync<T>(T document) where T : BaseModel
    {
        try
        {
            document.UpdatedAt = DateTime.UtcNow;
            await _bucket.DefaultCollection().UpsertAsync(document.Id, document);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document with id {Id}", document.Id);
            return false;
        }
    }

    public async Task<bool> CreateBookingAsync(Booking booking)
    {
        try
        {
            await _bucket.DefaultCollection().UpsertAsync(booking.Id.ToString(), booking);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking with id {Id}", booking.Id);
            return false;
        }
    }

    public async Task<bool> CreateBooking3Async(Booking booking)
    {
        try
        {
            var collection = _bucket.Scope(ESHET).Collection("Booking3");
            await collection.UpsertAsync(booking.Id.ToString(), booking);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking in Bookings3 collection with id {Id}", booking.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _bucket.DefaultCollection().RemoveAsync(id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document with id {Id}", id);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        try
        {
            await _bucket.DefaultCollection().GetAsync(id);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of document with id {Id}", id);
            return false;
        }
    }

    public async Task<int> GetCountAsync()
    {
        try
        {
            var query = $"SELECT COUNT(*) as count FROM `{BUCKET}`.`{ESHET}`.`{BOOKINGS}`";
            var result = await _cluster.QueryAsync<dynamic>(query);
            var countResult = await result.FirstAsync();
            return (int)countResult.count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting count for Bookings collection");
            throw;
        }
    }

    public void Dispose()
    {
        _cluster?.Dispose();
    }
}
