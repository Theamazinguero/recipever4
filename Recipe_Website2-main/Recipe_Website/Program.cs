using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecipeApp.Api.Data;
using RecipeApp.Api.Models;
using RecipeApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------------ CONFIG ------------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super-secret-key-change-me";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RecipeApp";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

// ------------------ DB CONTEXT (SQLite) ------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ------------------ IDENTITY (COOKIE AUTH + ROLES + MVC) ------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Identity Cookie Paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/Login";
});

// ------------------ JWT AUTH (FOR API) ------------------
// ------------------ AUTHENTICATION (COOKIE DEFAULT + JWT FOR API) ------------------
builder.Services.AddAuthentication(options =>
{
    // Make cookie auth the default scheme for MVC
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
// DO NOT add AddCookie() manually — Identity sets up cookies already
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});



builder.Services.AddAuthorization();

// ------------------ SERVICES ------------------
builder.Services.AddScoped<JwtTokenService>();

// ------------------ MVC + API + Swagger ------------------
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ------------------ SEED ROLES + DEFAULT ADMIN ------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.SeedRolesAndAdminAsync(services, jwtIssuer);
}

// ------------------ MIDDLEWARE ------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ------------------ MVC ROUTES ------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ------------------ API CONTROLLERS ------------------
app.MapControllers();

app.Run();
