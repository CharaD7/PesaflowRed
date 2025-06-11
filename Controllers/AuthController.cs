using Microsoft.AspNetCore.Mvc;
using PesaflowRed.Models;

public class AuthController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("IsAuthenticated") == "true")
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View(new LoginViewModel());
    }

    [HttpPost("login")]
    public IActionResult Login(LoginViewModel model)
    {
        var configClientId = _configuration["OAuth:PesaFlow:ClientId"];
        var configClientSecret = _configuration["OAuth:PesaFlow:ClientSecret"];

        if (string.IsNullOrEmpty(model.ClientId) || string.IsNullOrEmpty(model.ClientSecret))
        {
            ModelState.AddModelError("", "Please enter both Client ID and Client Secret");
            return View(model);
        }

        if (model.ClientId == configClientId && model.ClientSecret == configClientSecret)
        {
            _logger.LogInformation("Client credentials validated successfully");
            
            // Store the credentials temporarily
            HttpContext.Session.SetString("ClientId", model.ClientId);
            HttpContext.Session.SetString("ClientSecret", model.ClientSecret);
            
            // Redirect to PesaFlow's authorization endpoint
            var redirectUri = "http://localhost:5038/oauth/callback";
            var authUrl = $"https://sso.pesaflow.com/oauth/authorize" +
                         $"?client_id={Uri.EscapeDataString(model.ClientId)}" +
                         $"&response_type=code" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}";

            return Redirect(authUrl);
        }

        ModelState.AddModelError("", "Invalid client credentials");
        return View(model);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}