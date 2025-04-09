using Refit;

namespace Package.Infrastructure.Utility.UI;

public class ApiResult<T>
{
    public T? Data { get; set; }
    public ProblemDetails? Problem { get; set; }
    public bool IsSuccess => Problem == null;

    public static ApiResult<T> Success(T data) => new() { Data = data };
    public static ApiResult<T> Failure(ProblemDetails problem) => new() { Problem = problem };
}