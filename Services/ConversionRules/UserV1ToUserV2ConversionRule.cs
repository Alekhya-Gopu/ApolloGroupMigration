using ApolloMigration.Models;

namespace ApolloMigration.Services.ConversionRules;

public class UserV1ToUserV2ConversionRule : IConversionRule<UserV1, UserV2>
{
    public UserV2 Convert(UserV1 source)
    {
        return new UserV2
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            DocumentType = "UserV2",
            FullName = $"{source.FirstName} {source.LastName}".Trim(),
            Email = source.Email,
            Age = source.Age,
            Contact = new ContactInfo
            {
                PhoneNumber = source.PhoneNumber,
                PreferredContactMethod = "email"
            },
            Metadata = new UserMetadata
            {
                Version = "2.0",
                LastLogin = DateTime.UtcNow,
                IsActive = true
            }
        };
    }

    public bool CanConvert(BaseModel source)
    {
        return source is UserV1;
    }

    public string GetTargetDocumentType()
    {
        return "UserV2";
    }

    public string GetSourceDocumentType()
    {
        return "UserV1";
    }
}
