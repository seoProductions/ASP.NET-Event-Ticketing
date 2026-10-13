var builder = WebApplication.CreateBuilder(args);

// SETUP CORS
// 1. Add CORS services with a wildcard policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()   // <-- Allows any URL
                .AllowAnyMethod()   // Allows GET, POST, PUT, DELETE, etc.
                .AllowAnyHeader();  // Allows any HTTP headers
        });
});
builder.Services.AddControllers();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var app = builder.Build();

// 2. Enable CORS middleware
app.UseRouting();
app.UseCors("AllowAll"); // <-- Apply the policy name here
app.UseAuthorization();
app.MapControllers();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// API STUFF:

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// This api url will return forecast
app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

// This record will be sent as a JSON data back to the front end
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
}