using Ardalis.Result;
using MediatR;

namespace Homeless.Application.UseCases.Auth;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName
) : IRequest<Result<RegisteredUserResponse>>;

public sealed record RegisteredUserResponse(Guid Id, string Email);
