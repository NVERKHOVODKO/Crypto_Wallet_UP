using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using UP.Models;

namespace UP.Controllers;

public class TransactionController: ControllerBase
{
    [HttpGet, Route("getUserConversationsHistory")]
    public async Task<ActionResult> GetUserList(int id)
    {
        var tr = new Repositories.TransactionsRepository();
        try
        {
            return Ok(tr.GetUserConversionsHistory(id));
        }
        catch(Exception)
        {
            return BadRequest("Unable to return user transactions history");
        }
    }
    
    [HttpPut, Route("Convert")]
        public async Task<ActionResult> Convert(string shortNameStart, string shortNameFinal, double quantity, int userId)
        {
            try
            {
                if (quantity == 0)
                {
                    return BadRequest("Error. Quantity must be above than zero");
                }
                
                double priceRatio = await GetPriceRatio(shortNameStart, shortNameFinal);
                double finalQuantity = priceRatio * quantity;
                var ur = new Repositories.UserRepository();
                double startCoinQuantityInUserWallet = ur.GetCoinQuantityInUserWallet(userId, shortNameStart);
                if (startCoinQuantityInUserWallet < quantity)
                {
                    return BadRequest("The user doesn't have enough coins to complete the conversion");
                }
                var cr = new Repositories.CurrencyRepository();
                cr.SubtractCoinFromUser(userId, shortNameStart, quantity);
                cr.AddCryptoToUserWallet(userId, shortNameFinal, finalQuantity);

                var tr = new Repositories.TransactionsRepository();
                tr.WriteNewConversionDataToDatabase(new Conversion(1, 0, quantity, finalQuantity, await cr.GetCoinPrice(quantity, shortNameStart), shortNameStart, shortNameFinal, userId, DateTime.Now));
                return Ok("Converted successfully");
            }
            catch (Exception e)
            {
                return BadRequest("Error. Currencies have not been converted");
            }
        }

        public async Task<double> GetPriceRatio(string shortNameStart, string shortNameFinal)
        {
            string apiKey = "4da2c4791b9c285b22c1bf08bc36f304ab2ca80bc901504742b9a42a814c4614";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);
            string url = $"https://min-api.cryptocompare.com/data/price?fsym=" + shortNameStart + "&tsyms=" + shortNameFinal;
            var response = await httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseContent);
            
            return  (double)json[shortNameFinal.ToUpper()];
        }
        
        
        [HttpPut, Route("Buy crypto")]
        public async Task<ActionResult> BuyCrypto(int userId, string coinName, double quantity)
        {
            if (quantity == 0)
            {
                return UnprocessableEntity("Quantity must be above than zero");
            }
            string apiKey = "4da2c4791b9c285b22c1bf08bc36f304ab2ca80bc901504742b9a42a814c4614";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);
            string url = $"https://min-api.cryptocompare.com/data/price?fsym=" + coinName + "&tsyms=USD";
            var response = await httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseContent);
            //double price = (double)json[coinName.ToUpper()];
            var cr = new Repositories.CurrencyRepository();
            cr.BuyCrypto(userId, coinName, quantity);

            return Ok();
        }
        
        
        [HttpPut, Route("Sell crypto")]
        public async Task<ActionResult> SellCrypto(int userId, string coinName, double quantity)
        {
            try
            {
                if (quantity == 0)
                {
                    return UnprocessableEntity("Quantity must be above than zero");
                }
                string apiKey = "4da2c4791b9c285b22c1bf08bc36f304ab2ca80bc901504742b9a42a814c4614";
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);
                string url = $"https://min-api.cryptocompare.com/data/price?fsym=" + coinName + "&tsyms=USD";
                var response = await httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseContent);
                //double price = (double)json[coinName.ToUpper()];

                var cr = new Repositories.CurrencyRepository();
                cr.SellCrypto(userId, coinName, quantity);

                return Ok();
            }
            catch (Exception)
            {
                return BadRequest("Error. Coin wasn't purchased");
            }
        }
        
        [HttpGet, Route("replenishTheBalance")]
        public async Task<ActionResult> ReplenishTheBalance(int userId, double quantityUsd)
        {
            var tr = new Repositories.TransactionsRepository();
            try
            {
                tr.ReplenishTheBalance(userId, quantityUsd);
                return Ok("Balance replenished successfully");
            }
            catch(Exception)
            {
                return BadRequest("Unable to replenish the balance");
            }
        }
}