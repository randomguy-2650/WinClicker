using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinClicker.Services
{
    /// <summary>
    /// JSON serialisation context for the application.
    /// This source generator creates serialisation code at compile time,
    /// allowing the app to work with trimming enabled.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(object))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(bool))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
