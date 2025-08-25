using copiloty_stats_viewer.Components;
using copiloty_stats_viewer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddSingleton<DataService>();
builder.Services.AddScoped<PdfReportService>();

// Enable detailed errors for Blazor circuits
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<copiloty_stats_viewer.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
