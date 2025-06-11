using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PesaflowRed.Models;

namespace PesaflowRed.Controllers;

public class OAuthController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthController> logger)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;

        // Validate configuration
        var clientId = _configuration["OAuth:PesaFlow:ClientId"];
        var clientSecret = _configuration["OAuth:PesaFlow:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogError("Missing OAuth configuration. Please check appsettings.json");
            throw new InvalidOperationException("Missing OAuth configuration");
        }
    }

    [HttpGet("oauth/callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogError("No authorization code provided in callback");
                return RedirectToAction("Error", "Home", new
                {
                    message = "No authorization code received"
                });
            }

            var clientId = HttpContext.Session.GetString("ClientId");
            var clientSecret = HttpContext.Session.GetString("ClientSecret");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return RedirectToAction("Login", "Auth");
            }

            var parameters = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", "http://localhost:5038/oauth/callback" }
            };

            // Log the request parameters
            _logger.LogInformation("Token request parameters: {@Parameters}",
                parameters.ToDictionary(p => p.Key, p => p.Key == "client_secret" ? "****" : p.Value));

            var content = new FormUrlEncodedContent(parameters);

            // Add explicit headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync("https://sso.pesaflow.com/oauth/access-token", content);
            var result = await response.Content.ReadAsStringAsync();

            // Log the response
            _logger.LogInformation("Token endpoint response status: {StatusCode}", response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token endpoint error response: {Response}", result);
            }

            if (response.IsSuccessStatusCode)
            {
                // Try to parse the response to ensure it's valid JSON
                try
                {
                    var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(result);
                    if (!tokenResponse.ContainsKey("access_token"))
                    {
                        throw new Exception("Response doesn't contain access_token");
                    }

                    // Store the auth code and access token in session
                    HttpContext.Session.SetString("AuthCode", code);
                    HttpContext.Session.SetString("AccessToken", result);
                    HttpContext.Session.SetString("IsAuthenticated", "true");

                    return RedirectToAction("Index", "Dashboard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse token response: {Response}", result);
                    return RedirectToAction("Error", "Home", new
                    {
                        message = "Invalid token response format"
                    });
                }
            }
            else
            {
                // Log response headers for debugging
                var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                _logger.LogError(
                    "Token endpoint error details: Status: {StatusCode}, Headers: {@Headers}, Body: {Body}",
                    response.StatusCode, headers, result);

                return RedirectToAction("Error", "Home", new
                {
                    message = $"Failed to retrieve access token: {response.StatusCode} - {result}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback");
            return RedirectToAction("Error", "Home", new
            {
                message = "An error occurred during authentication"
            });
        }
    }

    [HttpPost("oauth/access-token")]
    public async Task<IActionResult> GetAccessToken()
    {
        try
        {
            var clientId = _configuration["OAuth:PesaFlow:ClientId"];
            var clientSecret = _configuration["OAuth:PesaFlow:ClientSecret"];
            var authCode = HttpContext.Session.GetString("AuthCode");
            var redirectUri = "http://localhost:5038/oauth/callback";

        var parameters = new Dictionary<string, string>
        {
            {"client_id", clientId},
            {"client_secret", clientSecret},
            {"grant_type", "authorization_code"},
            {"code", authCode},
            {"redirect_uri", redirectUri}
        };

        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/x-www-form-urlencoded" }
        };

        // Store request details in TempData
        TempData["RequestDetails"] = JsonSerializer.Serialize(new RequestDetails
        {
            Url = "https://sso.pesaflow.com/oauth/access-token",
            Parameters = parameters,
            Headers = headers
        });

        using var httpClient = new HttpClient();
        var content = new FormUrlEncodedContent(parameters);
        foreach (var header in headers)
        {
            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        var response = await httpClient.PostAsync("https://sso.pesaflow.com/oauth/access-token", content);
        var result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(result);
            if (!string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                HttpContext.Session.SetString("AccessToken", tokenResponse.AccessToken);
                return RedirectToAction("Index", "Dashboard");
            }
        }
        
        TempData["TokenError"] = $"Failed to get access token. Status: {response.StatusCode}. Error: {result}";
        return RedirectToAction("Index", "Dashboard");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting access token");
        TempData["TokenError"] = "An error occurred while getting the access token";
        return RedirectToAction("Index", "Dashboard");
    }
}
}