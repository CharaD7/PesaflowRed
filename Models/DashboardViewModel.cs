namespace PesaflowRed.Models;

public class DashboardViewModel
{
    public string AuthCode { get; set; }
    public string Username { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RedirectUri { get; set; }
    public string AccessToken { get; set; }
}