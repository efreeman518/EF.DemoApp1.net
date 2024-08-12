using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Package.Infrastructure.AspNetCore.Filters;

public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        //before endpoint
        var validateable = context.Arguments.SingleOrDefault(x => x is T);
        if (validateable is null)
        {
            return ProblemDetailsHelper.BuildProblemDetailsResponse(message: "Invalid model binding.", traceId: context.HttpContext.TraceIdentifier);
        }
        else
        {
            var validationResult = await validator.ValidateAsync((T)validateable);
            if (!validationResult.IsValid)
            {
                return ProblemDetailsHelper.BuildProblemDetailsResponse(message: validationResult.Errors.ToMessage(), traceId: context.HttpContext.TraceIdentifier);
            }
        }

        return await next(context);
    }
}
