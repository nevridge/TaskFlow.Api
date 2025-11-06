using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StatusController(IStatusService statusService, IValidator<Status> validator) : ControllerBase
{
    private const string ApiVersionString = "1.0";
    private readonly IStatusService _statusService = statusService;
    private readonly IValidator<Status> _validator = validator;

    // GET: api/v1/Status
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StatusResponseDto>>> GetStatuses()
    {
        var statuses = await _statusService.GetAllStatusesAsync();
        var dtos = statuses.Select(s => new StatusResponseDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description
        });
        return Ok(dtos);
    }

    // GET: api/v1/Status/5
    [HttpGet("{id}", Name = "GetStatusV1")]
    public async Task<ActionResult<StatusResponseDto>> GetStatus(int id)
    {
        var status = await _statusService.GetStatusAsync(id);

        if (status is null)
        {
            return NotFound();
        }

        var statusDto = new StatusResponseDto
        {
            Id = status.Id,
            Name = status.Name,
            Description = status.Description
        };
        return Ok(statusDto);
    }

    // POST: api/v1/Status
    [HttpPost]
    public async Task<ActionResult<StatusResponseDto>> Create([FromBody] CreateStatusDto createDto)
    {
        var status = new Status
        {
            Name = createDto.Name,
            Description = createDto.Description
        };

        var validationResult = await _validator.ValidateAsync(status);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var createdStatus = await _statusService.CreateStatusAsync(status);
        var responseDto = new StatusResponseDto
        {
            Id = createdStatus.Id,
            Name = createdStatus.Name,
            Description = createdStatus.Description
        };
        return CreatedAtRoute("GetStatusV1", new { version = ApiVersionString, id = responseDto.Id }, responseDto);
    }

    // PUT: api/v1/Status/5
    [HttpPut("{id}")]
    public async Task<ActionResult<StatusResponseDto>> UpdateStatus(int id, [FromBody] UpdateStatusDto updateDto)
    {
        var existingStatus = await _statusService.GetStatusAsync(id);
        if (existingStatus is null)
        {
            return NotFound();
        }

        // Apply incoming changes
        existingStatus.Name = updateDto.Name;
        existingStatus.Description = updateDto.Description;

        var validationResult = await _validator.ValidateAsync(existingStatus);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        await _statusService.UpdateStatusAsync(existingStatus);

        var responseDto = new StatusResponseDto
        {
            Id = existingStatus.Id,
            Name = existingStatus.Name,
            Description = existingStatus.Description
        };

        return Ok(responseDto);
    }

    // DELETE: api/v1/Status/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteStatus(int id)
    {
        var existingStatus = await _statusService.GetStatusAsync(id);
        if (existingStatus is null)
        {
            return NotFound();
        }

        await _statusService.DeleteStatusAsync(id);
        return NoContent();
    }
}
