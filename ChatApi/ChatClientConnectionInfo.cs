using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

public class ChatClientConnectionInfo
{
    public required Uri Endpoint { get; init; }
    public required string SelectedModel { get; init; }

    public string? AccessKey { get; init; }

    // Example connection string:
    // Endpoint=https://localhost:4523;Model=phi3.5;AccessKey=1234;
    public static bool TryParse(string? connectionString, [NotNullWhen(true)] out ChatClientConnectionInfo? settings)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            settings = null;
            return false;
        }

        var connectionBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        Uri? endpoint = null;
        if (connectionBuilder.ContainsKey("Endpoint") && Uri.TryCreate(connectionBuilder["Endpoint"].ToString(), UriKind.Absolute, out endpoint))
        {
        }

        string? model = null;
        if (connectionBuilder.ContainsKey("Model"))
        {
            model = (string)connectionBuilder["Model"];
        }

        string? accessKey = null;
        if (connectionBuilder.ContainsKey("AccessKey"))
        {
            accessKey = (string)connectionBuilder["AccessKey"];
        }

        if (endpoint is null || model is null)
        {
            settings = null;
            return false;
        }

        settings = new ChatClientConnectionInfo
        {
            Endpoint = endpoint,
            SelectedModel = model,
            AccessKey = accessKey
        };

        return true;
    }
}
