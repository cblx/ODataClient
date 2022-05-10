using Microsoft.IdentityModel.Clients.ActiveDirectory;
namespace Cblx.Dynamics.AspNetCore;
public class ClientAuthCredentials
{
    public ClientCredential? Credentials { get; set; }
    public AuthenticationResult? Authentication { get; set; }
}
