using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderRepository.GetOrdersAsync();
            return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            return order;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName)
        {
            var orders = await _orderRepository.GetOrdersByStatusAsync(statusName);
            return orders;
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, string statusName)
        {
            // Input validation - this is business logic, belongs in service layer
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
            }

            if (string.IsNullOrWhiteSpace(statusName))
            {
                throw new ArgumentException("Status name cannot be null or empty", nameof(statusName));
            }

            // Call repository for data operation
            var result = await _orderRepository.UpdateOrderStatusAsync(orderId, statusName);

            // Handle repository results and translate to business logic
            switch (result)
            {
                case UpdateOrderStatusResult.Success:
                    return true;
                case UpdateOrderStatusResult.OrderNotFound:
                    return false; // Let controller handle 404
                case UpdateOrderStatusResult.StatusNotFound:
                    throw new ArgumentException($"Status '{statusName}' not found", nameof(statusName));
                case UpdateOrderStatusResult.UpdateFailed:
                    throw new InvalidOperationException("Failed to update order status due to a database error");
                default:
                    throw new InvalidOperationException("Unknown error occurred while updating order status");
            }
        }
    }
}
