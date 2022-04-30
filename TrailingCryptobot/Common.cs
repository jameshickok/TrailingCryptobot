using CoinbasePro;
using CoinbasePro.Services.Orders.Models.Responses;
using CoinbasePro.Services.Orders.Types;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrailingCryptobot.Models;

namespace TrailingCryptobot
{
    public static class Common
    {
        public static void ThrottleSpeedPublic()
        {
            Thread.Sleep(300);
        }

        public static void ThrottleSpeedPrivate()
        {
            Thread.Sleep(200);
        }

        public static decimal GetTruncatedValue(decimal value, decimal increment)
        {
            var remainder = value % increment;

            if (remainder > 0)
            {
                value -= remainder;
            }

            return value;
        }

        public static async Task<IEnumerable<OrderResponse>> GetOrders(CoinbaseProClient client)
        {
            var orderResult =  await client.OrdersService.GetAllOrdersAsync(new CoinbasePro.Services.Orders.Types.OrderStatus[] {
                 OrderStatus.Active,
                 OrderStatus.Open
            });
            ThrottleSpeedPrivate();
            return orderResult.SelectMany(x => x);
        }

        public static async Task PlaceOrder(PrivateClient client, OrderSide side, string productId, decimal size, decimal limitPrice, decimal stopPrice, decimal? oldPrice)
        {
            var newOrder = await client.OrdersService.PlaceStopOrderAsync(
                                 side,
                                 productId,
                                 size,
                                 limitPrice,
                                 stopPrice
                            );
            Common.ThrottleSpeedPrivate();

            var feeRates = await client.FeesService.GetCurrentFeesAsync();
            Common.ThrottleSpeedPrivate();

            if(side == OrderSide.Buy)
            {
                // records.csv = "name,coin,price,fee"
                var filename = "records.csv";
                var contents = File.ReadAllLines(filename);

                if(contents != null)
                {
                    var oldRecord = contents.FirstOrDefault(x => x.Contains(client.Name) && x.Contains(client.Coin));
                    if (oldRecord != null)
                    {
                        contents = contents.Where(x => x != oldRecord && x != Environment.NewLine && !string.IsNullOrWhiteSpace(x)).ToArray();
                        File.Delete(filename);
                        File.AppendAllLines(filename, contents);
                    }
                }
                
                var newRecord = $"{client.Name},{client.Coin},{limitPrice},{feeRates.TakerFeeRate}";
                File.AppendAllLines(filename, new string[] { newRecord });
            }
            
            if (oldPrice == null)
            {
                Log.Information($"Created {client.Name}'s {client.Coin} {side.ToString()} order priced at ${Math.Round(limitPrice, 6)};");
            }
            else
            {
                Log.Information($"Pushed {client.Name}'s {client.Coin} {side.ToString()} order price from ${Math.Round(oldPrice.Value, 6)} to ${Math.Round(limitPrice, 6)}.");
            }
        }
    }
}
