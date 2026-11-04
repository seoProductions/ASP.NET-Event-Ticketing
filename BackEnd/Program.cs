
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

//app.RegisterApiEndpoints();

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

app.Run();
