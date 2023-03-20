using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Utilities;
using Microsoft.OpenApi.Models;
using DomainLayer.Context;
using RepositoryLayer;
using ServiceLayer;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var connectionString = builder.Configuration.GetConnectionString("UserServicesContext");
builder.Services.AddDbContext<ApplicationDB>(options =>
	options.UseSqlServer(connectionString ?? throw new InvalidOperationException("Connection string not found.")));

// Add services to the container.

#region AppSetting
//AppSettings.ConnectionString = connectionString;
builder.Services.AddSingleton(new AppSettings()
{
	ConnectionString = connectionString
});
builder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
#endregion


#region Services Injected
builder.Services.AddScoped(typeof(IUserRepository<>), typeof(UserRepository<>));
builder.Services.AddTransient(typeof(IUserService<>), typeof(UserService<>));
#endregion

builder.Services.AddControllersWithViews().AddNewtonsoftJson(options =>
			options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
	options.RequireHttpsMetadata = false;
	options.SaveToken = true;
	options.TokenValidationParameters = new TokenValidationParameters()
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidAudience = builder.Configuration["Jwt:Audience"],
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
	};
});

builder.Services.AddSwaggerGen(options =>
{
	options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Version = "v1",
		Title = "User Service API",
		Description = "Web api for medvantage user service"
	});
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Please insert bearer token into field",
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey
	});
	options.AddSecurityRequirement(new OpenApiSecurityRequirement {
	{
		new OpenApiSecurityScheme
		{
		Reference = new OpenApiReference
		{
			Type = ReferenceType.SecurityScheme,
			Id = "Bearer"
		}
		},
		Array.Empty<string>()
	}
  });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(x => x
			   .AllowAnyOrigin()
			   .AllowAnyMethod()
			   .AllowAnyHeader());

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
