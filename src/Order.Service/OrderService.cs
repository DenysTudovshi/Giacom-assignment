using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<Guid> CreateOrderAsync(CreateOrderDto orderDto)
        {
            // Input validation - this is business logic, belongs in service layer
            if (orderDto == null)
            {
                throw new ArgumentNullException(nameof(orderDto), "Create order DTO cannot be null");
            }

            if (orderDto.ResellerId == Guid.Empty)
            {
                throw new ArgumentException("Reseller ID cannot be empty", nameof(orderDto.ResellerId));
            }

            if (orderDto.CustomerId == Guid.Empty)
            {
                throw new ArgumentException("Customer ID cannot be empty", nameof(orderDto.CustomerId));
            }

            if (orderDto.Items == null || !orderDto.Items.Any())
            {
                throw new ArgumentException("Order must contain at least one item", nameof(orderDto.Items));
            }

            // Call repository for data operation
            var (result, orderId) = await _orderRepository.CreateOrderAsync(orderDto);

            // Handle repository results and translate to business logic
            switch (result)
            {
                case CreateOrderResult.Success:
                    return orderId.Value;
                case CreateOrderResult.ProductNotFound:
                    throw new ArgumentException("One or more products not found");
                case CreateOrderResult.ServiceNotFound:
                    throw new ArgumentException("One or more services not found");
                case CreateOrderResult.CreationFailed:
                    throw new InvalidOperationException("Failed to create order due to a database error");
                default:
                    throw new InvalidOperationException("Unknown error occurred while creating order");
            }
        }
    }
}
