namespace Cblx.Dynamics;

public record DynamicsConfig
{
    public string Authority { get; set; } = "";
    public string ResourceUrl { get; set; } = "";
    public string Users { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    public IEnumerable<DynamicsUserConfig> UserConfigs { 
        get {
            var users = new List<DynamicsUserConfig>();
            if(!string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret))
            {
                users.Add(new DynamicsUserConfig { ClientId = ClientId, ClientSecret = ClientSecret });
            }
            var usersStrings = Users.Split(",", StringSplitOptions.RemoveEmptyEntries);
            users.AddRange(usersStrings.Select(str => {
                string[] stringParts = str.Split(':');
                return new DynamicsUserConfig
                {
                    ClientId = stringParts[0],
                    ClientSecret = stringParts[1]
                };
            }));
            return users;
        } 
    }
}

public class DynamicsUserConfig
{
    public string ClientId { get; init; } = "";

    public string ClientSecret { get; init; } = "";
}
