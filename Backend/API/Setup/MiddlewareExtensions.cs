using API.Middleware;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace API.Setup;

public static class MiddlewareExtensions
{
    public static WebApplication UseAppMiddleware(this WebApplication app)
    {
        app.UseForwardedHeaders();

        app.UseExceptionMiddleware(includeDeveloperDetails: app.Environment.IsDevelopment());

        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseRouting();

        app.UseCors("CorsAllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication UseAppSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant()
                );
            }
        });

        return app;
    }

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        app.MapStaticAssets();

        app.MapControllerRoute(
                name: "area",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.MapAppHubs();

        return app;
    }
}
