using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cblx.OData.Client.Abstractions;
using OData.Client;

namespace Cblx.Dynamics.FetchXml.Linq;

public class FetchXmlQueryProvider : IAsyncQueryProvider
{
    public string LastUrl { get; private set; } = string.Empty;

    private readonly HttpClient? _httpClient;

    public FetchXmlQueryProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        throw new NotImplementedException();
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new FetchXmlQueryable<TElement>(this, expression);
    }

    public object? Execute(Expression expression)
    {
        throw new NotImplementedException();
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return ExecuteAsync<TResult>(expression).Result;
    }


    async Task<(HttpResponseMessage HttpResponseMessage, FetchXmlExpressionVisitor Visitor)> ExecuteRequestAsync(
        Expression expression,
        CancellationToken cancellationToken = default
    )
    {
        if (_httpClient == null)
        {
            throw new InvalidOperationException("Query cannot be execute without a HttpClient");
        }

        var visitor = new FetchXmlExpressionVisitor();
        visitor.Visit(expression);
        string url = visitor.ToRelativeUrl();
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        if (visitor.HasFormattedValues)
        {
            requestMessage.Headers.Add("Prefer", $"odata.include-annotations={DynAnnotations.FormattedValue}");
        }
        var responseMessage = await _httpClient.SendAsync(requestMessage,cancellationToken);
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

        // Review: Makes sense using Options to desserialize to a JsonObject?
        //var jsonSerializerOptions = new JsonSerializerOptions {
        //    PropertyNameCaseInsensitive = true
        //};
        Stream responseContent = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
        JsonObject? jsonObject =
            await JsonSerializer.DeserializeAsync<JsonObject>(responseContent, 
                /*jsonSerializerOptions,*/
                cancellationToken: cancellationToken);
        if (jsonObject == null)
        {
            throw new InvalidOperationException("Unexpected behavior: Desserialization result is null");
        }

        LambdaExpression projectionExpression =
            visitor.IsGroupBy
                ? new FetchXmlGroupProjectionRewriter(
                    visitor.GroupExpression,
                    visitor.GroupByExpression
                ).Rewrite(expression)
                : new FetchXmlProjectionRewriter().Rewrite(expression);

        Delegate del = projectionExpression.Compile();
        JsonArray jsonArray = jsonObject["value"]!.AsArray();
        if (typeof(TResult).IsGenericType && typeof(TResult).IsAssignableTo(typeof(IEnumerable)))
        {
            Type itemType = typeof(TResult).GenericTypeArguments[0];
            return (TResult) GetType()
                .GetMethod(nameof(ToEnumerable), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(itemType)
                .Invoke(this, new object[] {jsonArray, del})!;
        }
        else
        {
            return (TResult) GetType()
                .GetMethod(nameof(ToItem), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TResult))
                .Invoke(this, new object[] {jsonArray, del})!;
        }
    }

    static TItem? ToItem<TItem>(JsonArray jsonArray, Delegate del) => ToEnumerable<TItem>(jsonArray, del).FirstOrDefault();

    static IEnumerable<TItem> ToEnumerable<TItem>(JsonArray jsonArray, Delegate del) => jsonArray.Select(item => (TItem) del.DynamicInvoke(item)!)!;

    static void ThrowODataErrorIfItFits(string json)
    {
        ODataError? error = null;

        try
        {
            error = JsonSerializer.Deserialize<ODataError>(json,
                new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch
        {
            // Does not throw, if can't deserialize to ODataError
        }

        if (error?.Error is not null)
        {
            throw new ODataErrorException(error.Error.Code, error.Error.Message);
        }
    }
}