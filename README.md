# TrailingCryptobot
This is a simple automated cryptocurrency trading bot that uses the Coinbase Pro API. It can be used with multiple accounts and is intended to be used with a task scheduler for regular execution.

How to create a Coinbase Pro API key: https://help.coinbase.com/en/pro/other-topics/api/how-do-i-create-an-api-key-for-coinbase-pro

Populate the data in the keys.csv file. The format for each row should be: Name,Email,Passphrase,Secret,Key,Sandbox,Coin,BuyTrailPercent,SellTrailPercent,StopLossPercent,IsStopLossEnabled.

# Examples
Name: John Smith<br />
Email: someone@example.com<br />
Passphrase: [Generated from your API key]<br />
Secret: [Generated from your API key]<br />
Key: [Generated from your API key]<br />
Sandbox: true or false<br />
Coin: BTC-USD<br />
BuyTrailPercent: 0.02<br />
SellTrailPercent: 0.02<br />
StopLossPercent: 0.02<br />
IsStopLossEnabled: true<br />
