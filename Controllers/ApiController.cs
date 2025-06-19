using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace PesaflowRed.Controllers;

public class ApiController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiController> _logger;

    public ApiController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<ApiController> logger)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    [HttpPost("get-access-token")]
    public async Task<IActionResult> GetAccessToken()
    {
        try
        {
            var clientId = _configuration["OAuth:PesaFlow:ClientId"];
            var clientSecret = _configuration["OAuth:PesaFlow:ClientSecret"];
            var authCode = HttpContext.Session.GetString("AuthCode");
            var tokenUrl = "https://sso.pesaflow.com/oauth/access-token";
            var redirectUri = "http://localhost:5038/oauth/callback";

            var parameters = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "redirect_uri", redirectUri }
            };

            using var content = new FormUrlEncodedContent(parameters);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync(tokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                if (!string.IsNullOrEmpty(tokenResponse?.AccessToken))
                {
                    return Ok(new
                    {
                        success = true,
                        access_token = tokenResponse.AccessToken,
                        expires_in = tokenResponse.ExpiresIn,
                        token_type = tokenResponse.TokenType,
                        message = "Access token retrieved successfully"
                    });
                }
            }

            return BadRequest(new { error = "Failed to retrieve access token", details = responseContent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token");
            return StatusCode(500, new { error = "An error occurred while retrieving the access token" });
        }
    }
    
    [HttpGet("api/user-info")]
    public async Task<IActionResult> GetUserInfo([FromQuery(Name = "access_token")] string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { error = "Access token is required" });
        }

        try
        {
            var baseUrl = "https://sso.pesaflow.com/api/user-info";
            // Parse JSON and extract access_token
            JObject jsonObj = JObject.Parse(Uri.EscapeDataString(accessToken));
            string accessTk = jsonObj["access_token"].ToString();
            var url = $"{baseUrl}?access_token={accessTk}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { data = JsonDocument.Parse(content).RootElement });
            }

            return StatusCode((int)response.StatusCode, new { error = content });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info");
            return StatusCode((int)Response.StatusCode, new { error = "Failed to retrieve user info" });
            // return StatusCode(500, new { error = "Failed to retrieve user info" });
        }
    }
    
    [HttpGet("api/find-user")]
    public async Task<IActionResult> FindUser(
        [FromQuery(Name = "access_token")] string accessToken,
        [FromQuery(Name = "idNumber")] string idNumber,
        [FromQuery(Name = "idType")] string idType
        )
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { error = "Access token is required" });
        }

        try
        {
            var baseUrl = "https://sso.pesaflow.com/api/user-info";
            // Parse JSON and extract access_token
            JObject jsonObj = JObject.Parse(Uri.EscapeDataString(accessToken));
            string accessTk = jsonObj["access_token"].ToString();
            var url = $"{baseUrl}?access_token={accessTk}&id_number={idNumber}&id_type={idType}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { data = JsonDocument.Parse(content).RootElement });
            }

            return StatusCode((int)response.StatusCode, new { error = content });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info");
            return StatusCode((int)Response.StatusCode, new { error = "Failed to retrieve user info" });
            // return StatusCode(500, new { error = "Failed to retrieve user info" });
        }
    }
}