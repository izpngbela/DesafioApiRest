using System.Text;
using DesafioApiRest.Api.Interfaces;
using DesafioApiRest.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuração de Autenticação JWT ---
// Obtém as configurações JWT - lança exceção se não configuradas
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("Chave JWT não configurada no appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
    ?? throw new InvalidOperationException("Issuer JWT não configurado no appsettings.json");
var jwtAudience = builder.Configuration["Jwt:Audience"] 
    ?? throw new InvalidOperationException("Audience JWT não configurado no appsettings.json");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        // ValidateIssuer: Garante que o token foi emitido por esta API específica
        // Justificativa: Previne tokens de outras fontes sejam aceitos
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        // ValidateAudience: Garante que o token foi destinado para este cliente/aplicação
        // Justificativa: Previne reutilização de tokens em sistemas não autorizados
        ValidateAudience = true,
        ValidAudience = jwtAudience
    };
});

// --- 2. Injeção de Dependência ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 3. Swagger com Suporte a JWT ---
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Cabeçalho Authorization usando o esquema Bearer. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// --- 4. Pipeline de Requisição ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();