namespace OCPP.Core.Management.BackgroundServices.Models
{
    public class CregPrice
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public float PriceInEuroCent { get; set; }
        public float? Price3MonthsAverageInEuroCent { get; set; }
    }
}
