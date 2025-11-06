using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Validators;

public class TaskItemValidator : AbstractValidator<TaskItem>
{
    public TaskItemValidator(TaskDbContext context)
    {
        RuleFor(t => t.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(t => t.StatusId)
            .MustAsync(async (statusId, cancellation) =>
            {
                return await context.Statuses.AnyAsync(s => s.Id == statusId, cancellation);
            })
            .When(t => t.Id == 0) // Only validate on create (new entities have Id = 0)
            .WithMessage("StatusId must be a valid status.");
    }
}
