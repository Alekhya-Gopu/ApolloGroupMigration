using ApolloMigration.Models;

namespace ApolloMigration.Repositories;

public interface IDataRepository
{
    Task<IEnumerable<Dictionary<string, object>>> GetAllAsync(int offset, int limit);
    Task<T?> GetByIdAsync<T>(string id) where T : BaseModel;
    Task<bool> CreateAsync<T>(T document) where T : BaseModel;
    Task<bool> UpdateAsync<T>(T document) where T : BaseModel;
    Task<bool> CreateBookingAsync(Booking booking);
    Task<bool> CreateBooking3Async(Booking booking);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<int> GetCountAsync();
}
