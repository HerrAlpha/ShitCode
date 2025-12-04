using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ErrorLogDashboard.Web.Routes;

public static class Web
{
    public static void RegisterRoutes(IEndpointRouteBuilder endpoints)
    {
        // Define your routes here, similar to Laravel's web.php
        
        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
            
        // Example:
        // endpoints.MapGet("/about", async context => await context.Response.WriteAsync("About Page"));
    }
}
