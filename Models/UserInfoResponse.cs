public class UserInfoResponse
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}