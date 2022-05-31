using Cblx.OData.Client.Abstractions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OData.Client;
record RequestParameters(
    ODataClient ODataClient,
    Action<HttpRequestMessage>? RequestMessageConfiguration,
    string Url)
{
    internal HttpMessageInvoker Invoker => ODataClient.Invoker;
    internal bool ShowLog => ODataClient.Options.ShowLog;
};

record RequestParametersWithValue(
    ODataClient ODataClient,
    Action<HttpRequestMessage> RequestMessageConfiguration,
    string Url, object Value) : RequestParameters(ODataClient, RequestMessageConfiguration, Url);

static class HttpHelpers
{
    static readonly JsonSerializerOptions JsonSerializerOptionsForWrite = new()
    {
        WriteIndented = true
    };

    static readonly DateOnlyJsonConverter DateOnlyJsonConverter = new();

    static HttpHelpers()
    {
        JsonSerializerOptionsForWrite.Converters.Add(DateOnlyJsonConverter);
    }

    public static async Task Patch(RequestParametersWithValue parameters)
    {
        string jsonValue = SerializeForWrite(parameters.Value);
        StringBuilder sbLog = new();
        if (parameters.ShowLog)
        {
            AppendRequestInfo(sbLog, parameters.Invoker, "PATCH", parameters.Url);
            AppendBodyInfo(sbLog, jsonValue);
        }
        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, parameters.Url)
        {
            Content = new StringContent(jsonValue, Encoding.UTF8, "application/json")
        };
        parameters.RequestMessageConfiguration?.Invoke(requestMessage);
        HttpResponseMessage responseMessage = await parameters.Invoker.SendAsync(requestMessage, default);

