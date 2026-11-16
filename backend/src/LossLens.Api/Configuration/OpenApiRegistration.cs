using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace LossLens.Api.Configuration;

public static class OpenApiRegistration
{
    public const string BearerScheme = "Bearer";

    /// <summary>
    /// Registers the OpenAPI document with a title, the Bearer (JWT) security scheme, and a lock on every
    /// operation that needs a token, so the Authorize button of the UI works. Sign in with
    /// <c>POST /api/v1/auth/login</c> and paste the <c>accessToken</c> into the Authorize dialog.
    /// </summary>
    public static IServiceCollection AddOpenApiWithBearer(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "LossLens API",
                    Version = "v1",
                    Description = "Daily production metrics of the plant. Sign in with POST /api/v1/auth/login, " +
                                  "then press Authorize and paste the accessToken."
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "The accessToken of the sign-in answer, without the word Bearer."
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var anonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
                if (!anonymous)
                {
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerScheme }
                        }] = []
                    });
                }

                return Task.CompletedTask;
            });
        });
}
