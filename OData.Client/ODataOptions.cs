using System;
using System.Collections.Generic;

namespace OData.Client
{
    internal class ODataOptions
    {
        readonly SortedDictionary<string, HashSet<string>> options = new SortedDictionary<string, HashSet<string>>();

        public IReadOnlyDictionary<string, HashSet<string>> Items => options;

        public ODataOptions()
        {

        }

        private ODataOptions(SortedDictionary<string, HashSet<string>> options)
        {
            this.options = options;
        }

        public ODataOptions Add(string key, string value)
        {
            if (!options.ContainsKey(key))
            {
                options.Add(key, new HashSet<string>());
            }
            options[key].Add(value);
            return this;
        }

        public ODataOptions Clone()
        {
            var copy = new SortedDictionary<string, HashSet<string>>();
            foreach(var kvp in this.options)
            {
                copy[kvp.Key] = new HashSet<string>();
                foreach(var str in kvp.Value)
                {
                    copy[kvp.Key].Add(str);
                }
            }
            return new ODataOptions(copy);
        }
    }
}
