using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Data
{
    public interface IOrderRepository
    {
        Task<IEnumerable<OrderSummary>> GetOrdersAsync();

        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);

        Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName);

        Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string statusName);

        Task<(CreateOrderResult Result, Guid? OrderId)> CreateOrderAsync(CreateOrderDto orderDto);

        Task<IEnumerable<ProfitByMonthDto>> GetProfitByMonthAsync(int? year, int? month);
    }
}
