using System.Text.Json;
using System.Text.Json.Serialization;

namespace RabbitMQ.Messaging.Abstractions;

/// <summary>
/// Provides shared <see cref="JsonSerializerOptions"/> used by the messaging components.
/// </summary>
/// <remarks>
/// The options are configured with <see cref="JsonSerializerDefaults.Web"/> to follow common web-friendly
/// serialization conventions and set <see cref="JsonIgnoreCondition.WhenWritingDefault"/> to minimize payload size
/// by omitting default values. The single instance is safe for reuse and should be used where consistent
/// JSON behavior is required across the library.
/// </remarks>
internal static class JsonOptions
{
    /// <summary>
    /// Gets the default <see cref="JsonSerializerOptions"/> for message serialization and deserialization.
    /// </summary>
    /// <returns>
    /// A configured <see cref="JsonSerializerOptions"/> instance using <see cref="JsonSerializerDefaults.Web"/>
    /// and <see cref="JsonIgnoreCondition.WhenWritingDefault"/>.
    /// </returns>
    /// <remarks>
    /// This instance is intended to be reused for performance and consistency. Do not mutate the returned
    /// instance after retrieval; if custom options are required, clone this instance or create a new one.
    /// </remarks>
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };
}
