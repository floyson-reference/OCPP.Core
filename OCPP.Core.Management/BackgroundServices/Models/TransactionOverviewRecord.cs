namespace OCPP.Core.Management.BackgroundServices.Models
{
    public record TransactionOverviewRecord(string Periode, double kWh, float Tarief)
    {
        public double Totaal => kWh * Tarief;
    }
}
