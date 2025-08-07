using System;

namespace OrderService.WebAPI.Models
{
    public class UpdateOrderStatusRequest
    {
        public Guid OrderId { get; set; }
        public string StatusName { get; set; }
    }
}
