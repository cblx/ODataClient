using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cblx.Dynamics;

public static class DynamicsBaseAddress
{
    public static Uri FromResourceUrl(string resourceUrl)
    {
        return new Uri(new Uri(resourceUrl), "api/data/v9.0/");
    }
}
