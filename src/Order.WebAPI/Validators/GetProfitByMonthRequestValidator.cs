using FluentValidation;
using OrderService.WebAPI.Models;
using System;

namespace OrderService.WebAPI.Validators
{
    public class GetProfitByMonthRequestValidator : AbstractValidator<GetProfitByMonthRequest>
    {
        public GetProfitByMonthRequestValidator()
        {
            RuleFor(x => x.Year)
                .GreaterThan(1900)
                .WithMessage("Year must be greater than 1900")
                .LessThanOrEqualTo(DateTime.Now.Year + 1)
                .WithMessage($"Year cannot be greater than {DateTime.Now.Year + 1}")
                .When(x => x.Year.HasValue);

            RuleFor(x => x.Month)
                .InclusiveBetween(1, 12)
                .WithMessage("Month must be between 1 and 12")
                .When(x => x.Month.HasValue);

            // If month is specified, year must also be specified
            RuleFor(x => x.Year)
                .NotNull()
                .WithMessage("Year is required when Month is specified")
                .When(x => x.Month.HasValue);
        }
    }
}
