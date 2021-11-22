using System;

namespace Cblx.OData.Client.Abstractions
{
    public class ODataErrorException : Exception
    {
        public string Code { get; }

        public ODataErrorException(string code, string message) : base(message)
        {
            Code = code;
        }
    }

   
}
