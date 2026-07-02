using CourtPulse.Application;
using CourtPulse.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow the Angular dev server to call the API.
const string AngularCors = "angular";
builder.Services.AddCors(options => options.AddPolicy(AngularCors, policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddCourtPulseApplication();
builder.Services.AddCourtPulseInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(AngularCors);
app.MapControllers();

app.Run();
