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
    public async Task<ActionResult<IEnumerable<Status>>> GetStatuses()
    {
        var statuses = await _statusService.GetAllStatusesAsync();
        return Ok(statuses);
    }

    // GET: api/v1/Status/5
    [HttpGet("{id}", Name = "GetStatusV1")]
    public async Task<ActionResult<Status>> GetStatus(int id)
    {
        var status = await _statusService.GetStatusAsync(id);

        if (status is null) return NotFound();
        return Ok(status);
    }

    // POST: api/v1/Status
    [HttpPost]
    public async Task<ActionResult<Status>> Create([FromBody] CreateStatusDto createDto)
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
        return CreatedAtRoute("GetStatusV1", new { version = ApiVersionString, id = createdStatus.Id }, createdStatus);
    }

    // PUT: api/v1/Status/5
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto updateDto)
    {
        var existingStatus = await _statusService.GetStatusAsync(id);
        if (existingStatus is null) return NotFound();

        // Apply incoming changes
        existingStatus.Name = updateDto.Name;
        existingStatus.Description = updateDto.Description;

        var validationResult = await _validator.ValidateAsync(existingStatus);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        await _statusService.UpdateStatusAsync(existingStatus);
        return NoContent();
    }

    // DELETE: api/v1/Status/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteStatus(int id)
    {
        var existingStatus = await _statusService.GetStatusAsync(id);
        if (existingStatus is null) return NotFound();

        await _statusService.DeleteStatusAsync(id);
        return NoContent();
    }
}
