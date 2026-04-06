using Business;
var builder = WebApplication.CreateBuilder(args);
//paymentte bu kullanýlýr .
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
