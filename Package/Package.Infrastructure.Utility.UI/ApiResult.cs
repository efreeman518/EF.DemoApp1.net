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

public class ApiResult
{
    public bool IsSuccess { get; }
    public ProblemDetails? Problem { get; }

    private ApiResult(bool isSuccess, ProblemDetails? problem = null)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    public static ApiResult Success() => new(true);
    public static ApiResult Failure(ProblemDetails problem) => new(false, problem);
}