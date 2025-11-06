using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Validators;

public class StatusValidator : AbstractValidator<Status>
{
    public StatusValidator(TaskDbContext context)
    {
        RuleFor(s => s.Name)
            .NotEmpty().WithMessage("Status name is required.")
            .MaximumLength(50).WithMessage("Status name cannot exceed 50 characters.");

        RuleFor(s => s.Name)
            .MustAsync(async (status, name, cancellation) =>
            {
                return !await context.Statuses
                    .AnyAsync(s => s.Name == name && s.Id != status.Id, cancellation);
            })
            .WithMessage("A status with the same name already exists.");

        RuleFor(s => s.Description)
            .MaximumLength(200).WithMessage("Status description cannot exceed 200 characters.");
    }
}
