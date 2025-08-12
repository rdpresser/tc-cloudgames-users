using TC.CloudGames.Users.Api.Extensions;
using TC.CloudGames.Users.Application;
using TC.CloudGames.Users.Application.Abstractions.Ports;
using TC.CloudGames.Users.Application.UseCases.CreateUser;
using TC.CloudGames.Users.Domain.Aggregates;
using TC.CloudGames.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddUserServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
// Adicionar serviços para API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.


// Endpoints da API de usuários usando APIs mínimas
app.MapPost("/api/users", async (CreateUserCommand request, IUserRepository userRepository) =>
{
    var user = UserAggregate.CreateFromPrimitives(request.Name, request.Email, request.Username, request.Password, request.Role);

    if (!user.IsSuccess)
    {
        return Results.BadRequest(new { Errors = user.ValidationErrors.Select(e => new { e.Identifier, e.ErrorMessage }) });
    }

    await userRepository.SaveAsync(user);

    return Results.Created($"/api/users/{user.Value.Id}", new { Id = user.Value.Id });
})
.WithName("CreateUser")
.WithOpenApi();

await app.RunAsync();