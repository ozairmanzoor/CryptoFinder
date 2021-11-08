using Newtonsoft.Json;

namespace CryptoFinder.Models
{
    public class EthereumResponse
    {
        public string Jsonrpc { get; set; }
        public int Id { get; set; }
        public EthereumBlock Result { get; set; }
        public EthereumError Error { get; set; }
    }
}
