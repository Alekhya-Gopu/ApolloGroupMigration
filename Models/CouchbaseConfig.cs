namespace ApolloMigration.Models;

public class CouchbaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}
