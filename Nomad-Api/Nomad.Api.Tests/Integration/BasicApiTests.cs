using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nomad.Api.Data;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Nomad.Api.Tests.Integration;

public class BasicApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Guid _tenantId = Guid.NewGuid();

    public BasicApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<NomadSurveysDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<NomadSurveysDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                });
            });
        });

        _client = _factory.CreateClient();
        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NomadSurveysDbContext>();
        
        context.Database.EnsureCreated();
        
        if (!context.Tenants.Any(t => t.Id == _tenantId))
        {
            var tenant = new Tenant
            {
                Id = _tenantId,
                Name = "Test Tenant",
                Slug = "test-tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            context.Tenants.Add(tenant);
            context.SaveChanges();
        }
    }

    [Fact]
    public async Task GetSubjects_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/subjects");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_WithValidData_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateSubjectRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            CompanyName = "Test Company"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subjects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_WithInvalidData_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateSubjectRequest
        {
            // Missing required fields
            CompanyName = "Test Company"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subjects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BulkCreateSubjects_WithEmptyList_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new BulkCreateSubjectsRequest
        {
            Subjects = new List<CreateSubjectRequest>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subjects/bulk", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvaluator_WithValidData_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateEvaluatorRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            EvaluatorEmail = "jane.smith@test.com",
            CompanyName = "Test Company"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/evaluators", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BulkCreateEvaluators_WithValidData_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new BulkCreateEvaluatorsRequest
        {
            Evaluators = new List<CreateEvaluatorRequest>
            {
                new CreateEvaluatorRequest
                {
                    FirstName = "Manager",
                    LastName = "One",
                    EvaluatorEmail = "manager1@test.com",
                    CompanyName = "Test Company"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/evaluators/bulk", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEvaluators_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/evaluators");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSubjectEvaluators_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/subject-evaluators");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_WithExtremelyLongName_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateSubjectRequest
        {
            FirstName = new string('A', 101), // Exceeds max length
            LastName = "Doe",
            Email = "long.name@test.com",
            CompanyName = "Test Company"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subjects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubject_WithInvalidEmail_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateSubjectRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email", // Invalid email format
            CompanyName = "Test Company"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subjects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSubject_WithInvalidId_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/subjects/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSubject_WithValidData_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new UpdateSubjectRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@test.com",
            CompanyName = "Updated Company"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/subjects/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSubject_WithValidId_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/subjects/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvaluator_WithInvalidEmail_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateEvaluatorRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            EvaluatorEmail = "invalid-email", // Invalid email format
            CompanyName = "Test Company"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/evaluators", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BulkCreateSubjects_WithMixedValidInvalidData_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new BulkCreateSubjectsRequest
        {
            Subjects = new List<CreateSubjectRequest>
            {
                new CreateSubjectRequest
                {
                    FirstName = "Valid",
                    LastName = "User",
                    Email = "valid@test.com",
                    CompanyName = "Test Company"
                },
                new CreateSubjectRequest
                {
                    FirstName = "", // Invalid - empty name
                    LastName = "Invalid",
                    Email = "invalid@test.com",
                    CompanyName = "Test Company"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/subjects/bulk", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BulkCreateEvaluators_WithDuplicateEmails_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new BulkCreateEvaluatorsRequest
        {
            Evaluators = new List<CreateEvaluatorRequest>
            {
                new CreateEvaluatorRequest
                {
                    FirstName = "First",
                    LastName = "Evaluator",
                    EvaluatorEmail = "duplicate@test.com",
                    CompanyName = "Test Company"
                },
                new CreateEvaluatorRequest
                {
                    FirstName = "Second",
                    LastName = "Evaluator",
                    EvaluatorEmail = "duplicate@test.com", // Duplicate email
                    CompanyName = "Test Company"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/evaluators/bulk", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
