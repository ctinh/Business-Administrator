using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// register db context connect postgresql
builder.Services.AddDbContext<QtkdDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// register business services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IContractService, ContractService>();

// register controller & swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();