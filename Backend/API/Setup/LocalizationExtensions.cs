using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Base;
using Base.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Setup;

public static class LocalizationExtensions
{
    public static IServiceCollection AddAppLocalization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLocalization();

        services.AddControllersWithViews()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.SubFolder)
            .AddDataAnnotationsLocalization(options =>
            {
                // Route all DataAnnotations localization through the shared Common resource
                // so [Display], [Required], [Range], etc. on ViewModels use the same resx files
                // as the views (instead of looking up per-ViewModel .resx files).
                options.DataAnnotationLocalizerProvider = (type, factory) =>
                    factory.Create(typeof(Common));
            });

        var supportedCultures = configuration
            .GetSection("SupportedCultures")
            .GetChildren()
            .Select(x => new CultureInfo(x.Value!))
            .ToArray();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            // datetime and currency support
            options.SupportedCultures = supportedCultures;
            // UI translated strings
            options.SupportedUICultures = supportedCultures;
            // if nothing is found, use this
            options.DefaultRequestCulture = new RequestCulture("et-EE", "et-EE");
            options.SetDefaultCulture("et-EE");

            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                // Order is important, it's in which order they will be evaluated
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        LangStr.DefaultCulture = configuration.GetValue<string>("LangStrDefaultCulture") ?? "en";

        return services;
    }
}
