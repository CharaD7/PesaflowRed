using Microsoft.AspNetCore.Mvc;
using PesaflowRed.Models;
using System.Text.Json;

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

        // Get request details from TempData if available
        if (TempData["RequestDetails"] is string requestDetailsJson)
        {
            viewModel.LastRequest = JsonSerializer.Deserialize<RequestDetails>(requestDetailsJson);
        }

        return View(viewModel);
    }
}