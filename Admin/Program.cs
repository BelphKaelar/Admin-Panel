using Admin.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Firebase Authentication Setup
var pathToCredentials = "FirebaseKey/firekey.json";
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToCredentials);

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(pathToCredentials),
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add the custom FirebaseService
builder.Services.AddSingleton<FirebaseService>();

// Add support for Session
builder.Services.AddDistributedMemoryCache(); 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); //Session Duration
    options.Cookie.HttpOnly = true; 
    options.Cookie.IsEssential = true; 
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Global Middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();
    if (!path.Contains("/account/login") && context.Session.GetString("AdminEmail") == null)
    {
        context.Response.Redirect("/Account/Login");
        return;
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
