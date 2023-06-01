using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Grpc;
public partial class ServiceErrorInterceptor : Interceptor
{
    private readonly ILogger<ServiceErrorInterceptor> _logger;
    private readonly ErrorInterceptorSettings _settings;

    public ServiceErrorInterceptor(ILogger<ServiceErrorInterceptor> logger, IOptions<ErrorInterceptorSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        _logger.Log(LogLevel.Debug, "Executing {Path}", context.GetHttpContext().Request.Path);
        try
        {
            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            StatusCode statusCode = StatusCode.Internal;
            List<KeyValuePair<string, string>>? list = null;
            try
            {
                _logger.Log(LogLevel.Error, 0, "ServiceErrorInterceptor caught exception.", ex);
            }
            catch (Exception exception)
            {
                try
                {
                    _logger.Log(LogLevel.Error, 0, "ServiceErrorInterceptor internal exception when attempting to log an application exception.", exception);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine(ex2.Message);
                }
            }

            Metadata trailers = new();
            if (_settings.IncludeLogDataInResponse)
            {
                list?.ForEach(delegate (KeyValuePair<string, string> d)
                {
                    trailers.Add(CleanMetadataKey(d.Key), d.Value ?? "");
                });
            }

            throw new RpcException(new Status(statusCode, ex.Message), trailers, ex.Message);
        }
    }

    private static string CleanMetadataKey(string key)
    {
        return RegexClean().Replace(key, "");
    }

    [GeneratedRegex("[^a-zA-Z0-9-_.]")]
    private static partial Regex RegexClean();
}
