using CoinbasePro.Network.Authentication;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrailingCryptobot.Models;

namespace TrailingCryptobot
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                           .MinimumLevel.Information()
                           .WriteTo.Console()
                           .WriteTo.File("log.txt",
                               rollingInterval: RollingInterval.Day,
                               rollOnFileSizeLimit: true,
                               retainedFileCountLimit: 3)
                           .CreateLogger();

            Log.Information("This cryptobot simply trails the price down to buy and up to sell for your specified cryptocurrency while using your Coinbase Pro API.");

            var clientOptions = InitializePrivateClientOptions();

            foreach (var option in clientOptions)
            {
                try
                {
                    var authenticator = new Authenticator(option.Key, option.Secret, option.Passphrase);

                    using (var client = new PrivateClient(option.Name, option.Email, authenticator, option.Sandbox, option.Coin, option.TrailPercent))
                    {
                        Log.Information($"Now managing {client.Name}'s account.");

                        var coinInfo = client.ProductsService.GetSingleProductAsync(client.Coin).Result;
                        Common.ThrottleSpeedPublic();
                        var accounts = client.AccountsService.GetAllAccountsAsync().Result;
                        Common.ThrottleSpeedPrivate();

                        var usdAccount = accounts.FirstOrDefault(x => x.Currency == "USD");
                        var coinAccount = accounts.FirstOrDefault(x => x.Currency == coinInfo.BaseCurrency);

                        if(coinAccount.Balance >= coinInfo.BaseMinSize)
                        {
                            var handler = new SellHandler(client, coinInfo, coinAccount);
                            
                            if(coinAccount.Available >= coinInfo.BaseMinSize)
                            {
                                // Coin has a balance without a sell order hold.
                                handler.HandleStopLoss().Wait();
                            }
                            else
                            {
                                // Coin currently has an active sell order - see if it can be improved.
                                handler.HandleTrailStop().Wait();
                            }
                        }
                        else
                        {
                            // Trail stop buy.
                            var handler = new BuyHandler(client, coinInfo, usdAccount);
                            
                            handler.HandleTrailStop().Wait();
                        }
                        Log.Information($"Done managing {client.Name}'s account.");
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

        private static List<ClientOptions> InitializePrivateClientOptions()
        {
            var options = new List<ClientOptions>();
            var contents = File.ReadAllLines("keys.csv");
            var headerRow = contents.FirstOrDefault();
            if (headerRow == "Name,Email,Passphrase,Secret,Key,Sandbox,Coin")
            {
                foreach (var friend in contents.Where(x => x != headerRow))
                {
                    var friendKeys = friend.Split(',');

                    if (friendKeys.Count() == 7)
                    {
                        var name = friendKeys.ElementAt(0);
                        var email = friendKeys.ElementAt(1);
                        var passphrase = friendKeys.ElementAt(2);
                        var secret = friendKeys.ElementAt(3);
                        var key = friendKeys.ElementAt(4);
                        var sandbox = friendKeys.ElementAt(5);
                        var coin = friendKeys.ElementAt(6);

                        var option = new ClientOptions
                        {
                            Name = name,
                            Email = email,
                            Passphrase = passphrase,
                            Secret = secret,
                            Key = key,
                            Sandbox = bool.Parse(sandbox),
                            Coin = coin
                        };

                        options.Add(option);
                    }
                }
            }
            return options;
        }
    }
}
