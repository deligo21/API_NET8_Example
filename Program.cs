using PaymentAPI.Services;

var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payment API",
        Version = "v1",
        Description = "API para registrar y consultar pagos de servicios básicos"
    });
});

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

await InitializeDatabaseAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();

async Task InitializeDatabaseAsync(WebApplication application)
{
    using var scope = application.Services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
    await repository.InitializeDatabaseAsync();
}
