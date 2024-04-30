using Package.Infrastructure.Auth;

namespace Test.Endpoints;
public class SampleRestApiAuthMessageHandler(string[] scopes)
    : BaseDefaultAzureCredsAuthMessageHandler(scopes)
{
}
