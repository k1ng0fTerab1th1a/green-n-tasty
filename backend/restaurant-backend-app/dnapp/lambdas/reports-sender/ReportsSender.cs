using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Restaurant.Reports.Application;
using Restaurant.Reports.Application.Exporters;
using Restaurant.Reports.Domain.Data;
using Restaurant.Reports.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Restaurant.ReportsSender;

public class ReportsSender
{
    private readonly ReportService _reportService;
    private readonly ReportEmailService _emailService;

    public ReportsSender()
    {
        var repository = new ReportsRepository();
        _reportService = new ReportService(repository);
        _emailService = new ReportEmailService(new AmazonSimpleEmailServiceClient());
    }

    public async Task FunctionHandler(CloudWatchEvent<object> @event, ILambdaContext context)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(context.RemainingTime.TotalSeconds - 1));
        var ct = cts.Token;

        context.Logger.LogInformation("Weekly report triggered");

        var now = DateTime.UtcNow;
        var range = new DateRange(now.AddDays(-7), now);
        var comparisonRange = new DateRange(now.AddDays(-14), now.AddDays(-7));

        var reportResult = await _reportService.GetFullReportAsync(range, comparisonRange, ct);
        if (reportResult.IsFailed)
        {
            context.Logger.LogError($"Report generation failed: {string.Join("; ", reportResult.Errors)}");
            return;
        }

        var excel = new ExcelReportExporter().Export(reportResult.Value);
        var csvFiles = new CsvReportExporter().Export(reportResult.Value);

        await _emailService.SendWeeklyReportAsync(excel, csvFiles, range.From, range.To, ct);

        context.Logger.LogInformation("Report email sent successfully");
    }
}