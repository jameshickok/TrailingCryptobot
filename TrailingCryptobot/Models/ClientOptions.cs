namespace TrailingCryptobot.Models
{
    public class ClientOptions
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Passphrase { get; set; }

        public string Secret { get; set; }

        public string Key { get; set; }

        public bool Sandbox { get; set; }

        public string Coin { get; set; }

        public decimal BuyTrailPercent { get; set; }

        public decimal SellTrailPercent { get; set; }

        public decimal StopLossPercent { get; set; }

        public bool IsStopLossEnabled { get; set; }
    }
}
