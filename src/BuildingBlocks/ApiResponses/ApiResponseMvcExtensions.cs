using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiResponses;

public static class ApiResponseMvcExtensions
{
    public static IServiceCollection AddBaseResponseValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Values
                    .SelectMany(value => value.Errors)
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid request value." : error.ErrorMessage)
                    .ToArray();

                return new BadRequestObjectResult(ApiResponse<object>.Fail(errors, "Validation failed."));
            };
        });

        return services;
    }
}
