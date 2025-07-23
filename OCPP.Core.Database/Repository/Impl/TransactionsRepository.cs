using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace OCPP.Core.Database.Repository.Impl
{
    public class TransactionsRepository(OCPPCoreContext dbContext) : ITransactionsRepository
    {
        private readonly OCPPCoreContext dbContext = dbContext;

        public IList<TransactionLight> GetTransactions(DateTime sinceDt, DateTime untilDt)
        {
            return [.. dbContext.Transactions
                    .Where(t => t.StartTime >= sinceDt && t.StartTime <= untilDt)
                    .OrderBy(t => t.StartTime)
                    .Select(t => new TransactionLight(t.StartTime, t.MeterStart, t.MeterStop, t.ChargePoint.Name))
                    .ToList()];
        }
    }
}
