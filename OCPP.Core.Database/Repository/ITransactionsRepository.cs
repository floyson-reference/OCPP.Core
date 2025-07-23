using System;
using System.Collections.Generic;

namespace OCPP.Core.Database.Repository
{
    public interface ITransactionsRepository
    {
        IList<TransactionLight> GetTransactions(DateTime sinceDt, DateTime untilDt);
    }
}
