namespace Package.Infrastructure.Grpc;

public class ErrorInterceptorSettings
{
    public bool IncludeLogDataInResponse { get; set; } = true;
    public bool LogIncomingRequest { get; set; }
    public bool LogIncomingRequestBody { get; set; }
    public List<string>? LogRequestBodyOnLevels { get; set; }
}
