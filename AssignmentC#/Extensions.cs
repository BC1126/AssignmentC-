using Microsoft.Identity.Client;
using System.Text.Json;

namespace AssignmentC_;

public static class Extensions
{
    public static bool IsAjax(this HttpRequest request)
    {
        return request.Headers.XRequestedWith == "XMLHttpRequest";
    }

    // ------------------------------------------------------------------------
    // Session Extension Methods
    // ------------------------------------------------------------------------

    public static void Set<T>(this ISession session, string key, T value)
    {
        //convert custom object (record etc) to Jason string
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? Get<T>(this ISession session, string key)
    {
        //convert Json string to custom object (record etc)
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    // ------------------------------------------------------------------------
    // Date and Time Extension Methods
    // ------------------------------------------------------------------------

    public static DateOnly ToDateOnly(this DateTime dt)
    {
        return DateOnly.FromDateTime(dt);
    }

    public static TimeOnly ToTimeOnly(this DateTime dt)
    {
        return TimeOnly.FromDateTime(dt);
    }

    public static DateOnly Today(this DateOnly date)
    {
        return DateOnly.FromDateTime(DateTime.Today);
    }

    public static TimeOnly Now(this TimeOnly date)
    {
        return TimeOnly.FromDateTime(DateTime.Now);
    }
}
