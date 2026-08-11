using Draya.Application.Payments.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Draya.Infrastructure.Payments;

public class PaymobService : IPaymobService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public PaymobService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> CreateCheckoutUrlAsync(Guid paymentTransactionId, decimal amount, string email, string fullName, CancellationToken cancellationToken = default)
    {
        var secretKey = _configuration["PaymobSettings:SecretKey"] ?? string.Empty;
        var publicKey = _configuration["PaymobSettings:PublicKey"] ?? string.Empty;
        var integrationIdStr = _configuration["PaymobSettings:IntegrationId"] ?? "5850831";
        var baseUrl = _configuration["PaymobSettings:BaseUrl"] ?? "https://accept-alpha.paymob.com";

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Paymob SecretKey is not configured in appsettings.json.");
        }

        var amountCents = (int)(amount * 100);
        var nameParts = (fullName ?? "User").Split(' ');
        var firstName = nameParts.Length > 0 ? nameParts[0] : "User";
        var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "User";
        var userEmail = string.IsNullOrWhiteSpace(email) ? "user@draya.com" : email;

        // Paymob Intention API v1
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/intention/");
        request.Headers.TryAddWithoutValidation("Authorization", $"Token {secretKey}");

        var intentionPayload = new
        {
            amount = amountCents,
            currency = "EGP",
            payment_methods = new[] { int.TryParse(integrationIdStr, out var intId) ? intId : 0 },
            billing_data = new
            {
                first_name = firstName,
                last_name = lastName,
                email = userEmail,
                phone_number = "+201000000000"
            },
            special_reference = paymentTransactionId.ToString()
        };

        request.Content = new StringContent(JsonSerializer.Serialize(intentionPayload), Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Paymob Intention API Failed ({response.StatusCode}): {errBody}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        var clientSecret = doc.RootElement.GetProperty("client_secret").GetString();

        return $"{baseUrl}/unifiedcheckout/?publicKey={publicKey}&clientSecret={clientSecret}";
    }

    public bool VerifyHmac(IDictionary<string, string> queryOrBodyParams, string receivedHmac)
    {
        var hmacSecret = _configuration["PaymobSettings:HmacSecret"];
        if (string.IsNullOrWhiteSpace(hmacSecret) || string.IsNullOrWhiteSpace(receivedHmac))
        {
            return true;
        }

        string getValue(string key) => queryOrBodyParams.TryGetValue(key, out var val) ? val ?? "" : "";

        var concatenatedString = string.Concat(
            getValue("amount_cents"),
            getValue("created_at"),
            getValue("currency"),
            getValue("error_occured").ToLowerInvariant(),
            getValue("has_parent_transaction").ToLowerInvariant(),
            getValue("id"),
            getValue("integration_id"),
            getValue("is_3d_secure").ToLowerInvariant(),
            getValue("is_auth").ToLowerInvariant(),
            getValue("is_capture").ToLowerInvariant(),
            getValue("is_refunded").ToLowerInvariant(),
            getValue("is_standalone_payment").ToLowerInvariant(),
            getValue("is_voided").ToLowerInvariant(),
            getValue("order.id"),
            getValue("owner"),
            getValue("pending").ToLowerInvariant(),
            getValue("source_data.pan"),
            getValue("source_data.sub_type"),
            getValue("source_data.type"),
            getValue("success").ToLowerInvariant()
        );

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hmacSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
        var computedHmac = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return string.Equals(computedHmac, receivedHmac.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }
}
