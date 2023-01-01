using AMSGetFlights.Services;
using Newtonsoft.Json;
using Radzen;

var builder = WebApplication.CreateBuilder(args);
var provider = builder.Services.BuildServiceProvider();
var configuration = provider.GetService<IConfiguration>();
//string dataProvider = configuration.GetSection("GetFlights").GetValue<string>("DataProvider");
string webConfigFile = configuration.GetSection("GetFlights").GetValue<string>("ConfigFile");
GetFlightsConfig config = JsonConvert.DeserializeObject<GetFlightsConfig>(File.ReadAllText(webConfigFile));

// Add services to the container.
builder.Services.AddRazorPages();

// Service for monitoring AMS
builder.Services.AddHostedService<AMSGetFlightsBackgroundService>();

// Service to distribute updates 
builder.Services.AddSingleton<SubscriptionDispatcher>();
builder.Services.AddSingleton<SubscriptionManager>();
builder.Services.AddSingleton<IEventExchange, EventExchange>();
builder.Services.AddSingleton<IAMSGetFlightStatusService,AMSGetFlightsStatusService>();
builder.Services.AddSingleton<IGetFlightsConfigService,GetFlightsConfigService>();
builder.Services.AddSingleton<IFlightRepository, FlightRepository>();


if (config.Storage ==  "SQLite")
{
    builder.Services.AddSingleton<IFlightRepositoryDataAccessObject, SqLiteFlightRepository>();
}
if (config.Storage == "SQL")
{
    builder.Services.AddSingleton<IFlightRepositoryDataAccessObject, MSSQLFlightRepository>();
}
builder.Services.AddSingleton<IFlightRequestHandler,FlightRequestHandler>();

builder.Services.AddServerSideBlazor();
builder.Services.AddControllersWithViews().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});

if (config.EnableSubscriptions)
{
    builder.Services.AddHostedService<SubscriptionBackgroundService>();
}

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapControllers();   
app.MapFallbackToPage("/_Host");

app.Run();
