using Microsoft.EntityFrameworkCore;
using RuteaYa.Logica;

var builder = WebApplication.CreateBuilder(args);

// Registrar controladores y la API
builder.Services.AddControllers();

// Swagger / OpenAPI 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar DbContext y Servicios de Lógica[cite: 1]
builder.Services.AddDbContext<RuteaYaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Interfaz gráfica para Swagger en desarrollo[cite: 1, 16]
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
