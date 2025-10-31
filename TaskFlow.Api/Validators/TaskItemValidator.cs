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
    }
}