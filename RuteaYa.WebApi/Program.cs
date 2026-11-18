using Microsoft.EntityFrameworkCore;
using RuteaYa.Logica;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RuteaYa.Logica.Rutas;

var builder = WebApplication.CreateBuilder(args);
var hereApiKey = builder.Configuration["Here:ApiKey"];

var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();

                // Obtenemos el token del header
                var authHeader = context.Request.Headers["Authorization"].ToString();
                var token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

                if (!string.IsNullOrEmpty(token) && await tokenService.EstaRevocadoAsync(token))
                {
                    context.Fail("Este token ha sido revocado.");
                }
            }
        };
    });
builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresá tu token JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

builder.Services.AddDbContext<RuteaYaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IViajeService, ViajeService>();
builder.Services.AddScoped<ITransporteService, TransporteService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEnvioService, EnvioService>();

if (string.IsNullOrWhiteSpace(hereApiKey))
{
    builder.Services.AddScoped<IRutaService, MockRutaService>();
}
else
{
    builder.Services.AddHttpClient<IRutaService, HereRutaService>();
}

builder.Services.AddScoped<IOportunidadService,OportunidadService>();
builder.Services.AddScoped<IGananciasMensualesService, GananciasMensualesService>();

builder.Services.AddScoped<OptimizadorRecorridos>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();
