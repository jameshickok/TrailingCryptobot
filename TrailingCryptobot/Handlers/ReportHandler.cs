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
                var acctRpt = await _client.ReportsService.CreateNewAccountReportAsync(lastExecutionTime, now, _account.Id.ToString(), email: _client.Email, fileFormat: FileFormat.Pdf);
                Common.ThrottleSpeedPrivate();

                while(acctRpt.Status != ReportStatus.Ready)
                {
                    acctRpt = await _client.ReportsService.GetReportStatus(acctRpt.Id.ToString());
                    Common.ThrottleSpeedPrivate();
                }

                var fillsRpt = await _client.ReportsService.CreateNewFillsReportAsync(lastExecutionTime, now, _client.Coin, email: _client.Email, fileFormat: FileFormat.Pdf);
                Common.ThrottleSpeedPrivate();

                while (fillsRpt.Status != ReportStatus.Ready)
                {
                    fillsRpt = await _client.ReportsService.GetReportStatus(fillsRpt.Id.ToString());
                    Common.ThrottleSpeedPrivate();
                }

                SetLastExecutionTime(now);
                Log.Information("Reporting finished.");
            }
        }

        private void SetLastExecutionTime(DateTime dateTime)
        {
            var contents = File.ReadAllLines(FILENAME);

            if(contents != null)
            {
                var reportRecord = contents.FirstOrDefault(x => x.Contains(_client.Name));

                if (reportRecord != null)
                {
                    contents = contents.Where(x => x != reportRecord).ToArray();
                    File.Delete(FILENAME);
                    File.AppendAllLines(FILENAME, contents);
                }
            }
            
            var newRecord = $"{_client.Name},{DateTime.UtcNow.ToString()}";

            File.AppendAllLines(FILENAME, new string[] { newRecord });
        }

        private DateTime GetLastExecutionTime()
        {
            var yesterday = DateTime.UtcNow.AddDays(-1);
            var contents = File.ReadAllLines(FILENAME);

            if(contents == null)
            {
                return yesterday;
            }

            var reportRecord = contents.FirstOrDefault(x => x.Contains(_client.Name));

            if (reportRecord == null)
            {
                return yesterday;
            }

            var timeString = reportRecord.Split(',').ElementAtOrDefault(1);

            if(string.IsNullOrWhiteSpace(timeString))
            {
                return yesterday;
            }

            if(DateTime.TryParse(timeString, out var time))
            {
                return time;
            }
            else
            {
                return yesterday;
            }
        }
    }
}
