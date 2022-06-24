using IoTEmergency.Web.Data;
using Microsoft.Azure.Devices;
using Microsoft.Azure.EventHubs;

var ServiceConnectionString = File.ReadAllText(".key");
var EventhubConnectionString = File.ReadAllText("eventhub.key");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IoTEmergencyRoomService>();
builder.Services.AddSingleton<DeviceStreamProxyService>();

builder.Services.AddSingleton(_ => ServiceClient.CreateFromConnectionString(ServiceConnectionString));
builder.Services.AddSingleton(_ => RegistryManager.CreateFromConnectionString(ServiceConnectionString));
builder.Services.AddSingleton(_ => EventHubClient.CreateFromConnectionString(EventhubConnectionString));
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
app.MapFallbackToPage("/_Host");

app.Run();
