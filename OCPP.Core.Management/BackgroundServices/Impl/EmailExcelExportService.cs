using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;
using System.Threading.Tasks;
using ClosedXML.Excel;
using OCPP.Core.Database;
using OCPP.Core.Database.Repository;
using OCPP.Core.Management.BackgroundServices.Models;
using Microsoft.Extensions.Options;

namespace OCPP.Core.Management.BackgroundServices.Impl
{
    public class EmailExcelExportService(
        ITransactionsRepository transactionsRepository,
        ICregPriceService cregPriceService,
        IOptions<SmtpSettings> options) : IEmailExcelExportService
    {
        private readonly ITransactionsRepository transactionsRepository = transactionsRepository;
        private readonly ICregPriceService cregPriceService = cregPriceService;
        private readonly SmtpSettings smtpSettings = options.Value;
        private readonly CultureInfo DutchBelgiumCulture = CultureInfo.CreateSpecificCulture("nl-BE");
        


        public async Task ExportLastQuarterTransactionsAsync()
        {
            var startOfPreviousQuarter = GetStartOfPreviousQuarter();
            var sinceDt = startOfPreviousQuarter.StartPreviousQuarter;
            var untilDt = sinceDt.AddMonths(3).AddSeconds(-1);

            var lastQuarterTransactions = transactionsRepository.GetTransactions(sinceDt, untilDt).Where(t => t.MeterStop !=  t.MeterStart).ToList();

            await ExportToExcelAsync(lastQuarterTransactions, startOfPreviousQuarter.Quarter, sinceDt);
        }

        private async Task ExportToExcelAsync(IEnumerable<TransactionLight> transactions, int quarter, DateTime quarterStartDate)
        {
            var months = transactions.DistinctBy(t => t.StartTime.Month).Select(t => t.StartTime.Month);

            using var wb = new XLWorkbook();

            foreach (var month in months)
            {
                var monthName = transactions.First(t => t.StartTime.Month == month).StartTime.ToString("MMMM", DutchBelgiumCulture);
                var ws = wb.AddWorksheet(monthName);

                ws.ColumnWidth = 21;

                var monthTransactions = transactions
                    .Where(t => t.StartTime.Month == month)
                    .Select(t => new TransactionRecord(t.ChargePointName, t.StartTime, t.MeterStart, t.MeterStop))
                    .ToList();
                var table = ws.FirstCell().InsertTable(monthTransactions, $"tbl{monthName}");


                table.ShowTotalsRow = true;
                table.Field(nameof(TransactionRecord.Charged)).TotalsRowFunction = XLTotalsRowFunction.Sum;
                var ranges = ws.Ranges($"C2:D{monthTransactions.Count + 1},F2:F{monthTransactions.Count + 2}");
                foreach (var range in ranges)
                {
                    range.Style.NumberFormat.SetFormat("#,##0.00");
                }
            }

            await AddFirstWorksheetAsync(wb, transactions, quarter, quarterStartDate);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            SendEmailWithExcelFile(ms, quarter, quarterStartDate.Year);
        }

        private async Task AddFirstWorksheetAsync(XLWorkbook xLWorkbook, IEnumerable<TransactionLight> transactions, int quarter, DateTime quarterStartDate)
        {
            var start2QuartersAgo = quarterStartDate.AddMonths(-3);
            var cregPrice3MonthsAvg = await GetQuarterPrice3MonthsAvgInEuroAsync(start2QuartersAgo.Year, start2QuartersAgo.Month);

            var groupedTransactions = transactions
                .GroupBy(t => t.StartTime.Month)
                .Select(g => new TransactionOverviewRecord(g.First().StartTime.ToString("MMMM", DutchBelgiumCulture), g.Sum(t => t.MeterStop - t.MeterStart ?? 0), cregPrice3MonthsAvg))
                .ToList();

            var ws = xLWorkbook.AddWorksheet("Overzicht", 1);
            ws.Column(1).Width = 11;
            ws.Column(2).Width = 22;
            ws.Column(3).Width = 9;
            ws.Column(4).Width = 14;

            ws.Cell("D1").Value = "Onkostennota";
            ws.Cell("D1").Style.Font.Bold = true;
            ws.Cell("D1").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Cell("A1").Value = "Naam: Frederic Loyson";
            ws.Cell("A2").Value = "Adres: Acacialaan 42, 8550 Zwevegem";
            ws.Cell("A5").Value = "Periode";
            ws.Cell("B5").Value = $"Kwartaal {quarter} - {quarterStartDate.Year}";
            ws.Cell("A7").Value = "Aan";
            ws.Cell("B7").Value = "Naam vennootschap:";
            ws.Cell("C7").Value = "L-Soft BV";
            ws.Cell("B8").Value = "Adres:";
            ws.Cell("C8").Value = "Acacialaan 42, 8550 Zwevegem";
            ws.Cell("B9").Value = "Ondernemingsnummer:";
            ws.Cell("C9").Value = "BE 0800.726.189";

            var table = ws.Cell("A12").InsertTable(groupedTransactions);
            table.ShowTotalsRow = true;
            table.Field(nameof(TransactionOverviewRecord.Totaal)).TotalsRowFunction = XLTotalsRowFunction.Sum;

            var row = table.RangeAddress.FirstAddress.RowNumber + 1;

            foreach (var groupedTransaction in groupedTransactions)
            {
                var periodWorksheet = xLWorkbook.Worksheet(groupedTransaction.Periode);
                var periodTable = periodWorksheet.Table($"tbl{groupedTransaction.Periode}");
                var totalRow = periodTable.RangeAddress.LastAddress.RowNumber;
                ws.Cell($"B{row}").SetFormulaA1($"{groupedTransaction.Periode}!F{totalRow}");
                ws.Cell($"D{row}").SetFormulaA1($"=B{row}*C{row}");
                row++;
            }

            ws.Cell("A16").Value = "Totaal:";
            ws.Cells("B12:B15").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cells("C12:C15").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cells("D12:D15").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Cells("B13:B15").Style.NumberFormat.SetFormat("#,##0.00");
            ws.Cells("C13:C16").Style.NumberFormat.SetFormat("€ #,##0.0000");
            ws.Cells("D13:D16").Style.NumberFormat.SetFormat("€ #,##0.00");
            ws.Cells("A16:D16").Style.Font.Bold = true;

            ws.Cell("A18").Value = "Over te schrijven op rekeningnummer van Dhr & Mevr Loyson Tack";
        }

        private static (DateTime StartPreviousQuarter, int Quarter, int Year) GetStartOfPreviousQuarter()
        {
            var today = DateTime.Today;

            int currentQuarter = (today.Month - 1) / 3 + 1;

            int previousQuarter = currentQuarter - 1;
            int year = today.Year;

            if (previousQuarter == 0)
            {
                previousQuarter = 4;
                year--;
            }

            int startMonth = (previousQuarter - 1) * 3 + 1;

            return (new DateTime(year, startMonth, 1), previousQuarter, year);
        }

        private async Task<float> GetQuarterPrice3MonthsAvgInEuroAsync(int year, int month)
        {
            var cregPrices = await cregPriceService.GetCregPricesAsync();

            var price = cregPrices.FirstOrDefault(p => p.Year == year && p.Month == month)?.Price3MonthsAverageInEuroCent;

            return (price ?? 32F) / 100;
        }

        public void SendEmailWithExcelFile(Stream excelStream, int quarter, int year)
        {
            var from = new MailAddress("f.loyson.dias@telenet.be", "Laadpaal Zwevegem");
            var to = new MailAddress("f.loyson@telenet.be");

            using var message = new MailMessage(from, to);
            message.Subject = $"Laadpaal transacties: kwartaal {quarter} - {year}";
            message.Body = "Zie bijlage met het Excel-bestand.";

            // Bijlage maken vanuit stream
            var attachment = new Attachment(excelStream, "rapport.xlsx", MediaTypeNames.Application.Octet);
            message.Attachments.Add(attachment);

            using var smtp = new SmtpClient(smtpSettings.Server)
            {
                Port = 587,
                Credentials = new NetworkCredential(smtpSettings.UserName, smtpSettings.Password),
                EnableSsl = true
            };

            smtp.Send(message);
        }
    }
}
