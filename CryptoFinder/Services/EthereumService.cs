using CryptoFinder.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Etherium.Services
{
    public interface IEthereumService
    {
        Task<Response<Object>> SearchTransactions(string blockNumber, string address);
    }

    public class EthereumService : IEthereumService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration Configuration;
        private readonly ILogger<EthereumService> logger;

        public EthereumService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<EthereumService> logger)
        {
            //Services using constructor injection
            this.httpClientFactory = httpClientFactory;
            this.Configuration = configuration;
            this.logger = logger;
        }

        /// <summary>
        /// Returns Ethereum transactions from server.
        /// </summary>
        /// <param name="blockNumber"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Response<Object>> SearchTransactions(string blockNumber, string address)
        {
            try
            {
                logger.LogInformation($"Searching for transactions for address {address} within block {blockNumber}");

                //create httpclient
                var httpClient = httpClientFactory.CreateClient("ethereum");

                //read remote server address from appsettings
                var serverAdddress = Configuration.GetSection("EthereumServer").Value;
                if (string.IsNullOrEmpty(serverAdddress))
                {
                    throw new Exception();
                }

                //create http request
                var request = new EthereumRequest("eth_getBlockByNumber", new List<Object>()
                {
                    blockNumber,
                    true
                });

                var httpRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(serverAdddress),
                    Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"),

                };

                //send request to remote server
                var httpResponse = httpClient.SendAsync(httpRequest, CancellationToken.None).Result;
                
                //parse response
                var content = await httpResponse.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<EthereumResponse>(content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    logger.LogInformation($"Successful response from host: {content}");
                    
                    if (response?.Error != null)
                    {
                        //if there is an error return error response
                        return new Response<Object>(Status.FAIL, null)
                        {
                            Error = new Error() { Code = response.Error.Code, Message = response.Error.Message, Description = "Error from host" }
                        };
                    }
                    else if (response?.Result == null)
                    {
                        //if transactions list is null return error.
                        return new Response<Object>(Status.FAIL, null)
                        {
                            Error = new Error()
                            {
                                Message = "Internal Error",
                                Description = $"No transaction found for address { address} within the block { blockNumber}"
                            }
                        };
                    }
                    else
                    {
                        // In case there is no error and transaction list is not null retur transactions
                        var block = response.Result;
                        var transactions = new EthereumTransactionList()
                        {
                            Transactions = block.Transactions.Where(tr => tr.From == address || tr.To == address)
                        };
                        return new Response<Object>(Status.SUCCESS, transactions);
                    }
                }
                

                logger.LogInformation($"Unsuccessful response from host {JsonConvert.SerializeObject(httpResponse)}");

                // In case HttpStatusCode is not 2xx return error
                return new Response<Object>(Status.FAIL, null)
                {
                    Error = new Error() { Code = response.Error.Code, Message = response.Error.Message, Description = "Unsuccessful response from host" }
                };
            }
            catch(Exception ex)
            {
                logger.LogInformation($"Exception while searching for transactions: {JsonConvert.SerializeObject(ex)}");
                
                //In case of exception return error
                return new Response<Object>(Status.FAIL, null)
                {
                    Error = new Error() { Message = ex.Message, Description = "Internal Error" }
                };
            }
        }
    }
}
