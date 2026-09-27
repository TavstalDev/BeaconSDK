using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Json;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> used for simple dictionary payloads
/// that are not covered by <see cref="BeaconJsonContext"/>.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true, IgnoreReadOnlyFields = true )]
public partial class GenericJsonContext : JsonSerializerContext
{
    
}