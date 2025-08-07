using System;
using System.Collections.Generic;

namespace Order.Model
{
    public class CreateOrderDto
    {
        public Guid ResellerId { get; set; }
        public Guid CustomerId { get; set; }
        public ICollection<CreateOrderItemDto> Items { get; set; } = new List<CreateOrderItemDto>();
    }

    public class CreateOrderItemDto
    {
        public Guid ProductId { get; set; }
        public Guid ServiceId { get; set; }
        public int Quantity { get; set; }
    }
}
