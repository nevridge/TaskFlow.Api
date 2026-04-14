using FluentValidation;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Validators;

public class TaskItemValidator : AbstractValidator<TaskItem>
{
    public TaskItemValidator()
    {
        RuleFor(t => t.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(t => t.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid value (Draft, Todo, or Completed).");

        RuleFor(t => t.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid value (Low, Medium, or High).");
    }
}