        var json = await responseMessage.Content.ReadAsStringAsync();
        if (parameters.ShowLog)
        {
            AppendResponseInfo(sbLog, json);
            Console.WriteLine(sbLog);
        }
        ThrowErrorIfNotOk(responseMessage, json);
    }

    public static async Task Post(RequestParametersWithValue parameters)
    {
        string jsonValue = SerializeForWrite(parameters.Value);
        StringBuilder sbLog = new();
        if (parameters.ShowLog)
        {
            AppendRequestInfo(sbLog, parameters.Invoker, "POST", parameters.Url);
            AppendBodyInfo(sbLog, jsonValue);
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, parameters.Url)
        {
            Content = new StringContent(jsonValue, Encoding.UTF8, "application/json")
        };
        parameters.RequestMessageConfiguration?.Invoke(requestMessage);
        HttpResponseMessage responseMessage = await parameters.Invoker.SendAsync(requestMessage, default);

        var json = await responseMessage.Content.ReadAsStringAsync();
        if (parameters.ShowLog)
        {
            AppendResponseInfo(sbLog, json);
            Console.WriteLine(sbLog);
        }
        ThrowErrorIfNotOk(responseMessage, json);
    }

    static string SerializeForWrite<TValue>(TValue value) => JsonSerializer.Serialize(value, JsonSerializerOptionsForWrite);

    public static async Task Delete(RequestParameters parameters)
    {
        StringBuilder sbLog = new();
        if (parameters.ShowLog)
        {
            AppendRequestInfo(sbLog, parameters.Invoker, "DELETE", parameters.Url);
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, parameters.Url);
        parameters.RequestMessageConfiguration?.Invoke(requestMessage);
        HttpResponseMessage responseMessage = await parameters.Invoker.SendAsync(requestMessage, default);

        var json = await responseMessage.Content.ReadAsStringAsync();

        if (parameters.ShowLog)
        {
            AppendResponseInfo(sbLog, json);
            Console.WriteLine(sbLog);
        }
        ThrowErrorIfNotOk(responseMessage, json);
    }

    public static async Task<TResult?> Get<TResult>(RequestParameters parameters)
    {
        StringBuilder sbLog = new();
        if (parameters.ShowLog)
        {
            AppendRequestInfo(sbLog, parameters.Invoker, "GET", parameters.Url);
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, parameters.Url);
        parameters.RequestMessageConfiguration?.Invoke(requestMessage);
        HttpResponseMessage responseMessage = await parameters.Invoker.SendAsync(requestMessage, default);
        await ThrowErrorIfNotOk(responseMessage);

        var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        jsonSerializerOptions.Converters.Add(DateOnlyJsonConverter);
        string json = "*JSON FROM STREAM*";
        try
        {
            if (parameters.ODataClient.Options.ReadResponsesAsString)
            {
                json = await responseMessage.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResult>(json, jsonSerializerOptions);
            }
            else
            {
                return await JsonSerializer.DeserializeAsync<TResult>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);
            }
        }
        finally
        {
            if (parameters.ShowLog)
            {
                AppendResponseInfo(sbLog, json);
                Console.WriteLine(sbLog);
            }
        }
    }

    static void AppendRequestInfo(StringBuilder sbLog, HttpMessageInvoker invoker, string method, string url)
    {
        sbLog.AppendLine();
        sbLog.AppendLine("########################");
        sbLog.AppendLine("# ODataClient");
        sbLog.AppendLine("#");
        sbLog.AppendLine("# TOKEN: " + (invoker as HttpClient)?.DefaultRequestHeaders.Authorization?.Parameter);
        sbLog.AppendLine("#");
        sbLog.AppendLine($"# {method}");
        sbLog.AppendLine("#");
        sbLog.AppendLine("# " + url.Replace("?", "\n  ?").Replace("&", "\n  &"));
        sbLog.AppendLine("#");
    }

    static void AppendBodyInfo(StringBuilder sbLog, string json)
    {
        sbLog.AppendLine();
        sbLog.AppendLine("# BODY ");
        sbLog.AppendLine("#");
        sbLog.AppendLine(json);
        sbLog.AppendLine("#");
    }

    static void AppendResponseInfo(StringBuilder sbLog, string responseText)
    {
        int limit = 40000;
        string formattedResponseText;
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(DateOnlyJsonConverter);
            formattedResponseText = string.Join("",
                    JsonSerializer
                    .Serialize(JsonSerializer.Deserialize<Dictionary<string, object>>(responseText), options)
                    .Take(limit)
                    );
        }
        catch
        {
            formattedResponseText = responseText;
        }
        sbLog.Append("# " + formattedResponseText.Replace("\n", "\n# "));
        if (formattedResponseText.Length >= limit)
        {
            sbLog.AppendLine("# ...");
        }
        sbLog.AppendLine();
        sbLog.AppendLine("###########");
        sbLog.AppendLine();
    }

    static void ThrowErrorIfNotOk(HttpResponseMessage responseMessage, string json)
    {
        if (responseMessage.StatusCode != HttpStatusCode.OK && responseMessage.StatusCode != HttpStatusCode.NoContent)
        {
            ThrowODataErrorIfItFits(json);
            throw new HttpRequestException(responseMessage.StatusCode.ToString() + ": " + json);
        }
    }

    static async Task ThrowErrorIfNotOk(HttpResponseMessage responseMessage)
    {
        if (responseMessage.StatusCode != HttpStatusCode.OK && responseMessage.StatusCode != HttpStatusCode.NoContent)
        {
            string json = await responseMessage.Content.ReadAsStringAsync();
            ThrowODataErrorIfItFits(json);
            throw new HttpRequestException(responseMessage.StatusCode.ToString() + ": " + json);
        }
    }

    static void ThrowODataErrorIfItFits(string json)
    {
        ODataError? error = null;

        try
        {
            error = JsonSerializer.Deserialize<ODataError>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            // ignored
        }

        if (error?.Error is not null)
        {
            throw new ODataErrorException(error.Error.Code, error.Error.Message);
        }
    }

}

public class ODataError
{
    public ODataErrorCodeMessage? Error { get; set; }
}

public class ODataErrorCodeMessage
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}
