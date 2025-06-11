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
public async Task<IActionResult> GetUserInfo([FromQuery] string access_token, [FromQuery] string userId)
{
    try
    {
        if (string.IsNullOrEmpty(access_token))
        {
            return BadRequest(new { error = "Access token is required" });
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

        var baseUrl = "https://sso.pesaflow.com/api/user";
        var url = !string.IsNullOrEmpty(userId) ? $"{baseUrl}/{userId}" : baseUrl;

        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // Return the raw response with proper content type
        return Content(content, "application/json");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving user info");
        return StatusCode(500, JsonSerializer.Serialize(new { 
            error = "An error occurred while retrieving user info",
            details = ex.Message
        }));
    }
}
}