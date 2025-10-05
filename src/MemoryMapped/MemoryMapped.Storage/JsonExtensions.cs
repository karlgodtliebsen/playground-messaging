using System.Text.Json;

namespace MemoryMapped.Storage;

public static class JsonExtensions
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

}