using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Properties;

[RequireRole(Roles.TenantAdmin, Roles.Agent)]
public sealed record UploadPropertyImageCommand(
    Guid PropertyId,
    Stream ImageStream,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes
) : IRequest<Result<PropertyResponse>>;
