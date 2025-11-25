using Refit;

namespace Package.Infrastructure.Utility.UI;

public static class RefitExtensions
{
    public static string FormatProblemDetails(this ProblemDetails problem) =>
        problem switch
        {
            null => "Unknown error.",
            { Detail: not null and not "" } => problem.Detail,
            { Errors.Count: > 0 } => string.Join("; ", problem.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))),
            { Title: not null } => problem.Title,
            _ => "An error occurred."
        };
}

//public extension class ProblemDetailsExtensions
//{
//    public static string FormatProblemDetails(this ProblemDetails problem) =>
//        problem switch
//        {
//            null => "Unknown error.",
//            { Detail: not null and not "" } => problem.Detail,
//            { Errors.Count: > 0 } => string.Join("; ", problem.Errors
//                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))),
//            { Title: not null } => problem.Title,
//            _ => "An error occurred."
//        };
//}
