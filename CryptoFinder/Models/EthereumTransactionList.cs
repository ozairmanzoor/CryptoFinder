using System.Collections.Generic;

namespace CryptoFinder.Models
{
    public class EthereumTransactionList
    {
        public IEnumerable<EthereumTransaction> Transactions { get; set; }
    }
}
