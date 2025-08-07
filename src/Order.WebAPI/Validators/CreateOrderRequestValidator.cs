using FluentValidation;
using OrderService.WebAPI.Models;
using System;

namespace OrderService.WebAPI.Validators
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.ResellerId)
                .NotEmpty()
                .WithMessage("Reseller ID is required")
                .Must(BeAValidGuid)
                .WithMessage("Reseller ID must be a valid GUID");

            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Customer ID is required")
                .Must(BeAValidGuid)
                .WithMessage("Customer ID must be a valid GUID");

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("At least one order item is required")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("Order must contain at least one item");

            RuleForEach(x => x.Items)
                .SetValidator(new CreateOrderItemRequestValidator());
        }

        private static bool BeAValidGuid(Guid id)
        {
            return id != Guid.Empty;
        }
    }

    public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
    {
        public CreateOrderItemRequestValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product ID is required")
                .Must(BeAValidGuid)
                .WithMessage("Product ID must be a valid GUID");

            RuleFor(x => x.ServiceId)
                .NotEmpty()
                .WithMessage("Service ID is required")
                .Must(BeAValidGuid)
                .WithMessage("Service ID must be a valid GUID");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than 0")
                .LessThanOrEqualTo(1000)
                .WithMessage("Quantity cannot exceed 1000");
        }

        private static bool BeAValidGuid(Guid id)
        {
            return id != Guid.Empty;
        }
    }
}
