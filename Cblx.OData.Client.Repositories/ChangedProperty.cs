﻿using System.Reflection;

namespace Cblx.OData.Client
{
    public class ChangedProperty
    {
        public PropertyInfo PropertyInfo { get; set; }

        public object? OldValue { get; set; }

        public object? NewValue { get; set; }
    }
}
