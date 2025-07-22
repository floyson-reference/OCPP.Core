using System.Threading.Tasks;
using Hangfire;

namespace OCPP.Core.Management.BackgroundServices
{
    public interface IEmailExcelExportService
    {
        [DisableConcurrentExecution(0)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        Task ExportLastQuarterTransactionsAsync();
    }
}
