using Microsoft.AspNetCore.Mvc;
using PesaflowRed.Models;

namespace PesaflowRed.Controllers;

public class DashboardController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IConfiguration configuration, 
        IHttpClientFactory httpClientFactory,
        ILogger<DashboardController> logger)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public IActionResult Index()
    {
        if (!HttpContext.Session.GetString("IsAuthenticated").Equals("true"))
        {
            return RedirectToAction("Login", "Auth");
        }

        var viewModel = new DashboardViewModel
        {
            AuthCode = HttpContext.Session.GetString("AuthCode"),
            Username = HttpContext.Session.GetString("Username"),
            ClientId = _configuration["OAuth:PesaFlow:ClientId"],
            ClientSecret = _configuration["OAuth:PesaFlow:ClientSecret"],
            RedirectUri = "http://localhost:5038/oauth/callback",
            AccessToken = HttpContext.Session.GetString("AccessToken")
        };

        return View(viewModel);
    }

    [HttpPost("get-access-token")]
    public async Task<IActionResult> GetAccessToken()
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                {"client_id", _configuration["OAuth:PesaFlow:ClientId"]},
                {"client_secret", _configuration["OAuth:PesaFlow:ClientSecret"]},
                {"grant_type", "authorization_code"},
                {"code", HttpContext.Session.GetString("AuthCode")},
                {"redirect_uri", "http://localhost:5038/oauth/callback"}
            };

            // Create request information
            var requestInfo = new
            {
                url = "https://sso.pesaflow.com/oauth/access-token",
                method = "POST",
                headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/x-www-form-urlencoded" }
                },
                parameters = parameters
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(requestInfo.url, content);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json(new { 
                    success = true, 
                    response = result,
                    statusCode = (int)response.StatusCode,
                    message = "Access token retrieved successfully",
                    request = requestInfo
                });
            }
            else
            {
                return Json(new { 
                    success = false, 
                    error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    response = result,
                    request = requestInfo,
                    details = new {
                        statusCode = (int)response.StatusCode,
                        reasonPhrase = response.ReasonPhrase,
                        headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
                    }
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while getting access token");
            return Json(new { 
                success = false, 
                error = "Network or HTTP error occurred",
                details = new {
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting access token");
            return Json(new { 
                success = false, 
                error = "An unexpected error occurred",
                details = new {
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                }
            });
        }
    }
}