using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configure the application to listen on port 5009
builder.WebHost.UseUrls("http://0.0.0.0:5009");

// Add services to the container.
builder.Services.AddDbContext<ErrorLogDashboard.Web.Data.AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 32))));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure routing for lowercase URLs with dashes
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Add HttpClientFactory for DeepSeek service
builder.Services.AddHttpClient();

// Register services
builder.Services.AddSingleton<ErrorLogDashboard.Web.Services.DeepSeekService>();
builder.Services.AddScoped<ErrorLogDashboard.Web.Services.WebhookService>();

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();



ErrorLogDashboard.Web.Routes.Web.RegisterRoutes(app);

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ErrorLogDashboard.Web.Data.AppDbContext>();
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
    ErrorLogDashboard.Web.Data.DataSeeder.Seed(context);
}


app.Run();
