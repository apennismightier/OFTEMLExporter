using Aspose.Email;
using Aspose.Email.Mime;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();

app.MapGet("/", () => Results.Text("OFT+EML Exporter is running. POST /preview or /export"));

app.MapPost("/preview", ([FromBody] ComposeRequest req) =>
{
    var message = BuildMessage(req);
    var normalized = new NormalizedFields(
        message.To.Select(x => x.Address).Distinct().ToArray(),
        message.CC.Select(x => x.Address).Distinct().ToArray(),
        message.Bcc.Select(x => x.Address).Distinct().ToArray(),
        DetectPlaceholders(message.HtmlBody)
    );
    return Results.Json(new {
        subject = message.Subject,
        htmlPreview = message.HtmlBody,
        textPreview = message.Body,
        normalized
    });
});

app.MapPost("/export", ([FromBody] ExportRequest req) =>
{
    var message = BuildMessage(req);
    var formats = req.Formats?.Select(f => f.ToLowerInvariant()).ToHashSet() ?? new HashSet<string>(new []{"oft","eml"});
    var files = new List<object>();

    if (formats.Contains("oft"))
    {
        using var ms = new MemoryStream();
        message.Save(ms, SaveOptions.DefaultOft);
        files.Add(new {
            filename = Sanitize(req.Subject) + ".oft",
            mime = "application/vnd.ms-outlook",
            contentBase64 = Convert.ToBase64String(ms.ToArray())
        });
    }
    if (formats.Contains("eml"))
    {
        using var ms = new MemoryStream();
        message.Save(ms, SaveOptions.DefaultEml);
        files.Add(new {
            filename = Sanitize(req.Subject) + ".eml",
            mime = "message/rfc822",
            contentBase64 = Convert.ToBase64String(ms.ToArray())
        });
    }
    return Results.Json(new { files });
});

app.Run();

// ---- Helpers & DTOs ----

static MailMessage BuildMessage(ComposeRequest req)
{
    var msg = new MailMessage
    {
        Subject = req.Subject ?? string.Empty,
        IsBodyHtml = !string.IsNullOrWhiteSpace(req.HtmlBody),
        HtmlBody = req.HtmlBody ?? string.Empty,
        Body = req.TextBody ?? string.Empty
    };

    AddAddresses(msg.To, req.To);
    AddAddresses(msg.CC, req.Cc);
    AddAddresses(msg.Bcc, req.Bcc);

    if (req.Attachments != null)
    {
        foreach (var a in req.Attachments)
        {
            if (string.IsNullOrWhiteSpace(a.ContentBase64) || string.IsNullOrWhiteSpace(a.Filename)) continue;
            var bytes = Convert.FromBase64String(a.ContentBase64);
            var stream = new MemoryStream(bytes);
            var att = new Attachment(stream, a.Filename, string.IsNullOrWhiteSpace(a.Mime) ? "application/octet-stream" : a.Mime);
            att.NeedRecomputeContentType = false;
            msg.Attachments.Add(att);
        }
    }

    return msg;
}

static void AddAddresses(MailAddressCollection col, IEnumerable<string>? input)
{
    if (input == null) return;
    foreach (var raw in input.SelectMany(SplitAddresses))
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0) continue;
        try { col.Add(trimmed); } catch { /* skip invalid */ }
    }
}

static IEnumerable<string> SplitAddresses(string s)
    => s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

static string[] DetectPlaceholders(string? html)
{
    if (string.IsNullOrEmpty(html)) return Array.Empty<string>();
    var m = System.Text.RegularExpressions.Regex.Matches(html, @"\{\{[^}]+\}\}");
    return m.Select(x => x.Value).Distinct().ToArray();
}

static string Sanitize(string? s)
{
    s ??= "Message";
    foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
    return string.IsNullOrWhiteSpace(s) ? "Message" : s;
}

public record ComposeRequest(
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    IEnumerable<string>? To,
    IEnumerable<string>? Cc,
    IEnumerable<string>? Bcc,
    IEnumerable<AttachmentDto>? Attachments,
    bool FromChain = false,
    ChainFileDto? ChainFile = null,
    string? PlatformHint = null
);

public record ExportRequest(
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    IEnumerable<string>? To,
    IEnumerable<string>? Cc,
    IEnumerable<string>? Bcc,
    IEnumerable<AttachmentDto>? Attachments,
    bool FromChain = false,
    ChainFileDto? ChainFile = null,
    string? PlatformHint = null,
    IEnumerable<string>? Formats = null
);

public record AttachmentDto(string Filename, string ContentBase64, string? Mime);
public record ChainFileDto(string Filename, string ContentBase64, string? Mime);
public record NormalizedFields(string[] To, string[] Cc, string[] Bcc, string[] Placeholders);