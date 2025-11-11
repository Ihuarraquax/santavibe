using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace SantaVibe.Api.Services.Email;

/// <summary>
/// Email service implementation using Resend API
/// </summary>
public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _baseUrl;

    public ResendEmailService(
        HttpClient httpClient,
        IOptions<ResendOptions> options,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"]
            ?? throw new InvalidOperationException("App:BaseUrl not configured");

        // Configure HttpClient for Resend API
        _httpClient.BaseAddress = new Uri("https://api.resend.com");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
    }

    public async Task<EmailResult> SendDrawCompletedEmailAsync(
        string recipientEmail,
        string recipientName,
        string groupName,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var viewAssignmentUrl = $"{_baseUrl}/groups/{groupId}";

        var subject = "🎅 Losowanie Świętego Mikołaja zostało zakończone!";
        var htmlBody = BuildDrawCompletedHtml(recipientName, groupName, viewAssignmentUrl);
        var textBody = BuildDrawCompletedText(recipientName, groupName, viewAssignmentUrl);

        return await SendEmailAsync(
            recipientEmail,
            recipientName,
            subject,
            htmlBody,
            textBody,
            cancellationToken);
    }

    public async Task<EmailResult> SendWishlistUpdatedEmailAsync(
        string recipientEmail,
        string recipientName,
        string groupName,
        Guid groupId,
        string recipientFirstName,
        CancellationToken cancellationToken = default)
    {
        var viewWishlistUrl = $"{_baseUrl}/groups/{groupId}/wishlist";

        var subject = $"🎁 {recipientFirstName} zaktualizował(a) swoją listę życzeń";
        var htmlBody = BuildWishlistUpdatedHtml(recipientName, groupName, recipientFirstName, viewWishlistUrl);
        var textBody = BuildWishlistUpdatedText(recipientName, groupName, recipientFirstName, viewWishlistUrl);

        return await SendEmailAsync(
            recipientEmail,
            recipientName,
            subject,
            htmlBody,
            textBody,
            cancellationToken);
    }

    private async Task<EmailResult> SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                from = $"{_options.FromName} <{_options.FromEmail}>",
                to = new[] { $"{recipientName} <{recipientEmail}>" },
                subject,
                html = htmlBody,
                text = textBody
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/emails",
                payload,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent successfully to {Email} with subject '{Subject}'",
                    recipientEmail,
                    subject);
                return EmailResult.Success();
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Email sending failed with status {StatusCode}: {Error}",
                response.StatusCode,
                errorContent);

            return EmailResult.Failure(
                $"Email service returned {response.StatusCode}",
                response.StatusCode.ToString());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while sending email to {Email}", recipientEmail);
            return EmailResult.Failure($"HTTP error: {ex.Message}", "HttpError");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Email sending to {Email} timed out", recipientEmail);
            return EmailResult.Failure("Request timed out", "Timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Email}", recipientEmail);
            return EmailResult.Failure($"Unexpected error: {ex.Message}", "UnexpectedError");
        }
    }

    private static string BuildDrawCompletedHtml(string recipientName, string groupName, string viewAssignmentUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;"">
        <h1 style=""color: white; margin: 0; font-size: 28px;"">🎅 Losowanie zakończone!</h1>
    </div>

    <div style=""background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;"">
        <p style=""font-size: 16px; margin-bottom: 20px;"">
            Cześć <strong>{recipientName}</strong>,
        </p>

        <p style=""font-size: 16px; margin-bottom: 20px;"">
            Świetna wiadomość! Losowanie Świętego Mikołaja dla grupy <strong>{groupName}</strong> zostało zakończone. 🎉
        </p>

        <p style=""font-size: 16px; margin-bottom: 30px;"">
            Zaloguj się do SantaVibe, aby zobaczyć, dla kogo będziesz kupować prezent w tym roku!
        </p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{viewAssignmentUrl}""
               style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                      color: white;
                      padding: 14px 32px;
                      text-decoration: none;
                      border-radius: 6px;
                      font-weight: bold;
                      display: inline-block;
                      font-size: 16px;"">
                Zobacz moje przydzielenie
            </a>
        </div>

        <p style=""font-size: 14px; color: #666; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;"">
            Pamiętaj: Zachowaj swoje przydzielenie w tajemnicy! To sprawia, że Święty Mikołaj jest zabawny. 🤫
        </p>

        <p style=""font-size: 14px; color: #666; margin-top: 20px;"">
            Udanych prezentów!<br>
            <strong>Zespół SantaVibe</strong>
        </p>
    </div>
</body>
</html>";
    }

    private static string BuildDrawCompletedText(string recipientName, string groupName, string viewAssignmentUrl)
    {
        return $@"Cześć {recipientName},

Świetna wiadomość! Losowanie Świętego Mikołaja dla grupy {groupName} zostało zakończone.

Zaloguj się do SantaVibe, aby zobaczyć, dla kogo będziesz kupować prezent w tym roku!

Zobacz swoje przydzielenie: {viewAssignmentUrl}

Pamiętaj: Zachowaj swoje przydzielenie w tajemnicy! To sprawia, że Święty Mikołaj jest zabawny.

Udanych prezentów!
Zespół SantaVibe";
    }

    private static string BuildWishlistUpdatedHtml(string recipientName, string groupName, string recipientFirstName, string viewWishlistUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;"">
        <h1 style=""color: white; margin: 0; font-size: 28px;"">🎁 Lista życzeń zaktualizowana!</h1>
    </div>

    <div style=""background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;"">
        <p style=""font-size: 16px; margin-bottom: 20px;"">
            Cześć <strong>{recipientName}</strong>,
        </p>

        <p style=""font-size: 16px; margin-bottom: 20px;"">
            Dobra wiadomość! <strong>{recipientFirstName}</strong> zaktualizował(a) swoją listę życzeń dla grupy <strong>{groupName}</strong>.
        </p>

        <p style=""font-size: 16px; margin-bottom: 30px;"">
            Sprawdź zaktualizowaną listę życzeń, aby znaleźć idealny prezent! 🎁
        </p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{viewWishlistUrl}""
               style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
                      color: white;
                      padding: 14px 32px;
                      text-decoration: none;
                      border-radius: 6px;
                      font-weight: bold;
                      display: inline-block;
                      font-size: 16px;"">
                Zobacz zaktualizowaną listę
            </a>
        </div>

        <p style=""font-size: 14px; color: #666; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;"">
            To powiadomienie zostało wysłane, ponieważ Twój obdarowany dokonał zmian, aby pomóc Ci znaleźć idealny prezent. 💝
        </p>

        <p style=""font-size: 14px; color: #666; margin-top: 20px;"">
            Udanych zakupów!<br>
            <strong>Zespół SantaVibe</strong>
        </p>
    </div>
</body>
</html>";
    }

    private static string BuildWishlistUpdatedText(string recipientName, string groupName, string recipientFirstName, string viewWishlistUrl)
    {
        return $@"Cześć {recipientName},

Dobra wiadomość! {recipientFirstName} zaktualizował(a) swoją listę życzeń dla grupy {groupName}.

Sprawdź zaktualizowaną listę życzeń, aby znaleźć idealny prezent!

Zobacz listę życzeń: {viewWishlistUrl}

To powiadomienie zostało wysłane, ponieważ Twój obdarowany dokonał zmian, aby pomóc Ci znaleźć idealny prezent.

Udanych zakupów!
Zespół SantaVibe";
    }
}

/// <summary>
/// Configuration options for Resend email service
/// </summary>
public class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@santavibe.com";
    public string FromName { get; set; } = "SantaVibe";
}
