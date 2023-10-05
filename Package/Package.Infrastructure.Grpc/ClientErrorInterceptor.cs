using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;
using System.Runtime.CompilerServices;

namespace Package.Infrastructure.Grpc;
public class ClientErrorInterceptor(ILogger<ClientErrorInterceptor> logger, IOptions<ErrorInterceptorSettings> settings) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        _ = settings.GetHashCode();
        logger.Log(LogLevel.Debug, "Executing {Method}", context.Method);
        AsyncUnaryCall<TResponse> asyncUnaryCall = continuation(request, context);
        return new AsyncUnaryCall<TResponse>(HandleResponse(asyncUnaryCall.ResponseAsync), asyncUnaryCall.ResponseHeadersAsync, new Func<Status>(asyncUnaryCall.GetStatus), new Func<Metadata>(asyncUnaryCall.GetTrailers), new Action(asyncUnaryCall.Dispose));
    }

    private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> grpcResponse)
    {
        try
        {
            TResponse val = await grpcResponse;
            logger.Log(LogLevel.Debug, "Response received: {Response}", val);
            return val;
        }
        catch (Exception ex)
        {
            AttemptLogException(ex);
            throw;
        }
    }

    private void AttemptLogException(Exception ex)
    {
        List<KeyValuePair<string, string?>> logData = [];
        try
        {
            string text = "ClientErrorInterceptor caught exception: " + ex.Message + ".";
            if (ex is RpcException exRpc)
            {
                string text2 = text;
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new(2, 2);
                defaultInterpolatedStringHandler.AppendLiteral(" ");
                defaultInterpolatedStringHandler.AppendFormatted(exRpc.StatusCode);
                defaultInterpolatedStringHandler.AppendLiteral(" ");
                defaultInterpolatedStringHandler.AppendFormatted(exRpc.Status.Detail);
                text = text2 + defaultInterpolatedStringHandler.ToStringAndClear();
                exRpc.Trailers.ToList().ForEach(delegate (Metadata.Entry t)
                {
                    logData.Add(new KeyValuePair<string, string?>(t.Key, t.Value));
                });
            }

            logger.Log(LogLevel.Error, 0, text, ex, logData);
        }
        catch (Exception exception)
        {
            try
            {
                logger.Log(LogLevel.Error, 0, "ClientErrorInterceptor internal exception when attempting to log an application exception.", exception, logData);
            }
            catch
            {
                //not much we can do if logger fails
            }
        }
    }
}