using Pedidos.Api;
using Pedidos.Api.V1;
using Pedidos.Api.V2;
using Pedidos.Application;
using Pedidos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var cell = builder.Configuration.GetSection("Cell").Get<CellOptions>() ?? new CellOptions();
builder.Services.AddSingleton(cell);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPedidosInfrastructure(builder.Configuration.GetConnectionString("Pedidos")!);
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrderHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.Services.EnsurePedidosDatabase();
app.UseExceptionHandler();
app.UseStatusCodePages();

var contractsDir = Path.Combine(AppContext.BaseDirectory, "contracts");
if (Directory.Exists(contractsDir))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(contractsDir),
        RequestPath = "/contracts",
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/yaml"
    });
}

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/swagger", () => Results.Content(SwaggerPage.Html, "text/html; charset=utf-8"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapOrdersV1();
app.MapOrdersV2();

app.Run();

public partial class Program;
