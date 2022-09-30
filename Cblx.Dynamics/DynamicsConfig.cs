namespace Cblx.Dynamics;

public class DynamicsConfig
{
    public string Authority { get; set; } = "";
    public string ResourceUrl { get; set; } = "";
    public string Users { get; set; } = "";

    public IEnumerable<DynamicsUserConfig> UserConfigs { 
        get {
            IEnumerable<string> usersStrings = Users.Split(",");
            return usersStrings.Select(str => {
                string[] stringParts = str.Split(':');
                return new DynamicsUserConfig
                {
                    ClientId = stringParts[0],
                    ClientSecret = stringParts[1]
                };
            });
        } 
    }
}

public class DynamicsUserConfig
{
    public string ClientId { get; init; } = "";

    public string ClientSecret { get; init; } = "";
}
