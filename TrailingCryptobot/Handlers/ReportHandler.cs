using CoinbasePro.Services.Accounts.Models;
using CoinbasePro.Services.Reports.Types;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrailingCryptobot.Models;

namespace TrailingCryptobot.Handlers
{
    public class ReportHandler
    {
        private PrivateClient _client;
        private Account _account;
        private const string FILENAME = "reports.csv";

        public ReportHandler(PrivateClient client, Account account)
        {
            _client = client;
            _account = account;
        }

        public async Task HandleReporting()
        {
            var lastExecutionTime = GetLastExecutionTime();
            var now = DateTime.UtcNow;

            if(lastExecutionTime < now.AddDays(-1))
            {
                Log.Information($"Sending reports for {_client.Name} on {_client.Coin} account.");
                await _client.ReportsService.CreateNewAccountReportAsync(lastExecutionTime, now, _account.Id.ToString(), email: _client.Email, fileFormat: FileFormat.Pdf);
                Common.ThrottleSpeedPrivate();
                await _client.ReportsService.CreateNewFillsReportAsync(lastExecutionTime, now, _client.Coin, email: _client.Email, fileFormat: FileFormat.Pdf);
                Common.ThrottleSpeedPrivate();
                SetLastExecutionTime(now);
                Log.Information("Reporting finished.");
            }
        }

        private void SetLastExecutionTime(DateTime dateTime)
        {
            var contents = File.ReadAllLines(FILENAME);
            var reportRecord = contents.FirstOrDefault(x => x.Contains(_client.Name));

            if(reportRecord != null)
            {
                contents = contents.Where(x => x != reportRecord).ToArray();
                File.Delete(FILENAME);
                File.AppendAllLines(FILENAME, contents);
            }

            var newRecord = $"{_client.Name},{DateTime.UtcNow.ToString()}";

            File.AppendAllLines(FILENAME, new string[] { newRecord });
        }

        private DateTime GetLastExecutionTime()
        {
            var contents = File.ReadAllLines(FILENAME);
            var reportRecord = contents.FirstOrDefault(x => x.Contains(_client.Name));
            var timeString = reportRecord.Split(',').ElementAtOrDefault(1);

            if(string.IsNullOrWhiteSpace(timeString))
            {
                return DateTime.UtcNow.AddDays(-1);
            }

            if(DateTime.TryParse(timeString, out var time))
            {
                return time;
            }
            else
            {
                return DateTime.UtcNow.AddDays(-1);
            }
        }
    }
}
