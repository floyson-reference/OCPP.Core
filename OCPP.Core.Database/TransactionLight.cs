using System;

namespace OCPP.Core.Database
{
    public record TransactionLight(DateTime StartTime, double MeterStart, double? MeterStop, string ChargePointName);
}
