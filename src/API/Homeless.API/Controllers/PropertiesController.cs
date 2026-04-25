using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;
using Homeless.Application.Mappers;
using Homeless.Application.UseCases.Properties;

namespace Homeless.API.Controllers;

/// <summary>
/// Real estate property management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[TranslateResultToActionResult]
public sealed class PropertiesController(
    IMediator mediator,
    IImageProcessedStream imageProcessedStream) : ControllerBase
{
    /// <summary>
    /// Creates a new property listing.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ExpectedFailures(ResultStatus.Invalid, ResultStatus.Forbidden)]
    public async Task<Result<PropertyResponse>> Create(
        [FromBody] CreatePropertyRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new CreatePropertyCommand(request.ToWriteData(), request.Country),
            cancellationToken);

    /// <summary>
    /// Updates an existing property.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.Invalid, ResultStatus.NotFound, ResultStatus.Forbidden)]
    public async Task<Result<PropertyResponse>> Update(
        Guid id,
        [FromBody] UpdatePropertyRequest request,
        CancellationToken cancellationToken) =>
        await mediator.Send(
            new UpdatePropertyCommand(id, request.ToWriteData()),
            cancellationToken);

    /// <summary>
    /// Publishes a property listing (makes it publicly visible).
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Forbidden, ResultStatus.Unauthorized)]
    public async Task<Result<PropertyResponse>> Publish(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new PublishPropertyCommand(id), cancellationToken);

    /// <summary>
    /// Deactivates an active property listing (changes status to Draft).
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Forbidden)]
    public async Task<Result<PropertyResponse>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new DeactivatePropertyCommand(id), cancellationToken);

    /// <summary>
    /// Gets a single property by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound)]
    public async Task<Result<PropertyResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetPropertyQuery(id), cancellationToken);

    /// <summary>Returns location suggestions (cities + neighbourhoods) for AutoComplete.</summary>
    [HttpGet("suggestions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<LocationSuggestionResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<LocationSuggestionResponse>> GetSuggestions(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetLocationSuggestionsQuery(q), cancellationToken);
        return PaginatedResponse<LocationSuggestionResponse>.CreateFrom(result.Value, page, pageSize);
    }

    /// <summary>
    /// Returns featured property listings for the public landing page.
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<FeaturedPropertyResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<FeaturedPropertyResponse>> GetFeatured(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6,
        CancellationToken cancellationToken = default) =>
        (await mediator.Send(new GetFeaturedPropertiesQuery(page, pageSize), cancellationToken)).Value;

    /// <summary>
    /// Searches and paginates property listings.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponse<PropertyResponse>), StatusCodes.Status200OK)]
    public async Task<PaginatedResponse<PropertyResponse>> Search(
        [FromQuery] PropertySearchRequest filter,
        CancellationToken cancellationToken = default) =>
        (await mediator.Send(
            new SearchPropertiesQuery(filter.ToFilter(), filter.Page, filter.PageSize),
            cancellationToken)).Value;

    /// <summary>
    /// Uploads an image to a property.
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.Invalid, ResultStatus.NotFound, ResultStatus.Forbidden)]
    public async Task<Result<PropertyResponse>> UploadImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await mediator.Send(
            new UploadPropertyImageCommand(id, stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);
    }

    /// <summary>
    /// Deletes an image from a property.
    /// </summary>
    [HttpDelete("{id:guid}/images")]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Forbidden)]
    public async Task<Result<PropertyResponse>> DeleteImage(
        Guid id,
        [FromQuery] string key,
        CancellationToken cancellationToken) =>
        await mediator.Send(new DeletePropertyImageCommand(id, key), cancellationToken);

    /// <summary>
    /// Server-Sent Events stream for real-time image processing status updates.
    /// Pushes an event each time a property image finishes WebP processing.
    /// </summary>
    [HttpGet("{id:guid}/images/events")]
    [Produces("text/event-stream")]
    public async Task ImageEvents(Guid id, CancellationToken cancellationToken)
    {
        var stream = imageProcessedStream.ReadAllAsync(id, cancellationToken);
        await TypedResults.ServerSentEvents(stream, eventType: "image-processed").ExecuteAsync(HttpContext);
    }
}

