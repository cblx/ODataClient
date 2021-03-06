using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cblx.OData.Client.Abstractions;
using OData.Client;

namespace Cblx.Dynamics.OData.Linq;

public class ODataQueryProvider : IAsyncQueryProvider
{
    public string LastUrl { get; private set; } = string.Empty;

    private readonly HttpClient? _httpClient;

    public ODataQueryProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        throw new NotImplementedException();
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new ODataQueryable<TElement>(this, expression);
    }

    public object? Execute(Expression expression)
    {
        throw new NotImplementedException();
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return ExecuteAsync<TResult>(expression).Result;
    }


    async Task<(HttpResponseMessage HttpResponseMessage, ODataExpressionVisitor Visitor)> ExecuteRequestAsync(
        Expression expression,
        CancellationToken cancellationToken = default
    )
    {
        if (_httpClient == null)
        {
            throw new Exception("Query cannot be execute without a HttpClient");
        }

        var visitor = new ODataExpressionVisitor();
        visitor.Visit(expression);
        string url = visitor.ToRelativeUrl();
        HttpResponseMessage responseMessage = await _httpClient.GetAsync(url, cancellationToken);
        LastUrl = url;
        if (responseMessage.StatusCode is not HttpStatusCode.OK)
        {
            string json = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            ThrowODataErrorIfItFits(json);
            throw new HttpRequestException(responseMessage.StatusCode.ToString() + ": " + json);
        }

        return (responseMessage, visitor);
    }

    public async Task<string> GetStringResponseAsync(Expression expression,
        CancellationToken cancellationToken = default)
    {
        var (responseMessage, _) = await ExecuteRequestAsync(expression, cancellationToken);
        return await responseMessage.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<TResult> ExecuteAsync<TResult>(Expression expression,
        CancellationToken cancellationToken = default)
    {
        var (responseMessage, visitor) = await ExecuteRequestAsync(expression, cancellationToken);
        var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Stream responseContent = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
        JsonObject? jsonObject =
            await JsonSerializer.DeserializeAsync<JsonObject>(responseContent, jsonSerializerOptions,
                cancellationToken);
        if (jsonObject == null)
        {
            throw new Exception("Unexpected behavior: Desserialization result is null");
        }
        LambdaExpression projectionExpression = new ODataProjectionRewriter().Rewrite(expression);
        Delegate del = projectionExpression.Compile();
        JsonArray jsonArray = jsonObject["Value"]!.AsArray();
        if (typeof(TResult).IsGenericType && typeof(TResult).IsAssignableTo(typeof(IEnumerable)))
        {
            Type itemType = typeof(TResult).GenericTypeArguments[0];
            return (TResult)GetType()
                .GetMethod(nameof(ToEnumerable), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(itemType)
                .Invoke(this, new object[] { jsonArray, del })!;
        }
        else
        {
            return (TResult)GetType()
                .GetMethod(nameof(ToItem), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(typeof(TResult))
                .Invoke(this, new object[] { jsonArray, del })!;
        }
    }

    TItem? ToItem<TItem>(JsonArray jsonArray, Delegate del)
    {
        TItem? item = ToEnumerable<TItem>(jsonArray, del).FirstOrDefault();
        return item;
    }

    IEnumerable<TItem> ToEnumerable<TItem>(JsonArray jsonArray, Delegate del)
    {
        return jsonArray.Select(item => (TItem)del.DynamicInvoke(item)!)!;
    }

    static void ThrowODataErrorIfItFits(string json)
    {
        ODataError? error = null;

        try
        {
            error = JsonSerializer.Deserialize<ODataError>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
        }

        if (error?.Error is not null)
        {
            throw new ODataErrorException(error.Error.Code, error.Error.Message);
        }
    }
}