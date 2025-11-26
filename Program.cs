using ApolloMigration.Services;
using ApolloMigration.Repositories;
using ApolloMigration.Controllers;
using FluentValidation;
using FluentValidation.AspNetCore;
using ApolloMigration.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Couchbase configuration
builder.Services.Configure<CouchbaseConfig>(builder.Configuration.GetSection("Couchbase"));

// Add repositories
builder.Services.AddScoped<IDataRepository, CouchbaseDataRepository>();

// Add services
builder.Services.AddScoped<IDataMigrationService, DataMigrationService>();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
