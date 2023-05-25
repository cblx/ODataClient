using System;
using System.Collections.Generic;

namespace Cblx.OData.Client;

public class Change
{
    public Guid Id { get; set; }

    public object Entity { get; set; }

    public List<ChangedProperty> ChangedProperties { get; set; } = new List<ChangedProperty>();

    public string NewState { get; set; }

    public ChangeType ChangeType { get; set; }


}
