namespace Cblx.Dynamics;
public static class DynFunctions
{
    /// <summary>
    /// OData - Implemented
    /// FetchXml - Implemented
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static string? FormattedValue<T>(T? o) => throw new NotImplementedException();

    /// <summary>
    /// OData - Implemented
    /// FetchXml - NOT Implemented
    /// </summary>
    /// <param name="multiSelectValue"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool ContainValues(string? multiSelectValue, string[] values) => throw new NotImplementedException();

    /// <summary>
    /// OData - Implemented
    /// FetchXml - NOT Implemented
    /// </summary>
    /// <param name="multiSelectValue"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool DoesNotContainValues(string? multiSelectValue, string[] values) => throw new NotImplementedException();

    /// <summary>
    /// OData - Implemented
    /// FetchXml - NOT Implemented
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool LastMonth(DateTimeOffset? date) => throw new NotImplementedException();

    /// <summary>
    /// OData - Implemented
    /// FetchXml - NOT Implemented
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool NextMonth(DateTimeOffset? date) => throw new NotImplementedException();

    /// <summary>
    /// OData - Implemented
    /// FetchXml - NOT Implemented
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool ThisMonth(DateTimeOffset? date) => throw new NotImplementedException();

    /// <summary>
    /// OData - Implemented
    /// FetchXml - Implemented
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool In<T>(T? field, IEnumerable<T?> values) => throw new NotImplementedException();
}
