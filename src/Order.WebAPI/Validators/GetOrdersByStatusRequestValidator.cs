using FluentValidation;
using Order.Data;
using OrderService.WebAPI.Models;
using System;
using System.Linq;

namespace OrderService.WebAPI.Validators
{
    public class GetOrdersByStatusRequestValidator : AbstractValidator<GetOrdersByStatusRequest>
    {
        private static readonly string[] ValidStatuses = OrderStatusTypeExtensions.GetAllStatusNames();

        public GetOrdersByStatusRequestValidator()
        {
            RuleFor(x => x.StatusName)
                .NotEmpty()
                .WithMessage("Order status is required")
                .Must(BeAValidStatus)
                .WithMessage($"Order status must be one of the following values: {string.Join(", ", ValidStatuses)}. The comparison is case-insensitive.");
        }

        private static bool BeAValidStatus(string statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
                return false;

            return OrderStatusTypeExtensions.TryParseStatusName(statusName, out _);
        }
    }
}
