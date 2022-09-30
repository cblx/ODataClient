namespace Cblx.Dynamics;

public interface IDynamicsAuthenticator
{
    Task<string> GetAccessToken(DynamicsConfig config);
}
