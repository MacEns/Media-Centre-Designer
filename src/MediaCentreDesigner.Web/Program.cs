using MediaCentreDesigner.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/catalog", () => DeviceCatalog.Items);

app.MapPost("/api/design", (DesignRequest request) =>
{
    try
    {
        return Results.Ok(Designer.Generate(request));
    }
    catch (DesignException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public partial class Program;
