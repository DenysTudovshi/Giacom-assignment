using System;
using System.Collections.Generic;

namespace OrderService.WebAPI.Models
{
    public class CreateOrderRequest
    {
        public Guid ResellerId { get; set; }
        public Guid CustomerId { get; set; }
        public ICollection<CreateOrderItemRequest> Items { get; set; } = new List<CreateOrderItemRequest>();
    }

    public class CreateOrderItemRequest
    {
        public Guid ProductId { get; set; }
        public Guid ServiceId { get; set; }
        public int Quantity { get; set; }
    }
}
