using Package.Infrastructure.Auth;

namespace Infrastructure.SampleApi;
public class SampleRestApiAuthMessageHandler(string[] scopes)
    : BaseDefaultAzureCredsAuthMessageHandler(scopes)
{
}
