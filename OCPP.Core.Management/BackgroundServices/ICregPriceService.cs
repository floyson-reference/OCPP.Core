using OCPP.Core.Management.BackgroundServices.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OCPP.Core.Management.BackgroundServices
{
    public interface ICregPriceService
    {
        Task<List<CregPrice>> GetCregPricesAsync();
    }
}