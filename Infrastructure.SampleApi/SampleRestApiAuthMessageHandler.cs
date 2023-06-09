using Package.Infrastructure.Auth;

namespace Infrastructure.SampleApi;
public class SampleRestApiAuthMessageHandler : BaseDefaultAzureCredsAuthMessageHandler
{
    public SampleRestApiAuthMessageHandler(string[] scopes) : base(scopes)
    {
    }
}
