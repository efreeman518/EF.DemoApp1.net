using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Package.Infrastructure.Grpc;

public static class IServiceCollectionExtensions
{
    public static IHttpClientBuilder AddGrpcClient2<TClient>(this IServiceCollection services, Uri? baseAddress = null, int handlerLifeTimeSeconds = 600, int numRetries = 5, int retryWaitSeconds = 2, int circuitBreakerNum = 10, int circuitBreakerWaitSeconds = 30, HttpStatusCode[]? retryHttpStatusCodes = null, AuthenticationHeaderValue? authHeaderValue = null, HttpClientHandler? primaryHttpMessageHandler = null, int maxConnectionsPerServer = 200, ICollection<string>? certSecrets = null, Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? serverCertificateCustomValidationCallback = null) where TClient : class
    {
        Uri? baseAddress2 = baseAddress;
        AuthenticationHeaderValue? authHeaderValue2 = authHeaderValue;
        HttpClientHandler? primaryHttpMessageHandler2 = primaryHttpMessageHandler;
        Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? serverCertificateCustomValidationCallback2 = serverCertificateCustomValidationCallback;
        ICollection<string>? certSecrets2 = certSecrets;
        return services.AddGrpcClient<TClient>(delegate (GrpcClientFactoryOptions options)
        {
            if (baseAddress2 != null)
            {
                options.Address = baseAddress2;
            }
        }).ConfigureChannel(delegate (GrpcChannelOptions o)
        {
            CallCredentials callCredentials = CallCredentials.FromInterceptor(delegate (AuthInterceptorContext context, Metadata metadata)
            {
                if (authHeaderValue2 != null)
                {
                    metadata.Add("Authorization", authHeaderValue2.Scheme + " " + authHeaderValue2.Parameter);
                }

                return Task.CompletedTask;
            });
            o.Credentials = ChannelCredentials.Create(new SslCredentials(), callCredentials);
        }).ConfigurePrimaryHttpMessageHandler((Func<HttpMessageHandler>)delegate
        {
            HttpClientHandler? h = primaryHttpMessageHandler2;
            h ??= new HttpClientHandler();

            h.MaxConnectionsPerServer = maxConnectionsPerServer;
            if (serverCertificateCustomValidationCallback2 != null)
            {
                h.ServerCertificateCustomValidationCallback = serverCertificateCustomValidationCallback2;
            }

            byte[] privateKeyBytes;
            certSecrets2?.ToList().ForEach(delegate (string certSecret)
            {
                privateKeyBytes = Convert.FromBase64String(certSecret);
                X509Certificate2 value = new(privateKeyBytes, (string?)null);
                h.ClientCertificates.Add(value);
            });
            return h;
        })
            .SetHandlerLifetime(TimeSpan.FromSeconds(handlerLifeTimeSeconds))
            .AddPolicyHandler(GetRetryPolicy(numRetries, retryWaitSeconds, retryHttpStatusCodes))
            .AddPolicyHandler(GetCircuitBreakerPolicy(circuitBreakerNum, circuitBreakerWaitSeconds));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int numRetries = 5, int secDelay = 2, HttpStatusCode[]? retryHttpStatusCodes = null)
    {
        HttpStatusCode[]? retryHttpStatusCodes2 = retryHttpStatusCodes;
        Random jitterer = new();
        retryHttpStatusCodes2 ??= new HttpStatusCode[4]
            {
                    HttpStatusCode.RequestTimeout,
                    HttpStatusCode.BadGateway,
                    HttpStatusCode.ServiceUnavailable,
                    HttpStatusCode.GatewayTimeout
            };

        return Policy.Handle<HttpRequestException>().OrResult((HttpResponseMessage r) => retryHttpStatusCodes2.Contains(r.StatusCode)).WaitAndRetryAsync(numRetries, (int retryAttempt) => TimeSpan.FromSeconds(Math.Pow(secDelay, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 100)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int numConsecutiveFaults = 5, int secondsToWait = 30)
    {
        return HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(numConsecutiveFaults, TimeSpan.FromSeconds(secondsToWait));
    }
}