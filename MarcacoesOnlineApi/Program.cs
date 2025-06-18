using System.Text;
using MarcacoesOnline.DAL;
using MarcacoesOnline.DAL.Repositories;
using MarcacoesOnline.Interfaces;
using MarcacoesOnline.Interfaces.Services;
using MarcacoesOnline.Model;
using MarcacoesOnline.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    x.JsonSerializerOptions.WriteIndented = true;
});

// Swagger (versão padrão funcional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de Marcação de Consultas Online",
        Version = "v1"
    });
});

// DbContext
builder.Services.AddDbContext<MarcacoesOnlineDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Registo de dependências
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPedidoMarcacaoRepository, PedidoMarcacaoRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPedidoMarcacaoService, PedidoMarcacaoService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<PdfService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        )
    };
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MarcacoesOnlineDbContext>();

    // Criar Administrativo (único)
    if (!context.Users.Any(u => u.Perfil == Perfil.Administrativo))
    {
        context.Users.Add(new User
        {
            NumeroUtente = "000000001",
            NomeCompleto = "Administrativo do Sistema",
            Email = "administrativo@marcacoes.com",
            Telemovel = "999000111",
            Morada = "Centro de Saúde Central",
            DataNascimento = new DateTime(1980, 1, 1),
            Genero = "Masculino",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
            Perfil = Perfil.Administrativo
        });
    }

    // Criar Administrador (único)
    if (!context.Users.Any(u => u.Perfil == Perfil.Administrador))
    {
        context.Users.Add(new User
        {
            NumeroUtente = "000000002",
            NomeCompleto = "Administrador do Sistema",
            Email = "admin@marcacoes.com",
            Telemovel = "999000222",
            Morada = "Centro Nacional",
            DataNascimento = new DateTime(1980, 1, 1),
            Genero = "Masculino",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
            Perfil = Perfil.Administrador
        });
    }

    context.SaveChanges();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
