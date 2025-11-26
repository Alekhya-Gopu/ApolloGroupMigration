namespace ApolloMigration.Models;

// Sample source model
public class UserV1 : BaseModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
}

// Sample target model
public class UserV2 : BaseModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public ContactInfo Contact { get; set; } = new();
    public UserMetadata Metadata { get; set; } = new();
}

public class ContactInfo
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string PreferredContactMethod { get; set; } = "email";
}

public class UserMetadata
{
    public string Version { get; set; } = "2.0";
    public DateTime LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
}
