﻿using OData.Client.Abstractions;
using System.Reflection;

namespace Cblx.Dynamics;

public static class TypeExtensions
{
    public static bool HasODataEndpoint(this Type type)
    {
        return type.GetCustomAttribute<ODataEndpointAttribute>() != null;
    }

    public static bool IsDynamicsEntity(this Type type)
    {
        return type.GetCustomAttribute<DynamicsEntityAttribute>() != null;
    }

    public static string GetTableName(this Type type)
    {
        return type.GetCustomAttribute<DynamicsEntityAttribute>()!.Name;
    }
}
