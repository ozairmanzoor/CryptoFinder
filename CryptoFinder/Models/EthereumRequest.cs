using System;
using System.Collections.Generic;

namespace CryptoFinder.Models
{
    public class EthereumRequest
    {
        public string Jsonrpc { get; set; }
        public int Id { get; set; }
        public string Method { get; set; }
        public List<Object> Params { get; set; }

        public EthereumRequest(string methodName, List<Object> parameters)
        {
            this.Jsonrpc = "2.0";
            this.Id = 1;
            this.Method = methodName;
            this.Params = parameters;
        }
    }
}
