using AMSGetFlights.Services;
using Newtonsoft.Json;
using Radzen;

var builder = WebApplication.CreateBuilder(args);
var provider = builder.Services.BuildServiceProvider();
var configuration = provider.GetService<IConfiguration>();

// The configuration for the service is defined in the file defined in appsettings.json file
// We need to read it here to know which of the DataAccess objects to load 

string webConfigFile = configuration.GetSection("GetFlights").GetValue<string>("ConfigFile");
GetFlightsConfig config = JsonConvert.DeserializeObject<GetFlightsConfig>(File.ReadAllText(webConfigFile));



// Background process for monitoring updates from AMS
builder.Services.AddHostedService<AMSGetFlightsBackgroundService>();

// Realtime handling for dispatching subscriptions
builder.Services.AddSingleton<SubscriptionDispatcher>();

// Manages subscription requests and Updates
builder.Services.AddSingleton<SubscriptionManager>();

// Common Event broadcaster
builder.Services.AddSingleton<EventExchange>();

// Manages the interaction betwwen the system and AMS
builder.Services.AddSingleton<AMSGetFlightsStatusService>();

// Reads the configuration file and manageas configuration updates
builder.Services.AddSingleton<GetFlightsConfigService>();

// Service layer over the data access object
builder.Services.AddSingleton<FlightRepository>();

// Manages tihe flight request coming from the controller
builder.Services.AddSingleton<FlightRequestHandler>();

// Handles the flight to remove data the user is not configured to see
builder.Services.AddSingleton<FlightSanitizer>();


// Load the correct DataAccessObject as defined in the defined fonfig file
if (config.Storage ==  "SQLite")
{
    builder.Services.AddSingleton<IFlightRepositoryDataAccessObject, SqLiteFlightRepository>();
}
if (config.Storage == "SQL")
{
    builder.Services.AddSingleton<IFlightRepositoryDataAccessObject, MSSQLFlightRepository>();
}

// Configure Newtonsoft to handle the serialization and tell it to ignores keys with NULL values
builder.Services.AddControllersWithViews().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});

// Start the background process for managing subscriptions, if enabled in the configuration
if (config.EnableSubscriptions)
{
    builder.Services.AddHostedService<SubscriptionBackgroundService>();
}

// Service specific to the Blazor UI components
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
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
