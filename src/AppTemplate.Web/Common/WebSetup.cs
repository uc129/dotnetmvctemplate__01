using AppTemplate.Web.Common.Dates;
using AppTemplate.Web.Common.Errors;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace AppTemplate.Web.Common;

public static class WebSetup
{
    public static IServiceCollection AddWeb(this IServiceCollection services)
    {
        services
            .AddControllersWithViews(options =>
            {
                // First in the list, so it wins over MVC's culture-dependent date binding.
                options.ModelBinderProviders.Insert(0, new DateModelBinderProvider());
                // Every POST, PUT and DELETE needs an antiforgery token. Forms get it from the
                // form tag helper; AJAX gets it from the header site.js adds.
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            })
            .AddJsonOptions(options => DateJsonConverters.AddTo(options.JsonSerializerOptions.Converters));

        services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            // Same reference as the error page and the logs.
            context.ProblemDetails.Extensions["traceId"] = RequestReference.Get(context.HttpContext);
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static WebApplication UseWeb(this WebApplication app)
    {
        // First, so it sees the final status of every request, including errors handled below,
        // and writes one line per request (method, path, status, ms, trace id). It sits outside
        // the exception handler, so it never logs the exception itself a second time.
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandlingPath = "/Error",
                // GlobalExceptionHandler logs every exception once; stop the middleware logging it again.
                SuppressDiagnosticsCallback = _ => true,
            });
            app.UseHsts();
        }

        // 404s and other empty error responses get a friendly page (AJAX calls are left alone).
        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();
        return app;
    }
}
