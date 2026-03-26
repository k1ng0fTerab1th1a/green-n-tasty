using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Restaurant.ReportsSender;

public class ReportEmailService
{
    private readonly IAmazonSimpleEmailService _ses;
    private readonly string _fromEmail;
    private readonly List<string> _toEmails;

    public ReportEmailService(IAmazonSimpleEmailService ses)
    {
        _ses = ses;
        _fromEmail = Environment.GetEnvironmentVariable("REPORT_FROM_EMAIL")
            ?? throw new InvalidOperationException("REPORT_FROM_EMAIL env var is not set");

        var toRaw = Environment.GetEnvironmentVariable("REPORT_TO_EMAILS")
            ?? throw new InvalidOperationException("REPORT_TO_EMAILS env var is not set");

        _toEmails = toRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    public async Task SendWeeklyReportAsync(
        byte[] excelFile,
        Dictionary<string, byte[]> csvFiles,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken ct)
    {
        var boundary = $"boundary_{Guid.NewGuid():N}";
        var sb = new StringBuilder();

        sb.AppendLine($"From: {_fromEmail}");
        sb.AppendLine($"To: {string.Join(", ", _toEmails)}");
        sb.AppendLine($"Subject: Weekly Restaurant Report ({periodStart:dd.MM.yyyy} - {periodEnd:dd.MM.yyyy})");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        sb.AppendLine();

        // Text body
        sb.AppendLine($"--{boundary}");
        sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
        sb.AppendLine();
        sb.AppendLine($"Weekly report for period {periodStart:dd.MM.yyyy} - {periodEnd:dd.MM.yyyy}.");
        sb.AppendLine("See attached files.");
        sb.AppendLine();

        // Excel attachment
        var excelName = $"report_{periodStart:yyyyMMdd}_{periodEnd:yyyyMMdd}.xlsx";
        AppendAttachment(sb, boundary, excelName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelFile);

        // CSV attachments
        foreach (var (fileName, content) in csvFiles)
        {
            AppendAttachment(sb, boundary, fileName, "text/csv", content);
        }

        sb.AppendLine($"--{boundary}--");

        var rawBytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var rawStream = new MemoryStream(rawBytes);

        await _ses.SendRawEmailAsync(new SendRawEmailRequest
        {
            RawMessage = new RawMessage { Data = rawStream }
        }, ct);
    }

    private static void AppendAttachment(
        StringBuilder sb, string boundary, string fileName, string contentType, byte[] data)
    {
        sb.AppendLine($"--{boundary}");
        sb.AppendLine($"Content-Type: {contentType}; name=\"{fileName}\"");
        sb.AppendLine("Content-Transfer-Encoding: base64");
        sb.AppendLine($"Content-Disposition: attachment; filename=\"{fileName}\"");
        sb.AppendLine();
        sb.AppendLine(Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks));
        sb.AppendLine();
    }
}
