using CoinbasePro;
using CoinbasePro.Network.Authentication;
using System;

namespace TrailingCryptobot.Models
{
    public class PrivateClient : CoinbaseProClient, IDisposable
    {
        public PrivateClient(string name, string email, Authenticator authenticator, bool sandbox, string coin, decimal buyTrailPercent, decimal sellTrailPercent, decimal stopLossPercent, bool isStopLossEnabled) : base(authenticator, sandbox)
        {
            this.Name = name;
            this.Email = email;
            this.Sandbox = sandbox;
            this.Coin = coin;
            this.BuyTrailPercent = buyTrailPercent;
            this.SellTrailPercent = sellTrailPercent;
            this.StopLossPercent = stopLossPercent;
            this.IsStopLossEnabled = isStopLossEnabled;
        }

        public string Name { get; set; }

        /// <summary>
        /// For reports
        /// </summary>
        public string Email { get; set; }

        public bool Sandbox { get; set; }

        public string Coin { get; set; }

        public decimal BuyTrailPercent { get; set; }

        public decimal SellTrailPercent { get; set; }

        public decimal StopLossPercent { get; set; }

        public bool IsStopLossEnabled { get; set; }

        public void Dispose()
        {
            if (this.WebSocket?.State == WebSocket4Net.WebSocketState.Open || this.WebSocket?.State == WebSocket4Net.WebSocketState.Connecting)
            {
                this.WebSocket.Stop();

                while (this.WebSocket.State != WebSocket4Net.WebSocketState.Closed)
                {

                }
            }

            this.Name = null;
            this.Email = null;
            this.Coin = null;
        }
    }
}
