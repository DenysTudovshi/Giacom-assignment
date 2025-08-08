using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Data
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderContext _orderContext;

        public OrderRepository(OrderContext orderContext)
        {
            _orderContext = orderContext;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            var orders = await _orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Select(x => new OrderSummary
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    ItemCount = x.Items.Count,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    CreatedDate = x.CreatedDate
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            var orderIdBytes = orderId.ToByteArray();

            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .Select(x => new OrderDetail
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    CreatedDate = x.CreatedDate,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    Items = x.Items.Select(i => new Model.OrderItem
                    {
                        Id = new Guid(i.Id),
                        OrderId = new Guid(i.OrderId),
                        ServiceId = new Guid(i.ServiceId),
                        ServiceName = i.Service.Name,
                        ProductId = new Guid(i.ProductId),
                        ProductName = i.Product.Name,
                        UnitCost = i.Product.UnitCost,
                        UnitPrice = i.Product.UnitPrice,
                        TotalCost = i.Product.UnitCost * i.Quantity.Value,
                        TotalPrice = i.Product.UnitPrice * i.Quantity.Value,
                        Quantity = i.Quantity.Value
                    })
                }).SingleOrDefaultAsync();
            
            return order;
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                throw new ArgumentException("Status name cannot be null or empty", nameof(statusName));
            }

            // Validate that the status name is a valid enum value
            if (!OrderStatusTypeExtensions.TryParseStatusName(statusName, out var statusType))
            {
                throw new ArgumentException($"Invalid status name: {statusName}", nameof(statusName));
            }

            var validStatusName = statusType.GetStatusName();

            var orders = await _orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Where(x => x.Status.Name.ToLower() == validStatusName.ToLower())
                .Select(x => new OrderSummary
                {
                    Id = new Guid(x.Id),
                    ResellerId = new Guid(x.ResellerId),
                    CustomerId = new Guid(x.CustomerId),
                    StatusId = new Guid(x.StatusId),
                    StatusName = x.Status.Name,
                    ItemCount = x.Items.Count,
                    TotalCost = x.Items.Sum(i => i.Quantity * i.Product.UnitCost).Value,
                    TotalPrice = x.Items.Sum(i => i.Quantity * i.Product.UnitPrice).Value,
                    CreatedDate = x.CreatedDate
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return orders;
        }

        public async Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string statusName)
        {
            var orderIdBytes = orderId.ToByteArray();

            // Find the order
            var order = await _orderContext.Order
                .Where(x => _orderContext.Database.IsInMemory() ? x.Id.SequenceEqual(orderIdBytes) : x.Id == orderIdBytes)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return UpdateOrderStatusResult.OrderNotFound;
            }

            // Validate that the status name is a valid enum value
            if (!OrderStatusTypeExtensions.TryParseStatusName(statusName, out var statusType))
            {
                return UpdateOrderStatusResult.StatusNotFound;
            }

            var validStatusName = statusType.GetStatusName();

            // Find the status by name
            var status = await _orderContext.OrderStatus
                .FirstOrDefaultAsync(s => s.Name.ToLower() == validStatusName.ToLower());

            if (status == null)
            {
                return UpdateOrderStatusResult.StatusNotFound;
            }

            // Update the order status
            order.StatusId = status.Id;

            try
            {
                await _orderContext.SaveChangesAsync();
                return UpdateOrderStatusResult.Success;
            }
            catch
            {
                return UpdateOrderStatusResult.UpdateFailed;
            }
        }

        public async Task<(CreateOrderResult Result, Guid? OrderId)> CreateOrderAsync(CreateOrderDto orderDto)
        {
                    // Get the "Created" status for new orders
        var createdStatusName = OrderStatusType.Created.GetStatusName();
        var createdStatus = await _orderContext.OrderStatus
            .FirstOrDefaultAsync(s => s.Name.ToLower() == createdStatusName.ToLower());

            if (createdStatus == null)
            {
                return (CreateOrderResult.CreationFailed, null);
            }

            // Validate that all products exist
            var productIds = orderDto.Items.Select(i => i.ProductId.ToByteArray()).ToList();
            var existingProductsCount = await _orderContext.OrderProduct
                .Where(p => productIds.Any(pid => _orderContext.Database.IsInMemory() ? p.Id.SequenceEqual(pid) : p.Id == pid))
                .CountAsync();

            if (existingProductsCount != orderDto.Items.Count)
            {
                return (CreateOrderResult.ProductNotFound, null);
            }

            // Validate that all services exist
            var serviceIds = orderDto.Items.Select(i => i.ServiceId.ToByteArray()).ToList();
            var existingServicesCount = await _orderContext.OrderService
                .Where(s => serviceIds.Any(sid => _orderContext.Database.IsInMemory() ? s.Id.SequenceEqual(sid) : s.Id == sid))
                .CountAsync();

            if (existingServicesCount != orderDto.Items.Count)
            {
                return (CreateOrderResult.ServiceNotFound, null);
            }

            // Create the order
            var orderId = Guid.NewGuid();
            var order = new Entities.Order
            {
                Id = orderId.ToByteArray(),
                ResellerId = orderDto.ResellerId.ToByteArray(),
                CustomerId = orderDto.CustomerId.ToByteArray(),
                StatusId = createdStatus.Id,
                CreatedDate = DateTime.UtcNow
            };

            // Create order items
            foreach (var itemDto in orderDto.Items)
            {
                var orderItem = new Entities.OrderItem
                {
                    Id = Guid.NewGuid().ToByteArray(),
                    OrderId = order.Id,
                    ProductId = itemDto.ProductId.ToByteArray(),
                    ServiceId = itemDto.ServiceId.ToByteArray(),
                    Quantity = itemDto.Quantity
                };
                order.Items.Add(orderItem);
            }

            try
            {
                _orderContext.Order.Add(order);
                await _orderContext.SaveChangesAsync();
                return (CreateOrderResult.Success, orderId);
            }
            catch
            {
                return (CreateOrderResult.CreationFailed, null);
            }
        }

        public async Task<IEnumerable<ProfitByMonthDto>> GetProfitByMonthAsync(int? year, int? month)
        {
            var query = _orderContext.Order
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                .Include(x => x.Status)
                .Where(x => x.Status.Name.ToLower() == OrderStatusType.Completed.GetStatusName().ToLower());

            // Apply date range filters instead of Year/Month properties to avoid MySQL EF Core translation issues
            if (year.HasValue && month.HasValue)
            {
                var startDate = new DateTime(year.Value, month.Value, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                query = query.Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate);
            }
            else if (year.HasValue)
            {
                var startDate = new DateTime(year.Value, 1, 1);
                var endDate = new DateTime(year.Value, 12, 31);
                query = query.Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate);
            }

            // Load the filtered orders into memory first
            var orders = await query.ToListAsync();

            // Process the data in memory to avoid complex SQL translation issues
            var profitData = orders
                .GroupBy(o => new { o.CreatedDate.Year, o.CreatedDate.Month })
                .Select(g => new ProfitByMonthDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                    TotalProfit = g.Sum(o => o.Items.Sum(i => (i.Product.UnitPrice - i.Product.UnitCost) * i.Quantity.Value)),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToList();

            return profitData;
        }
    }
}
