using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

public sealed record GetLocationSuggestionsQuery(string? Q = null)
    : IRequest<Result<IReadOnlyList<LocationSuggestionResponse>>>;
