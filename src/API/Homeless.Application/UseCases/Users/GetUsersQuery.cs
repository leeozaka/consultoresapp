using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Users;

public sealed record GetUsersQuery : IRequest<Result<IReadOnlyList<UserResponse>>>;
