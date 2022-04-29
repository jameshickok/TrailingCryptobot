﻿using CoinbasePro.Services.Accounts.Models;
using CoinbasePro.Services.Orders.Models.Responses;
using CoinbasePro.Services.Orders.Types;
using CoinbasePro.Services.Products.Models;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrailingCryptobot.Models;

namespace TrailingCryptobot.Handlers
{
    public class SellHandler
    {
        private PrivateClient _client;
        private Account _coinAccount;
        private Product _product;

        public SellHandler(PrivateClient client, Product product, Account coinAccount)
        {
            _client = client;
            _coinAccount = coinAccount;
            _product = product;
        }

        public async Task HandleTrailStop()
        {
            var ticker = await _client.ProductsService.GetProductTickerAsync(_client.Coin);
            Common.ThrottleSpeedPublic();

            var stopPrice = ticker.Price - (ticker.Price * _client.TrailPercent);
            var limitPrice = stopPrice - (ticker.Price * _client.TrailPercent);
            stopPrice = Common.GetTruncatedValue(stopPrice, _product.QuoteIncrement);
            limitPrice = Common.GetTruncatedValue(limitPrice, _product.QuoteIncrement);

            var unitCost = GetCost();

            if(limitPrice > unitCost)
            {
                var orders = await Common.GetOrders(_client);
                var sellOrders = orders.Where(x => x.Side == OrderSide.Sell);

                foreach (var order in sellOrders)
                {
                    try
                    {
                        var createNewOrder = false;

                        if (
                            order.Status == OrderStatus.Active &&
                            stopPrice > order.StopPrice
                            )
                        {
                            createNewOrder = true;
                        }

                        if (
                            order.Status == OrderStatus.Open &&
                            limitPrice > order.Price
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

            
        }

        public async Task HandleStopLoss()
        {
            var unitCost = GetCost();
            var stopLossPrice = unitCost - (unitCost * _client.TrailPercent);
            var stopLossLimit = stopLossPrice - (unitCost * _client.TrailPercent);
            stopLossPrice = Common.GetTruncatedValue(stopLossPrice, _product.QuoteIncrement);
            stopLossLimit = Common.GetTruncatedValue(stopLossLimit, _product.QuoteIncrement);

            await Common.PlaceOrder(_client, OrderSide.Sell, _client.Coin, _coinAccount.Available, stopLossLimit, stopLossPrice, null);
        }

        private decimal GetCost()
        {
            // records.csv = "name,coin,price,fee"
            var contents = File.ReadAllLines("records.csv");
            var record = contents.FirstOrDefault(x => x.Contains(_client.Name) && x.Contains(_client.Coin));
            var priceString = record.Split(',').ElementAt(2);
            var feeString = record.Split(',').ElementAt(3);
            var price = decimal.Parse(priceString);
            var fee = decimal.Parse(feeString);
            return price + (price * fee);
        }

        private async Task PlaceOrder(OrderResponse order, decimal limitPrice, decimal stopPrice)
        {
            await Common.PlaceOrder(_client, order.Side, order.ProductId, order.Size, limitPrice, stopPrice, order.Price);
        }
    }
}