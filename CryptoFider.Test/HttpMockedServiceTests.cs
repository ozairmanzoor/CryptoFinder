using CryptoFinder.Models;
using Etherium.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CryptoFider.Test
{
    public class HttpMockedServiceTests
    {
        [Fact]
        public async Task GetTransactions_WithMatchingBlockIdAndAddress_ReturnsTransactions()
        {
            var ethereumService = SetupEthereumService(this.SerializedTestData);

            var response = await ethereumService.SearchTransactions("0xced189", "0x5aa3393e361c2eb342408559309b3e873cd876d6");

            Assert.Equal(Status.SUCCESS, response.Status);
            var transactions = ((EthereumTransactionList)response.Data).Transactions;
            Assert.Single(transactions);
            var transaction = transactions.FirstOrDefault();
            Assert.Equal("0xced189", transaction.BlockNumber);
            Assert.Equal("0x5aa3393e361c2eb342408559309b3e873cd876d6", transaction.From);
            Assert.Null(response.Error);
        }

        [Fact]
        public async Task GetTransactions_WithNonMatchingBlockNumber_ReturnsListOfTransactionsAsNull()
        {
            var blockNumber = "0xced190";
            var address = "0x5aa3393e361c2eb342408559309b3e873cd876d6";
            
            var ethereumService = SetupEthereumService("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":null}");

            var response = await ethereumService.SearchTransactions(blockNumber, address);

            Assert.Equal(Status.FAIL, response.Status);
            Assert.Equal($"No transaction found for address {address} within the block {blockNumber}", response.Error.Description);
        }

        [Theory]
        [InlineData("0xced189z", "invalid argument 0: invalid hex string")]
        [InlineData("", "invalid argument 0: empty hex string")]
        public async Task GetTransactions_WithInvalidBlockNumber_ReturnsError(string blockNumber, string message)
        {
            string json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{\"code\":-32602,\"message\":\"" + message + "\"}}"; 
              
            var ethereumService = SetupEthereumService(json);

            var response = await ethereumService.SearchTransactions(blockNumber, "0x5aa3393e361c2eb342408559309b3e873cd876d6");

            Assert.Equal(Status.FAIL, response.Status);
            Assert.Null(response.Data);
            Assert.Equal("Error from host", response.Error.Description);
            Assert.Equal(-32602, response.Error.Code);
            Assert.Equal(message, response.Error.Message);
        }

        [Fact]
        public async Task GetTransactions_WithNonMatchingAddress_ReturnsEmptyListOfTransactions()
        {
            var ethereumService = SetupEthereumService(this.SerializedTestData);

            var response = await ethereumService.SearchTransactions("0xced189", "0x5aa3393e361c2eb342408559309b3e873cd876d7");

            Assert.Equal(Status.SUCCESS, response.Status);
            var transactions = ((EthereumTransactionList)response.Data).Transactions;
            Assert.Empty(transactions);
            Assert.Null(response.Error);
        }


        [Fact]
        public async Task GetTransactions_WithHttpStatusSuccessButInvalidJsonResponseFromServer_ReturnsError()
        {
            var ethereumService = SetupEthereumService("{");

            var response = await ethereumService.SearchTransactions("0xced189", "0x5aa3393e361c2eb342408559309b3e873cd876d7");

            Assert.Equal(Status.FAIL, response.Status);
            Assert.Null(response.Data);
            Assert.Equal("Internal Error", response.Error.Description);
            Assert.Equal(0, response.Error.Code);
            Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 1.", response.Error.Message);
        }

        [Fact]
        public async Task GetTransactions_WithBadRequestErrorFromServer_ReturnsError()
        {
            var ethereumService = SetupEthereumService("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32600,\"message\":\"invalid json request\"}}", HttpStatusCode.BadRequest);

            var response = await ethereumService.SearchTransactions("0xced189", "0x5aa3393e361c2eb342408559309b3e873cd876d7");

            Assert.Equal(Status.FAIL, response.Status);
            Assert.Null(response.Data);
            Assert.Equal("Unsuccessful response from host", response.Error.Description);
            Assert.Equal(-32600, response.Error.Code);
            Assert.Equal("invalid json request", response.Error.Message);
        }

        [Fact]
        public async Task GetTransactions_WithExceptionFromHttpClient_ReturnsError()
        {
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException("Timeout from host"));
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient(It.Is<string>(str => str == "ethereum"))).Returns(mockHttpClient.Object);

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<EthereumService>();

            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var ethereumService = new EthereumService(mockHttpClientFactory.Object, config, logger);

            var response = await ethereumService.SearchTransactions("0xced189", "0x5aa3393e361c2eb342408559309b3e873cd876d7");

            Assert.Equal(Status.FAIL, response.Status);
            Assert.Null(response.Data);
            Assert.Equal("Internal Error", response.Error.Description);
            Assert.Equal(0, response.Error.Code);
            Assert.Equal("Timeout from host", response.Error.Message);
        }

        private EthereumService SetupEthereumService(string responseFromHost, HttpStatusCode? httpStatus = null)
        {
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpResponseMessage()
                {
                    Content = new StringContent(responseFromHost),
                    StatusCode = httpStatus ?? HttpStatusCode.OK,
                }));
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(x => x.CreateClient(It.Is<string>(str => str == "ethereum"))).Returns(mockHttpClient.Object);

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<EthereumService>();

            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var ethereumService = new EthereumService(mockHttpClientFactory.Object, config, logger);
            return ethereumService;
        }

        
        private string SerializedTestData = "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"baseFeePerGas\":\"0x1d110095c9\",\"difficulty\":\"0x244293dea2ef5d\",\"extraData\":\"0x617369612d65617374322d3132\",\"gasLimit\":\"0x1ca35ef\",\"gasUsed\":\"0x5d6e82\",\"hash\":\"0xae8144f4bca8e13321a762dc3a7c55828cbdf2529d9cd3a6cce91f7bf6b49d50\",\"logsBloom\":\"0x0032aa42080013158004a5808421403f480050300808c0500209024a0002c80010000410409001000800418002790182822288200800231040149a0c00a10a40004405008003012809031028800500b40074a0980060206108802000ca042500128006c01302807004c2b002184419f838c6c482000204415a301c33000a88441200803d840020149144004801100f05107180090f9026082842006000300128461e801000c52208808540d0a8080600204411100001201a4844040218a01810204050220054024132020004821801b008500a200209101000103032c204272521542408045c018200011104001854b050000060e140b0400203420008108103\",\"miner\":\"0xea674fdde714fd979de3edf0f56aa9716b898ec8\",\"mixHash\":\"0xa339da82c43db8714267bc8a0ce8474bd484db147bbf2270fe98887624eb24af\",\"nonce\":\"0x5b32636b465096e7\",\"number\":\"0xced189\",\"parentHash\":\"0xa287f9cd763bd6ed50687b57d2ae8ebba044d2481c70ca0e8d86673bde10d236\",\"receiptsRoot\":\"0xdd0f425db736ca20babd646699ef4b532fa3977aaafad7272527b098b3d1f5bb\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x6683\",\"stateRoot\":\"0x40777afd1eea0598bf9c3174c5379640f2491d859163498a32da15e19c4c05b8\",\"timestamp\":\"0x61849dba\",\"totalDifficulty\":\"0x726a038b0e15e6b3d7d\",\"transactions\":["
            + "{\"accessList\":[],\"blockHash\":\"0xae8144f4bca8e13321a762dc3a7c55828cbdf2529d9cd3a6cce91f7bf6b49d50\",\"blockNumber\":\"0xced189\",\"chainId\":\"0x1\",\"from\":\"0x5aa3393e361c2eb342408559309b3e873cd876d6\",\"gas\":\"0xf4240\",\"gasPrice\":\"0x1d4c9b5fc9\",\"hash\":\"0x9d3e0a41cd688082a658160cab7a0ddb4c5fc5fd5907498ada7fbdf371a806db\",\"input\":\"0x000000520000000000000000000000000000000000000000000000000000000000ced18900000000000000000000000000000000000000000000000010c466b26bd63579000000000000000000000000cc8fa225d80b9c7d42f96e9570156c65d6caaa250000000000000000000000008597fa0773888107e2867d36dd87fe5bafeab3280000000000000000000000000cfbed8f2248d2735203f602be0cae5a3131ec6800000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000000\",\"maxFeePerGas\":\"0x20eebb7282\",\"maxPriorityFeePerGas\":\"0x3b9aca00\",\"nonce\":\"0x4b18\",\"r\":\"0xfa8c4725e211a4bc399ccf6973c3b704158cb3e915cbea4361c6a0cceb094df4\",\"s\":\"0x2fed85b257b9be2e5b26c285f23824409b53276c7a97be1b2a53214d4db4966e\",\"to\":\"0x58418d6c83efab01ed78b0ac42e55af01ee77dba\",\"transactionIndex\":\"0x0\",\"type\":\"0x2\",\"v\":\"0x0\",\"value\":\"0x0\"},"
            + "{\"accessList\":[],\"blockHash\":\"0xae8144f4bca8e13321a762dc3a7c55828cbdf2529d9cd3a6cce91f7bf6b49d50\",\"blockNumber\":\"0xced189\",\"chainId\":\"0x1\",\"from\":\"0x44a407077bd922ef89949c6895e0ff12d9813c02\",\"gas\":\"0x10395\",\"gasPrice\":\"0x1d110095c9\",\"hash\":\"0xac6397bce5ab74fa93af9569a8fb8435d255f55c1b456c8600ff52e06709ac0e\",\"input\":\"0x095ea7b3000000000000000000000000a58f22e0766b3764376c92915ba545d583c19dbc00000000000000000000000000000000000000000000000000000001004ccb00\",\"maxFeePerGas\":\"0x2a9da0085f\",\"maxPriorityFeePerGas\":\"0x0\",\"nonce\":\"0x609\",\"r\":\"0xd1c0f08dd8d688fc6c1707e893d2b1b5171c38488434a3432246b7f028638163\",\"s\":\"0x40956b57c29e6930d1c54754ab31bb413e398e3b6aa161289237841b6d1b0fb1\",\"to\":\"0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48\",\"transactionIndex\":\"0x1\",\"type\":\"0x2\",\"v\":\"0x0\",\"value\":\"0x0\"},"
            + "{\"accessList\":[],\"blockHash\":\"0xae8144f4bca8e13321a762dc3a7c55828cbdf2529d9cd3a6cce91f7bf6b49d50\",\"blockNumber\":\"0xced189\",\"chainId\":\"0x1\",\"from\":\"0x44a407077bd922ef89949c6895e0ff12d9813c02\",\"gas\":\"0x7704c\",\"gasPrice\":\"0x1d110095c9\",\"hash\":\"0x21144ba3657cba8928649e12d2f690838b5e843df0a09fede04e5748886b70fb\",\"input\":\"0x54d51de40000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000001b6fdd6f78186e00000000000000000000000000000000000000000000000000000001004ccb00000000000000000000000000000000000000000000000056630c679bbb590ba900000000000000000000000000000000000000000000000000000000000000a000000000000000000000000044a407077bd922ef89949c6895e0ff12d9813c02000000000000000000000000000000000000000000000000000000006184a24e0000000000000000000000000000000000000000000000000000000000000002000000000000000000000000a0b86991c6218b36c1d19d4a2e9eb0ce3606eb48000000000000000000000000f5cfbc74057c610c8ef151a439252680ac68c6dc\",\"maxFeePerGas\":\"0x2a9da0085f\",\"maxPriorityFeePerGas\":\"0x0\",\"nonce\":\"0x60a\",\"r\":\"0xbb7ea443219d262f30e121211811b666bb682c9612c9df6209959d97f3ee9d88\",\"s\":\"0x2b6399d139dd4ff0cf9f3947a113d7cb160581b8f0f850b6bb7c781406fbbb09\",\"to\":\"0xa58f22e0766b3764376c92915ba545d583c19dbc\",\"transactionIndex\":\"0x2\",\"type\":\"0x2\",\"v\":\"0x1\",\"value\":\"0x1b6fdd6f78186e\"},"
            + "{\"blockHash\":\"0xae8144f4bca8e13321a762dc3a7c55828cbdf2529d9cd3a6cce91f7bf6b49d50\",\"blockNumber\":\"0xced189\",\"from\":\"0xcac725bef4f114f728cbcfd744a731c2a463c3fc\",\"gas\":\"0x15f90\",\"gasPrice\":\"0x2da282a800\",\"hash\":\"0x81a39fb6a05dc0db6f3ee813b15cf1085b392b68a40b88fc606f2e462892ce30\",\"input\":\"0x\",\"nonce\":\"0x3ab46\",\"r\":\"0x6baf592ef944d3d96bc46bfedaaa31b219171f01a1bacb3affa7bb6026fa2cc8\",\"s\":\"0x7fff960dea2e07871778203ecc33c27bce4ccc5bd18b49e3cf52b66ba4d06ef7\",\"to\":\"0x0d9b09a4307138adff044a793d082293b8a73df1\",\"transactionIndex\":\"0x3\",\"type\":\"0x0\",\"v\":\"0x25\",\"value\":\"0x1bb3376cbbae0000\"},"
            + "{\"blockHash\":\"0xae8144f4bca8e13321a762dc3a7c55828cbdf2529d9cd3a6cce91f7bf6b49d50\",\"blockNumber\":\"0xced189\",\"from\":\"0xac340085eb11f43cc62ee4a7fc4916da4f8f4c3a\",\"gas\":\"0x370b6\",\"gasPrice\":\"0x2883354c00\",\"hash\":\"0xc94a0e8978b902ee6b64629ec2733ea69494a4bec58455c9d7f0422ecad68155\",\"input\":\"0x5cf5402600000000000000000000000000000000000000000000000000000000000000c0000000000000000000000000eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee000000000000000000000000eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee0000000000000000000000000000000000000000000000000007cb6a9dae4820000000000000000000000000e53ec727dbdeb9e2d5456c3be40cff031ab40a5500000000000000000000000000000000000000000000000000001427dd6405e60000000000000000000000000000000000000000000000000000000000000128d9627aa400000000000000000000000000000000000000000000000000000000000000800000000000000000000000000000000000000000000000000007cb6a9dae48200000000000000000000000000000000000000000000000005e5bfcf8f2cdeca700000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000002000000000000000000000000eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee000000000000000000000000e53ec727dbdeb9e2d5456c3be40cff031ab40a55869584cd000000000000000000000000382ffce2287252f930e1c8dc9328dac5bf282ba100000000000000000000000000000000000000000000009f9329f23e61849da6000000000000000000000000000000000000000000000000\",\"nonce\":\"0x0\",\"r\":\"0x18b12314f26d66ae3a6c7cf708c4da9119eeb3348135c9f9be5b9b1ae6543af2\",\"s\":\"0x3db742fd4030016137aa54a5ce5f7bd5fad7f384ff1ad22518e5d0e73d447746\",\"to\":\"0xe66b31678d6c16e9ebf358268a790b763c133750\",\"transactionIndex\":\"0x4\",\"type\":\"0x0\",\"v\":\"0x26\",\"value\":\"0x7df927b124e06\"}],\"transactionsRoot\":\"0x6c3f65f1bcca00ff3c482f05b0d65b14a0b9363492dfa5fd1bf09897542f3c09\",\"uncles\":[]}}";
    }
}
