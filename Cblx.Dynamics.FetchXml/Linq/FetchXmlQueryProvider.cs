using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using System.Xml.Linq;
using Cblx.Dynamics.Linq;
using Cblx.OData.Client.Abstractions;
using OData.Client;
using OData.Client.Abstractions;

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

    Task<(HttpResponseMessage HttpResponseMessage, FetchXmlExpressionVisitor Visitor)> ExecuteRequestAsync(
      Expression expression,
      CancellationToken cancellationToken = default
  )
    {
        var visitor = new FetchXmlExpressionVisitor();
        visitor.Visit(expression);
        return ExecuteRequestAsync(visitor, visitor.ToFetchXmlElement(), cancellationToken);
    }


    async Task<(HttpResponseMessage HttpResponseMessage, FetchXmlExpressionVisitor Visitor)> ExecuteRequestAsync(
        FetchXmlExpressionVisitor visitor,
        XElement fetchXmlElement,
        CancellationToken cancellationToken = default
    )
    {

        if (_httpClient == null)
        {
            throw new InvalidOperationException("Query cannot be execute without a HttpClient");
        }
        //string url = $"{visitor.Endpoint}?fetchXml={HttpUtility.UrlEncode(fetchXmlElement.ToString())}";
        string url = $"{visitor.Endpoint}?fetchXml={fetchXmlElement}";
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        if (visitor.IncludeAllAnnotations)
        {
            // Pagination data will only return with all annotations enabled
            requestMessage.Headers.Add("Prefer", $"odata.include-annotations={DynAnnotations.All}");
        }
        else if (visitor.HasFormattedValues)
        {
            requestMessage.Headers.Add("Prefer", $"odata.include-annotations={DynAnnotations.FormattedValue}");
        }
        var responseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);
        LastUrl = $"{visitor.Endpoint}?fetchXml={fetchXmlElement}";
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

    public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var nonPublicAndStatic = BindingFlags.NonPublic | BindingFlags.Static;

        var visitor = new FetchXmlExpressionVisitor();
        visitor.Visit(expression);
        Delegate projectionFunc = GetProjectionFunc(expression, visitor);
        var fetchXmlElement = visitor.ToFetchXmlElement();
        var (responseMessage, _) = await ExecuteRequestAsync(visitor, fetchXmlElement, cancellationToken);

        Stream responseContent = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
        JsonObject? jsonObject = await JsonSerializer.DeserializeAsync<JsonObject>(responseContent, cancellationToken: cancellationToken);
        if (jsonObject == null)
        {
            throw new InvalidOperationException("Unexpected behavior: Desserialization result is null");
        }
        
        JsonArray jsonArray = jsonObject["value"]!.AsArray();
        if (typeof(TResult).IsGenericType && typeof(TResult).IsAssignableTo(typeof(IEnumerable)))
        {
            Type itemType = typeof(TResult).GenericTypeArguments[0];
            return (TResult)GetType()
                .GetMethod(nameof(ToEnumerable), nonPublicAndStatic)!
                .MakeGenericMethod(itemType)
                .Invoke(this, new object[] { jsonArray, projectionFunc })!;
        }
        else if(typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(DynamicsResult<>))
        {
            Type itemType = typeof(TResult).GenericTypeArguments[0];
            return (TResult)GetType()
                .GetMethod(nameof(ToResult), nonPublicAndStatic)!
                .MakeGenericMethod(itemType)
                .Invoke(this, new object[] { jsonObject, projectionFunc })!;
        }
        else
        {
            return (TResult)GetType()
                .GetMethod(nameof(ToItem), nonPublicAndStatic)!
                .MakeGenericMethod(typeof(TResult))
                .Invoke(this, new object[] { jsonArray, projectionFunc })!;
        }
    }

    private static Delegate GetProjectionFunc(Expression expression, FetchXmlExpressionVisitor visitor)
    {
        LambdaExpression projectionExpression =
        visitor.IsGroupBy
            ? new FetchXmlGroupProjectionRewriter(
                visitor.GroupExpression,
                visitor.GroupByExpression
            ).Rewrite(expression)
            : new FetchXmlProjectionRewriter().Rewrite(expression);
        return projectionExpression.Compile();
    }


    static TItem? ToItem<TItem>(JsonArray jsonArray, Delegate del) => ToEnumerable<TItem>(jsonArray, del).FirstOrDefault();

    static IEnumerable<TItem> ToEnumerable<TItem>(JsonArray jsonArray, Delegate del) => jsonArray.Select(item => (TItem)del.DynamicInvoke(item)!)!;

    static DynamicsResult<TItem> ToResult<TItem>(JsonObject jsonObject, Delegate del)
    {
        var jsonArray = jsonObject["value"]!.AsArray();
        var items = ToEnumerable<TItem>(jsonArray, del);
        return new DynamicsResult<TItem>
        {
            Value = items.ToArray(),
            Count = jsonObject.ContainsKey("@odata.count") ? jsonObject["@odata.count"]?.GetValue<int>() : null,
            FetchXmlPagingCookie = jsonObject.ContainsKey("@Microsoft.Dynamics.CRM.fetchxmlpagingcookie") ?
                jsonObject["@Microsoft.Dynamics.CRM.fetchxmlpagingcookie"]?.GetValue<string>() : null
        };
    }

    static void ThrowODataErrorIfItFits(string json)
    {
        ODataError? error = null;

        try
        {
            error = JsonSerializer.Deserialize<ODataError>(json,
                new JsonSerializerOptions
                {
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