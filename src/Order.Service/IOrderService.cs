using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Service
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderSummary>> GetOrdersAsync();
        
        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

        Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName);

        Task<bool> UpdateOrderStatusAsync(Guid orderId, string statusName);

        Task<Guid> CreateOrderAsync(CreateOrderDto orderDto);
    }
}
