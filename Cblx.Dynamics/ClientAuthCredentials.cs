using Microsoft.IdentityModel.Clients.ActiveDirectory;
namespace Cblx.Dynamics;
public class ClientAuthCredentials
{
    public ClientCredential? Credentials { get; set; }
    public AuthenticationResult? Authentication { get; set; }
}
