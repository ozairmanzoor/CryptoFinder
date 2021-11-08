using CryptoFinder.Models;
using Etherium.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Etherium.Controllers
{
    [Controller]
    public class EthereumController : Controller
    {
        private readonly IEthereumService ethereumService;

        public EthereumController(IEthereumService ethereumService)
        {
            this.ethereumService = ethereumService;
        }

        /// <summary>
        /// Search for transactions associated with an address within Ethereum block
        /// </summary>
        /// <param name="blockNumber"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpGet("/searchTransactions")]
        public async Task<IActionResult> SearchTransactions(string blockNumber, string address)
        {
            //Validate input and return error if invalid
            if (string.IsNullOrEmpty(blockNumber) || string.IsNullOrEmpty(address))
            {
                return View("SearchTransactionsError", new Error() { Description = "BlockNumber/Adress input fields must have a value" });
            }

            //Get trasnactions from ethereum service
            var response = await this.ethereumService.SearchTransactions(blockNumber, address);
            if (response.Status == Status.SUCCESS)
            {
                //on success display search trasnaction page
                return View(response.Data);
            }
            else
                //on error display error page.
                return View("SearchTransactionsError", response.Error);
        }
    }
}
