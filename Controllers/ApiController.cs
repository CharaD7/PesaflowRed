using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PesaflowRed.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiController> _logger;

    public ApiController(IHttpClientFactory httpClientFactory, ILogger<ApiController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    [HttpGet("user-info")]
    public async Task<IActionResult> GetUserInfo([FromQuery] string access_token)
    {
        try
        {
            if (string.IsNullOrEmpty(access_token))
            {
                return BadRequest(new { error = "Access token is required" });
            }

            // Set up the request
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

            // Make request to PesaFlow's user info endpoint
            var response = await _httpClient.GetAsync("https://sso.pesaflow.com/api/user");
            var content = await response.Content.ReadAsStringAsync();

            // Log the response for debugging
            _logger.LogInformation("User info response: Status {StatusCode}, Content: {Content}", 
                response.StatusCode, content);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // Try to parse the response as UserInfoResponse
                    var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(content);
                    return Ok(userInfo);
                }
                catch (JsonException ex)
                {
                    // If parsing fails, return the raw response
                    _logger.LogWarning(ex, "Failed to parse user info response");
                    return Ok(JsonSerializer.Deserialize<object>(content));
                }
            }
            
            return StatusCode((int)response.StatusCode, new
            {
                error = "Failed to retrieve user info",
                details = content
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info");
            return StatusCode(500, new { error = "An error occurred while retrieving user info" });
        }
    }
}