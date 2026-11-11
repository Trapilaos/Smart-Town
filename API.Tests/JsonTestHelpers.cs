using System.Text.Json;

namespace API.Tests;

public static class JsonTestHelpers
{
    /// <summary>Every property name in the document, at any depth.</summary>
    public static IEnumerable<string> AllPropertyNames(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Name;
                    foreach (var nested in AllPropertyNames(property.Value))
                        yield return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    foreach (var nested in AllPropertyNames(item))
                        yield return nested;
                break;
        }
    }

    /// <summary>Property names that would reveal who a person is or who reserved or paid for something.</summary>
    public static readonly string[] IdentityFields =
    {
        "userId", "userIds", "interestedUsers", "username", "email", "phoneNumber", "payerName", "paymentCode",
    };
}
