using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using CsvHelper.Configuration;
using CsvHelper;
using System.Text;
using System.Threading.Tasks;
using OCPP.Core.Management.BackgroundServices.Models;

namespace OCPP.Core.Management.BackgroundServices.Impl
{
    public class CregPriceService(IHttpClientFactory httpClientFactory) : ICregPriceService
    {
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
        private readonly CultureInfo DutchBelgiumCulture = CultureInfo.CreateSpecificCulture("nl-BE");

        public async Task<List<CregPrice>> GetCregPricesAsync()
        {
            var client = httpClientFactory.CreateClient();
            var url = "https://www.creg.be/sites/default/files/assets/Prices/CREG_Tariff_EV.csv";

            using var stream = await client.GetStreamAsync(url);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var config = new CsvConfiguration(DutchBelgiumCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true
            };

            using var csv = new CsvReader(reader, config);
            var result = new List<CregPrice>();

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var year = csv.GetField<int>(0);
                var month = csv.GetField<int>(1);
                var price = csv.GetField<float>(2);
                var price3MonthsAvg = csv.GetField<float?>(3);
                                
                result.Add(new CregPrice { Year = year, Month = month, PriceInEuroCent = price, Price3MonthsAverageInEuroCent = price3MonthsAvg });
            }

            return result;
        }
    }
}
