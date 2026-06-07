
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Add services to the container.

//common services: carter, mediatr, fluentvalidation, masstransit
var catalogAssembly = typeof(CatalogModule).Assembly;
var basketAssembly = typeof(BasketModule).Assembly;
var orderingAssembly = typeof(OrderingModule).Assembly;
var paymentAssembly = typeof(PaymentModule).Assembly;

builder.Services
    .AddCarterWithAssemblies(catalogAssembly, basketAssembly, orderingAssembly, paymentAssembly);

builder.Services
    .AddMediatRWithAssemblies(catalogAssembly, basketAssembly, orderingAssembly, paymentAssembly);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services
    .AddMassTransitWithAssemblies(builder.Configuration, catalogAssembly, basketAssembly, orderingAssembly, paymentAssembly);

builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

//module services: catalog, basket, ordering, payment
builder.Services
    .AddCatalogModule(builder.Configuration)
    .AddBasketModule(builder.Configuration)
    .AddOrderingModule(builder.Configuration)
    .AddPaymentModule(builder.Configuration);

builder.Services
    .AddExceptionHandler<CustomExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapCarter();
app.UseSerilogRequestLogging();
app.UseExceptionHandler(options => { });
app.UseAuthentication();
app.UseAuthorization();

app
    .UseCatalogModule()
    .UseBasketModule()
    .UseOrderingModule()
    .UsePaymentModule();

app.Run();