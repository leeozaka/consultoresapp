using Ardalis.Result;
using MediatR;
using Homeless.Application.Authorization;
using Homeless.Application.DTOs;

namespace Homeless.Application.UseCases.Tenants;

[RequireRole(Roles.TenantAdmin)]
public sealed record UploadTenantPortalImageCommand(
    Stream ImageStream,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes
) : IRequest<Result<PortalAssetUploadResponse>>;
