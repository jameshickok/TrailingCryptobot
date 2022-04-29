using CoinbasePro.Services.Accounts.Models;
using CoinbasePro.Services.Orders.Models.Responses;
using CoinbasePro.Services.Orders.Types;
using CoinbasePro.Services.Products.Models;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using TrailingCryptobot.Models;

namespace TrailingCryptobot.Handlers
{
    public class BuyHandler
    {
        private PrivateClient _client;
        private Account _usdAccount;
        private Product _product;

        public BuyHandler(PrivateClient client, Product product, Account usdAccount)
        {
            _client = client;
            _usdAccount = usdAccount;
            _product = product;
        }
        
        public async Task HandleTrailStop()
        {
            var ticker = await _client.ProductsService.GetProductTickerAsync(_client.Coin);
            Common.ThrottleSpeedPublic();

            var stopPrice = ticker.Price + (ticker.Price * _client.TrailPercent);
            var limitPrice = stopPrice + (ticker.Price * _client.TrailPercent);
            stopPrice = Common.GetTruncatedValue(stopPrice, _product.QuoteIncrement);
            limitPrice = Common.GetTruncatedValue(limitPrice, _product.QuoteIncrement);

            var orders = await Common.GetOrders(_client);
            var buyOrders = orders.Where(x => x.Side == OrderSide.Buy);

            if(buyOrders.Any())
            {
                // Trail stop buy orders
                foreach (var order in buyOrders)
                {
                    try
                    {
                        var createNewOrder = false;

                        if (
                            order.Status == OrderStatus.Active &&
                            stopPrice < order.StopPrice
                            )
                        {
                            createNewOrder = true;
                        }

                        if (
                            order.Status == OrderStatus.Open &&
                            limitPrice < order.Price
                            )
                        {
                            createNewOrder = true;
                        }

                        if (createNewOrder)
                        {
                            await _client.OrdersService.CancelOrderByIdAsync(order.Id.ToString());
                            Common.ThrottleSpeedPrivate();

                            await PlaceOrder(order, limitPrice, stopPrice);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, ex.Message);
                        if (ex.InnerException != null)
                        {
                            Log.Error(ex.InnerException, ex.InnerException.Message);
                        }
                    }
                }
            }
            else
            {
                // First buy order
                var investment = _usdAccount.Available * (decimal)0.94;
                var size = investment / limitPrice;
                size = Common.GetTruncatedValue(size, _product.BaseIncrement);

                await Common.PlaceOrder(_client, OrderSide.Buy, _client.Coin, size, limitPrice, stopPrice, null);
            }
        }

        private async Task PlaceOrder(OrderResponse order, decimal limitPrice, decimal stopPrice)
        {
            await Common.PlaceOrder(_client, order.Side, order.ProductId, order.Size, limitPrice, stopPrice, order.Price);
        }
    }
}
