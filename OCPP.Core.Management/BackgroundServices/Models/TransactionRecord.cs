using System;

namespace OCPP.Core.Management.BackgroundServices.Models
{
    public record TransactionRecord(
        string Connector,
        DateTime Start,
        double MeterStart,
        double? MeterStop
        )
    {
        public string StopTag => "Frederic";
        public double? Charged => MeterStop - MeterStart;
    }
}
