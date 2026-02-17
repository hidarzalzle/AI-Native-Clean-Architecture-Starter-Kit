using Application.Commands;
using FluentValidation;

namespace Application.Validators;

public class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerEmail).EmailAddress();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
