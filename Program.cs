using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using restaurant.DataAccess;
using restaurant.Interfaces;
using restaurant.Services;

var builder = WebApplication.CreateBuilder(args);

Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
    builder.Configuration["Authentication:Google:Credentials"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;


    options.Scope.Add("profile");
    options.Events.OnCreatingTicket = (context) =>
    {
        var email = context.User.GetProperty("email").GetString();
        var picture = context.User.GetProperty("picture").GetString();
        context.Identity!.AddClaim(new Claim("email", email!));
        context.Identity!.AddClaim(new Claim("picture", picture!));
        return Task.CompletedTask;
    };
});

builder.Services.AddSingleton<CacheService>();
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<MenuRepository>();
builder.Services.AddScoped<IBucketStorageService, BucketStorageService>();
builder.Services.AddSingleton<PubSubService>();
//builder.Services.AddSingleton<TranslationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
