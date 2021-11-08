using CryptoFinder.Models;
using Etherium.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CryptoFider.Test
{
    public class ServiceTests
    {

        [Fact]
        public async Task GetTransactions_WithMatchingBlockIdAndAddress_ReturnsTransactions()
        {
            var mockService = new Mock<IEthereumService>();
            mockService.Setup(x => x.SearchTransactions(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Response<Object>(Status.SUCCESS, new EthereumTransactionList()
                {
                    Transactions = new List<EthereumTransaction>()
                    {
                        CreateTransaction("0xced189","0x5aa3393e361c2eb342408559309b3e873cd876d6"),
                        CreateTransaction("0xced189","0x5aa3393e361c2eb342408559309b3e873cd876d6")
                    }
                })));

            var response = await mockService.Object.SearchTransactions("0xced189", "0x5aa3393e361c2eb342408559309b3e873cd876d6");

            Assert.Equal(Status.SUCCESS, response.Status);
            var transactions = ((EthereumTransactionList)response.Data).Transactions;
            Assert.Equal(2, transactions.Count());
            var transaction = transactions.FirstOrDefault();
            Assert.Equal("0xced189", transaction.BlockNumber);
            Assert.Equal("0x5aa3393e361c2eb342408559309b3e873cd876d6", transaction.From);
        }

        [Fact]
        public async Task GetTransactions_WithNonMatchingBlockNumber_ReturnsError()
        {
            var blockNumber = "0xced189";
            var address = "0x5aa3393e361c2eb342408559309b3e873cd876d6";
            var mockService = new Mock<IEthereumService>();
            mockService.Setup(x => x.SearchTransactions(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Response<Object>(Status.FAIL, null) 
                { 
                    Error = new Error() { Description = $"No transaction found for address {address} within the block {blockNumber}"}
                }));
            var response = await mockService.Object.SearchTransactions(blockNumber, address);

            Assert.Equal(Status.FAIL, response.Status);
            Assert.Equal($"No transaction found for address {address} within the block {blockNumber}", response.Error.Description);
        }

        [Fact]
        public async Task GetTransactions_WithInvalidBlockNumber_ReturnsError()
        {
            var mockService = new Mock<IEthereumService>();
            mockService.Setup(x => x.SearchTransactions(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Response<Object>(Status.FAIL, "invalid argument 0: invalid hex string")));

            var response = await mockService.Object.SearchTransactions("0xced189__", "0x5aa3393e361c2eb342408559309b3e873cd876d6");

            Assert.Equal(Status.FAIL, response.Status);
            var error = ((string)response.Data);
            Assert.Equal("invalid argument 0: invalid hex string", error);
        }

        [Fact]
        public async Task GetTransactions_WithNonMatchingAddress_ReturnsEmptyListOfTransactions()
        {
            var mockService = new Mock<IEthereumService>();
            mockService.Setup(x => x.SearchTransactions(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Response<Object>(Status.SUCCESS, new EthereumTransactionList()
                {
                    Transactions = Enumerable.Empty<EthereumTransaction>()

                }))); 
            var response = await mockService.Object.SearchTransactions("0xced189", "0x5aa3393e361c2eb342408559309b3e873cd876d6");

            Assert.Equal(Status.SUCCESS, response.Status);
            var transactions = ((EthereumTransactionList)response.Data).Transactions;
            Assert.Empty(transactions);
        }

        private EthereumTransaction CreateTransaction(string blockNumber, string address)
        {
            return new EthereumTransaction()
            {
                BlockHash = "0xae8144f4bca8e13321a762dc3a7c55828cbdf2529d9cd3a6cce91f7bf6b49d50",
                BlockNumber = blockNumber,
                From = address,
                Gas = "0xf4240",
                Hash = "0x9d3e0a41cd688082a658160cab7a0ddb4c5fc5fd5907498ada7fbdf371a806db",
                To = "0x58418d6c83efab01ed78b0ac42e55af01ee77dba",
                Value = "0x0"
            };
        }
    }
}
