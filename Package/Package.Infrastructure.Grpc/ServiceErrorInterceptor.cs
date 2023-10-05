using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Package.Infrastructure.Grpc;
public partial class ServiceErrorInterceptor(ILogger<ServiceErrorInterceptor> logger, IOptions<ErrorInterceptorSettings> settings) : Interceptor
{
    private readonly ILogger<ServiceErrorInterceptor> _logger = logger;
    private readonly ErrorInterceptorSettings _settings = settings.Value;

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
            try
            {
                _logger.Log(LogLevel.Error, 0, ex, "ServiceErrorInterceptor caught exception.");
            }
            catch (Exception exception)
            {
                try
                {
                    _logger.Log(LogLevel.Error, 0, exception, "ServiceErrorInterceptor internal exception when attempting to log an application exception.");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine(ex2.Message);
                }
            }

            Metadata trailers = [];
            if (_settings.IncludeLogDataInResponse)
            {
                List<KeyValuePair<string, string>> list =
                [
                    new KeyValuePair<string, string>("Exception", ex.Message)
                ];
                list.ForEach(delegate (KeyValuePair<string, string> d)
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
