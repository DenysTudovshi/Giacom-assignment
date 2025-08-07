using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;
using Order.Data;
using Order.Data.Entities;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests
{
    public class OrderServiceTests
    {
        private IOrderService _orderService;
        private IOrderRepository _orderRepository;
        private OrderContext _orderContext;
        private DbConnection _connection;

        private readonly byte[] _orderStatusCreatedId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderStatusFailedId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderServiceEmailId = Guid.NewGuid().ToByteArray();
        private readonly byte[] _orderProductEmailId = Guid.NewGuid().ToByteArray();


        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<OrderContext>()
                .UseSqlite(CreateInMemoryDatabase())
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .Options;

            _connection = RelationalOptionsExtension.Extract(options).Connection;

            _orderContext = new OrderContext(options);
            _orderContext.Database.EnsureDeleted();
            _orderContext.Database.EnsureCreated();

            _orderRepository = new OrderRepository(_orderContext);
            _orderService = new OrderService(_orderRepository);

            await AddReferenceDataAsync(_orderContext);
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
            _orderContext.Dispose();
        }


        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            return connection;
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsCorrectNumberOfOrders()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            Assert.AreEqual(3, orders.Count());
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsOrdersWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            var order1 = orders.SingleOrDefault(x => x.Id == orderId1);
            var order2 = orders.SingleOrDefault(x => x.Id == orderId2);
            var order3 = orders.SingleOrDefault(x => x.Id == orderId3);

            Assert.AreEqual(0.8m, order1.TotalCost);
            Assert.AreEqual(0.9m, order1.TotalPrice);

            Assert.AreEqual(1.6m, order2.TotalCost);
            Assert.AreEqual(1.8m, order2.TotalPrice);

            Assert.AreEqual(2.4m, order3.TotalCost);
            Assert.AreEqual(2.7m, order3.TotalPrice);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrder()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(orderId1, order.Id);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrderItemCount()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1, order.Items.Count());
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsOrderWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, 2);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1.6m, order.TotalCost);
            Assert.AreEqual(1.8m, order.TotalPrice);
        }

        [Test]
        public async Task GetOrdersByStatusAsync_ReturnsOrdersWithSpecifiedStatus()
        {
            // Arrange
            var createdOrderId = Guid.NewGuid();
            await AddOrderWithStatus(createdOrderId, 1, _orderStatusCreatedId);

            var failedOrderId = Guid.NewGuid();
            await AddOrderWithStatus(failedOrderId, 2, _orderStatusFailedId);

            // Act
            var failedOrders = await _orderService.GetOrdersByStatusAsync(OrderStatusType.Failed.GetStatusName());

            // Assert
            Assert.AreEqual(1, failedOrders.Count());
            Assert.AreEqual(failedOrderId, failedOrders.First().Id);
            Assert.AreEqual(OrderStatusType.Failed.GetStatusName(), failedOrders.First().StatusName);
        }

        [Test]
        public async Task GetOrdersByStatusAsync_IsCaseInsensitive()
        {
            // Arrange
            var failedOrderId = Guid.NewGuid();
            await AddOrderWithStatus(failedOrderId, 1, _orderStatusFailedId);

            // Act
            var failedOrders = await _orderService.GetOrdersByStatusAsync("FAILED");

            // Assert
            Assert.AreEqual(1, failedOrders.Count());
            Assert.AreEqual(failedOrderId, failedOrders.First().Id);
        }

        [Test]
        public void GetOrdersByStatusAsync_ThrowsArgumentExceptionForNullOrEmptyStatus()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetOrdersByStatusAsync(null));
            Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetOrdersByStatusAsync(""));
            Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetOrdersByStatusAsync("   "));
        }

        [Test]
        public async Task GetOrderByIdAsync_NonExistentId_ReturnsNull()
        {
            // Arrange
            var nonExistentOrderId = Guid.NewGuid();

            // Act
            var order = await _orderService.GetOrderByIdAsync(nonExistentOrderId);

            // Assert
            Assert.IsNull(order);
        }

        // UpdateOrderStatusAsync Tests
        [Test]
        public async Task UpdateOrderStatusAsync_ValidInput_ReturnsTrue()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrder(orderId, 1);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, OrderStatusType.Processing.GetStatusName());

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void UpdateOrderStatusAsync_EmptyOrderId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.UpdateOrderStatusAsync(Guid.Empty, OrderStatusType.Processing.GetStatusName()));
            Assert.AreEqual("Order ID cannot be empty (Parameter 'orderId')", ex.Message);
        }

        [Test]
        public void UpdateOrderStatusAsync_NullStatusName_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), null));
            Assert.AreEqual("Status name cannot be null or empty (Parameter 'statusName')", ex.Message);
        }

        [Test]
        public void UpdateOrderStatusAsync_EmptyStatusName_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), ""));
            Assert.AreEqual("Status name cannot be null or empty (Parameter 'statusName')", ex.Message);
        }

        [Test]
        public void UpdateOrderStatusAsync_WhitespaceStatusName_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), "   "));
            Assert.AreEqual("Status name cannot be null or empty (Parameter 'statusName')", ex.Message);
        }

        [Test]
        public async Task UpdateOrderStatusAsync_OrderNotFound_ReturnsFalse()
        {
            // Arrange
            var nonExistentOrderId = Guid.NewGuid();

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(nonExistentOrderId, OrderStatusType.Processing.GetStatusName());

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task UpdateOrderStatusAsync_StatusNotFound_ThrowsArgumentException()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrder(orderId, 1);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.UpdateOrderStatusAsync(orderId, "NonExistentStatus"));
            Assert.AreEqual("Status 'NonExistentStatus' not found (Parameter 'statusName')", ex.Message);
        }

        // CreateOrderAsync Tests
        [Test]
        public async Task CreateOrderAsync_ValidInput_ReturnsOrderId()
        {
            // Arrange
            var orderDto = new CreateOrderDto
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = new Guid(_orderProductEmailId),
                        ServiceId = new Guid(_orderServiceEmailId),
                        Quantity = 2
                    }
                }
            };

            // Act
            var orderId = await _orderService.CreateOrderAsync(orderDto);

            // Assert
            Assert.AreNotEqual(Guid.Empty, orderId);
        }

        [Test]
        public void CreateOrderAsync_NullDto_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(() => _orderService.CreateOrderAsync(null));
            Assert.AreEqual("Create order DTO cannot be null (Parameter 'orderDto')", ex.Message);
        }

        [Test]
        public void CreateOrderAsync_EmptyResellerId_ThrowsArgumentException()
        {
            // Arrange
            var orderDto = new CreateOrderDto
            {
                ResellerId = Guid.Empty,
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = new Guid(_orderProductEmailId),
                        ServiceId = new Guid(_orderServiceEmailId),
                        Quantity = 1
                    }
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(orderDto));
            Assert.AreEqual("Reseller ID cannot be empty (Parameter 'ResellerId')", ex.Message);
        }

        [Test]
        public void CreateOrderAsync_EmptyCustomerId_ThrowsArgumentException()
        {
            // Arrange
            var orderDto = new CreateOrderDto
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.Empty,
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = new Guid(_orderProductEmailId),
                        ServiceId = new Guid(_orderServiceEmailId),
                        Quantity = 1
                    }
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(orderDto));
            Assert.AreEqual("Customer ID cannot be empty (Parameter 'CustomerId')", ex.Message);
        }

        [Test]
        public void CreateOrderAsync_NullItems_ThrowsArgumentException()
        {
            // Arrange
            var orderDto = new CreateOrderDto
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = null
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(orderDto));
            Assert.AreEqual("Order must contain at least one item (Parameter 'Items')", ex.Message);
        }

        [Test]
        public void CreateOrderAsync_EmptyItems_ThrowsArgumentException()
        {
            // Arrange
            var orderDto = new CreateOrderDto
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>()
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(orderDto));
            Assert.AreEqual("Order must contain at least one item (Parameter 'Items')", ex.Message);
        }

        [Test]
        public void CreateOrderAsync_ProductNotFound_ThrowsArgumentException()
        {
            // Arrange
            var orderDto = new CreateOrderDto
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = Guid.NewGuid(), // Non-existent product
                        ServiceId = new Guid(_orderServiceEmailId),
                        Quantity = 1
                    }
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(orderDto));
            Assert.AreEqual("One or more products not found", ex.Message);
        }

        [Test]
        public void CreateOrderAsync_ServiceNotFound_ThrowsArgumentException()
        {
            // Arrange
            var orderDto = new CreateOrderDto
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = new Guid(_orderProductEmailId),
                        ServiceId = Guid.NewGuid(), // Non-existent service
                        Quantity = 1
                    }
                }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.CreateOrderAsync(orderDto));
            Assert.AreEqual("One or more services not found", ex.Message);
        }

        // GetProfitByMonthAsync Tests
        [Test]
        public async Task GetProfitByMonthAsync_ValidInput_ReturnsData()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrderWithStatusAndDate(orderId, 1, OrderStatusType.Completed.GetStatusName(), DateTime.UtcNow);

            // Act
            var profitData = await _orderService.GetProfitByMonthAsync(DateTime.UtcNow.Year, DateTime.UtcNow.Month);

            // Assert
            Assert.IsNotNull(profitData);
            var profitList = profitData.ToList();
            Assert.AreEqual(1, profitList.Count);
            Assert.AreEqual(DateTime.UtcNow.Year, profitList.First().Year);
            Assert.AreEqual(DateTime.UtcNow.Month, profitList.First().Month);
        }

        [Test]
        public void GetProfitByMonthAsync_YearTooLow_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetProfitByMonthAsync(1899, null));
            Assert.AreEqual("Year must be greater than 1900 (Parameter 'year')", ex.Message);
        }

        [Test]
        public void GetProfitByMonthAsync_YearTooHigh_ThrowsArgumentException()
        {
            // Arrange
            var futureYear = DateTime.Now.Year + 2;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetProfitByMonthAsync(futureYear, null));
            Assert.AreEqual($"Year cannot be greater than {DateTime.Now.Year + 1} (Parameter 'year')", ex.Message);
        }

        [Test]
        public void GetProfitByMonthAsync_MonthTooLow_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetProfitByMonthAsync(2024, 0));
            Assert.AreEqual("Month must be between 1 and 12 (Parameter 'month')", ex.Message);
        }

        [Test]
        public void GetProfitByMonthAsync_MonthTooHigh_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetProfitByMonthAsync(2024, 13));
            Assert.AreEqual("Month must be between 1 and 12 (Parameter 'month')", ex.Message);
        }

        [Test]
        public void GetProfitByMonthAsync_MonthWithoutYear_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetProfitByMonthAsync(null, 5));
            Assert.AreEqual("Year is required when Month is specified (Parameter 'year')", ex.Message);
        }

        [Test]
        public async Task GetProfitByMonthAsync_NoYear_ReturnsAllData()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrderWithStatusAndDate(orderId1, 1, OrderStatusType.Completed.GetStatusName(), DateTime.UtcNow);
            
            var orderId2 = Guid.NewGuid();
            await AddOrderWithStatusAndDate(orderId2, 1, OrderStatusType.Completed.GetStatusName(), DateTime.UtcNow.AddYears(-1));

            // Act
            var profitData = await _orderService.GetProfitByMonthAsync(null, null);

            // Assert
            Assert.IsNotNull(profitData);
            var profitList = profitData.ToList();
            Assert.GreaterOrEqual(profitList.Count, 2);
        }

        private async Task AddOrder(Guid orderId, int quantity)
        {
            var orderIdBytes = orderId.ToByteArray();
            _orderContext.Order.Add(new Data.Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = Guid.NewGuid().ToByteArray(),
                CustomerId = Guid.NewGuid().ToByteArray(),
                CreatedDate = DateTime.Now,
                StatusId = _orderStatusCreatedId,
            });

            _orderContext.OrderItem.Add(new Order.Data.Entities.OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ServiceId = _orderServiceEmailId,
                ProductId = _orderProductEmailId,
                Quantity = quantity
            });

            await _orderContext.SaveChangesAsync();
        }

        private async Task AddOrderWithStatus(Guid orderId, int quantity, byte[] statusId)
        {
            var orderIdBytes = orderId.ToByteArray();
            _orderContext.Order.Add(new Data.Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = Guid.NewGuid().ToByteArray(),
                CustomerId = Guid.NewGuid().ToByteArray(),
                CreatedDate = DateTime.Now,
                StatusId = statusId,
            });

            _orderContext.OrderItem.Add(new Order.Data.Entities.OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ServiceId = _orderServiceEmailId,
                ProductId = _orderProductEmailId,
                Quantity = quantity
            });

            await _orderContext.SaveChangesAsync();
        }

        private async Task AddOrderWithStatusAndDate(Guid orderId, int quantity, string statusName, DateTime createdDate)
        {
            var orderIdBytes = orderId.ToByteArray();
            
            // Find the status by name
            var status = await _orderContext.OrderStatus
                .FirstOrDefaultAsync(s => s.Name.ToLower() == statusName.ToLower());
            
            if (status == null)
            {
                // Add the status if it doesn't exist
                status = new OrderStatus
                {
                    Id = Guid.NewGuid().ToByteArray(),
                    Name = statusName
                };
                _orderContext.OrderStatus.Add(status);
                await _orderContext.SaveChangesAsync();
            }

            _orderContext.Order.Add(new Data.Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = Guid.NewGuid().ToByteArray(),
                CustomerId = Guid.NewGuid().ToByteArray(),
                CreatedDate = createdDate,
                StatusId = status.Id,
            });

            _orderContext.OrderItem.Add(new Order.Data.Entities.OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ServiceId = _orderServiceEmailId,
                ProductId = _orderProductEmailId,
                Quantity = quantity
            });

            await _orderContext.SaveChangesAsync();
        }

        private async Task AddReferenceDataAsync(OrderContext orderContext)
        {
            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = _orderStatusCreatedId,
                Name = OrderStatusType.Created.GetStatusName(),
            });

            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = _orderStatusFailedId,
                Name = OrderStatusType.Failed.GetStatusName(),
            });

            // Add additional statuses for testing
            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = Guid.NewGuid().ToByteArray(),
                Name = OrderStatusType.Pending.GetStatusName(),
            });

            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = Guid.NewGuid().ToByteArray(),
                Name = OrderStatusType.Processing.GetStatusName(),
            });

            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = Guid.NewGuid().ToByteArray(),
                Name = OrderStatusType.Completed.GetStatusName(),
            });

            orderContext.OrderService.Add(new Data.Entities.OrderService
            {
                Id = _orderServiceEmailId,
                Name = "Email"
            });

            orderContext.OrderProduct.Add(new OrderProduct
            {
                Id = _orderProductEmailId,
                Name = "100GB Mailbox",
                UnitCost = 0.8m,
                UnitPrice = 0.9m,
                ServiceId = _orderServiceEmailId
            });

            await orderContext.SaveChangesAsync();
        }
    }
}
