using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using DesafioApiRest.Api.Configuration;
using DesafioApiRest.Api.Interfaces;
using DesafioApiRest.Api.Middleware;
using DesafioApiRest.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação DesafioApiRest");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

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

    // --- 2. CORS ---
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:3000", "http://localhost:4200", "http://localhost:5173" };
    
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCorsPolicy", builder =>
        {
            builder.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // --- 3. Rate Limiting ---
    builder.Services.AddRateLimiter(options =>
    {
        // Política padrão para a API
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
        });

        // Política estrita para login (proteção contra brute force)
        options.AddPolicy("LoginPolicy", context =>
        {
            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 3,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                });
        });

        // Política para operações de arquivo
        options.AddPolicy("FilePolicy", context =>
        {
            return RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 20,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    TokensPerPeriod = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Muitas requisições",
                    message = $"Limite de taxa excedido. Tente novamente após {retryAfter.TotalSeconds:F0} segundos.",
                    retryAfter = retryAfter.TotalSeconds
                }, cancellationToken: cancellationToken);
            }
            else
            {
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Muitas requisições",
                    message = "Limite de taxa excedido. Tente novamente mais tarde."
                }, cancellationToken: cancellationToken);
            }
        };
    });

    // --- 4. Health Checks ---
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API está funcionando"))
        .AddCheck("file_system", () =>
        {
            try
            {
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), "ArquivosSeguros");
                
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }
                
                var testFile = Path.Combine(basePath, ".healthcheck");
                File.WriteAllText(testFile, DateTime.UtcNow.ToString());
                File.Delete(testFile);
                
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Sistema de arquivos acessível");
            }
            catch (Exception ex)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Falha ao acessar o sistema de arquivos", ex);
            }
        });

    // --- 5. FluentValidation ---
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    
    // --- 6. Configurações tipadas ---
    builder.Services.Configure<FileServiceOptions>(
        builder.Configuration.GetSection(FileServiceOptions.SectionName));
    builder.Services.Configure<UserCredentialsOptions>(
        builder.Configuration.GetSection(UserCredentialsOptions.SectionName));

    // --- 7. Injeção de Dependência ---
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IFileService, FileService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    // --- 8. Versionamento de API ---
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // --- 9. Swagger com Suporte a JWT e Versionamento ---
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "Desafio API Rest", 
            Version = "v1",
            Description = @"API RESTful para gerenciamento seguro de arquivos com autenticação JWT.
            
Suporta versionamento de API e políticas de Rate Limiting.",
            Contact = new OpenApiContact
            {
                Name = "Suporte Técnico",
                Email = "suporte@desafioapi.com",
                Url = new Uri("https://github.com/seu-usuario/desafio-api")
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = @"Autenticação JWT usando o esquema Bearer.
                          
Informe 'Bearer' [espaço] e em seguida o token.
                          
Exemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });
        
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        // Incluir XML comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }

    });

    var app = builder.Build();

    // --- 8. Pipeline de Requisição ---
    
    // Middleware de exceções global (deve ser um dos primeiros)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Serilog request logging
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Desafio API Rest v1");
            c.RoutePrefix = string.Empty; // Swagger na raiz
        });
    }
    else
    {
        // Em produção, forçar HTTPS
        app.UseHttpsRedirection();
    }

    // CORS deve vir antes de autenticação
    app.UseCors("DefaultCorsPolicy");

    // Rate Limiting
    app.UseRateLimiter();

    app.UseAuthentication(); 
    app.UseAuthorization();

    app.MapControllers();
    
    // Health Check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");

    Log.Information("Aplicação configurada com sucesso");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}