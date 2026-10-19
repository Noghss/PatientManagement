using Microsoft.EntityFrameworkCore;
using PatientManagement.Api.Data;
using PatientManagement.Api.Data.Daos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var patientManagementDatabaseConnectionString =
    builder.Configuration.GetConnectionString("PatientManagementDatabase");

if (string.IsNullOrWhiteSpace(patientManagementDatabaseConnectionString))
{
    throw new InvalidOperationException(
        "The connection string 'PatientManagementDatabase' is required.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(patientManagementDatabaseConnectionString));

builder.Services.AddScoped<IPatientDao, PatientDao>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
