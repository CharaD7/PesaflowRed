namespace PesaflowRed.Models;

public class DashboardViewModel
{
    public string AuthCode { get; set; }
    public string Username { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RedirectUri { get; set; }
    public string AccessToken { get; set; }
    public RequestDetails LastRequest { get; set; }
}

public class RequestDetails
{
    public string Url { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}