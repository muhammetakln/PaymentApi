using API.Models;
using Business;
using Core.Abstract.IManagements;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Payment API Çalışıyor! ✅");

// 1. Ödeme İşlemi Endpoint'i
app.MapPost("/api/payments/process", async (ProcessPaymentRequest request, IPaymentManager paymentManager) =>
{
    var result = await paymentManager.ProcessPaymentAsync(request.OrderNumber, request.TotalAmount, request.Currency);

    return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
});

// 2. İade İşlemi Endpoint'i
app.MapPost("/api/payments/refund", async (RefundPaymentRequest request, IPaymentManager paymentManager) =>
{
    var result = await paymentManager.RefundPaymentAsync(request.OrderNumber, request.Amount, request.Currency);

    return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
});

app.Run();