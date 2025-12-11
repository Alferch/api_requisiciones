using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RequisicionesApi.Interfaces;
using RequisicionesApi.Repositorios;
using RequisicionesApi.Services;
using RequisicionesApi.Utilidades;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Requisiciones API", Version = "v1" });


    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT con el prefijo 'Bearer ', por ejemplo: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new List<string>()
        }
    });
});


builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<IRolRepository, RolRepository>();

builder.Services.AddScoped<IImputacionValService, ImputacionValService>();
builder.Services.AddScoped<IImputacionValRepository, ImputacionValRepository>();
builder.Services.AddScoped<ISubAreaRepository, SubAreaRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<ClasProvRepository>();
builder.Services.AddScoped<IdiomaRepository>();
builder.Services.AddScoped<IUsuarioEdicionRepository, UsuarioEdicionRepository>();

builder.Services.AddScoped<IRequisicionService, RequisicionService>();

builder.Services.AddSingleton<CriterioRepository>();
builder.Services.AddScoped<ICriterioService, CriterioService>();

builder.Services.AddScoped<IRequisicionService, RequisicionService>();
builder.Services.AddScoped<IMailService, MailService>();

builder.Services.AddScoped<IAutorizacionService, AutorizacionService>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// DI: Repositorio
builder.Services.AddScoped<RequisicionesApi.Repositorios.FlujoRepository>();


builder.Services.AddScoped<ProvCRequisicionesService>(provider =>
    new ProvCRequisicionesService(builder.Configuration.GetConnectionString("DefaultConnection")));


// DI: interfaces -> implementaciones
builder.Services.AddScoped<ITimelineRepository, SqlTimelineRepository>();
builder.Services.AddScoped<ITimelineService, TimelineService>();


// Antes de registrar el servicio CondAdicEncabezadoService, define la variable connectionString
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ✅ Registrar el servicio con inyección de dependencia usando la variable connectionString
builder.Services.AddScoped<ICondAdicEncabezadoService>(sp =>
    new CondAdicEncabezadoService(builder.Configuration));



// DI
builder.Services.AddScoped<IDashboardService, DashboardService>();


builder.Services.AddScoped<ICargaService, CargaService>();
builder.Services.Configure<ExcelOptions>(builder.Configuration.GetSection("Excel"));




builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new EmptyToNullDateTimeConverter());
    });

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p =>
    {
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else {
    app.UseSwagger();
    app.UseSwaggerUI();
}
    app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();


public class ExcelOptions
{
    public int MaxSizeMB { get; set; } = 10;
}


//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
