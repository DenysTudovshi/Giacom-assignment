using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Order.Data;
using Order.Model;
using Order.Service;
using OrderService.WebAPI.Controllers;
using OrderService.WebAPI.Mapping;
using OrderService.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests
{
    [TestFixture]
    public class OrderControllerTests
    {
        private Mock<IOrderService> _mockOrderService;
        private IMapper _mapper;
        private OrderController _controller;

        [SetUp]
        public void Setup()
        {
            _mockOrderService = new Mock<IOrderService>();
            
            // Configure AutoMapper for version 15.0.1
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<MappingProfile>();
            
            var loggerFactory = NullLoggerFactory.Instance;
            var config = new MapperConfiguration(configExpression, loggerFactory);
            _mapper = config.CreateMapper();
            
            _controller = new OrderController(_mockOrderService.Object, _mapper);
            
            // Set up HttpContext for controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Get Tests

        [Test]
        public async Task Get_ReturnsOkWithOrderList()
        {
            // Arrange
            var orders = new List<OrderSummary>
            {
                new OrderSummary 
                { 
                    Id = Guid.NewGuid(), 
                    ResellerId = Guid.NewGuid(), 
                    CustomerId = Guid.NewGuid(),
                    StatusName = OrderStatusType.Pending.GetStatusName(),
                    ItemCount = 1,
                    TotalCost = 10.50m,
                    TotalPrice = 12.00m,
                    CreatedDate = DateTime.UtcNow
                }
            };

            _mockOrderService.Setup(s => s.GetOrdersAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.Get();

            // Assert
            Assert.IsInstanceOf<ActionResult<IEnumerable<OrderSummary>>>(result);
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(orders, okResult.Value);
            _mockOrderService.Verify(s => s.GetOrdersAsync(), Times.Once);
        }

        [Test]
        public async Task Get_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            _mockOrderService.Setup(s => s.GetOrdersAsync())
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Get());
            _mockOrderService.Verify(s => s.GetOrdersAsync(), Times.Once);
        }

        #endregion

        #region GetOrderById Tests

        [Test]
        public async Task GetOrderById_ExistingOrder_ReturnsOkWithOrder()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var orderDetail = new OrderDetail
            {
                Id = orderId,
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                StatusName = OrderStatusType.Pending.GetStatusName(),
                TotalCost = 10.50m,
                TotalPrice = 12.00m,
                CreatedDate = DateTime.UtcNow,
                Items = new List<Model.OrderItem>()
            };

            _mockOrderService.Setup(s => s.GetOrderByIdAsync(orderId))
                .ReturnsAsync(orderDetail);

            // Act
            var result = await _controller.GetOrderById(orderId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(orderDetail, okResult.Value);
            _mockOrderService.Verify(s => s.GetOrderByIdAsync(orderId), Times.Once);
        }

        [Test]
        public async Task GetOrderById_NonExistingOrder_ReturnsProblemDetails()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(orderId))
                .ReturnsAsync((OrderDetail)null);

            // Act
            var result = await _controller.GetOrderById(orderId);

            // Assert
            var problemResult = result.Result as ObjectResult;
            Assert.IsNotNull(problemResult);
            Assert.AreEqual(404, problemResult.StatusCode);
            
            var problemDetails = problemResult.Value as ProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("Order not found", problemDetails.Title);
            Assert.AreEqual($"No order found with ID: {orderId}", problemDetails.Detail);
            Assert.AreEqual(404, problemDetails.Status);
            
            _mockOrderService.Verify(s => s.GetOrderByIdAsync(orderId), Times.Once);
        }

        [Test]
        public async Task GetOrderById_ServiceThrowsException_ThrowsException()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(orderId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _controller.GetOrderById(orderId));
            _mockOrderService.Verify(s => s.GetOrderByIdAsync(orderId), Times.Once);
        }

        #endregion

        #region GetOrdersByStatus Tests

        [Test]
        public async Task GetOrdersByStatus_ValidStatus_ReturnsOkWithOrders()
        {
            // Arrange
            var statusName = OrderStatusType.Pending.GetStatusName();
            var orders = new List<OrderSummary>
            {
                new OrderSummary 
                { 
                    Id = Guid.NewGuid(), 
                    StatusName = statusName,
                    ItemCount = 1,
                    TotalCost = 10.50m,
                    TotalPrice = 12.00m
                }
            };

            _mockOrderService.Setup(s => s.GetOrdersByStatusAsync(statusName))
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.GetOrdersByStatus(statusName);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(orders, okResult.Value);
            _mockOrderService.Verify(s => s.GetOrdersByStatusAsync(statusName), Times.Once);
        }

        [Test]
        public async Task GetOrdersByStatus_ServiceThrowsArgumentException_ThrowsException()
        {
            // Arrange
            var statusName = "";
            _mockOrderService.Setup(s => s.GetOrdersByStatusAsync(statusName))
                .ThrowsAsync(new ArgumentException("Status name cannot be null or empty"));

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.GetOrdersByStatus(statusName));
            _mockOrderService.Verify(s => s.GetOrdersByStatusAsync(statusName), Times.Once);
        }

        #endregion

        #region GetProfitByMonth Tests

        [Test]
        public async Task GetProfitByMonth_ValidParameters_ReturnsOkWithData()
        {
            // Arrange
            var year = 2024;
            var month = 5;
            var profitData = new List<ProfitByMonthDto>
            {
                new ProfitByMonthDto
                {
                    Year = year,
                    Month = month,
                    MonthName = "May",
                    TotalProfit = 150.75m,
                    OrderCount = 5
                }
            };

            _mockOrderService.Setup(s => s.GetProfitByMonthAsync(year, month))
                .ReturnsAsync(profitData);

            // Act
            var result = await _controller.GetProfitByMonth(year, month);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(profitData, okResult.Value);
            _mockOrderService.Verify(s => s.GetProfitByMonthAsync(year, month), Times.Once);
        }

        [Test]
        public async Task GetProfitByMonth_NullParameters_ReturnsOkWithData()
        {
            // Arrange
            var profitData = new List<ProfitByMonthDto>
            {
                new ProfitByMonthDto
                {
                    Year = 2024,
                    Month = 5,
                    MonthName = "May",
                    TotalProfit = 150.75m,
                    OrderCount = 5
                }
            };

            _mockOrderService.Setup(s => s.GetProfitByMonthAsync(null, null))
                .ReturnsAsync(profitData);

            // Act
            var result = await _controller.GetProfitByMonth(null, null);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(profitData, okResult.Value);
            _mockOrderService.Verify(s => s.GetProfitByMonthAsync(null, null), Times.Once);
        }

        [Test]
        public async Task GetProfitByMonth_ServiceThrowsArgumentException_ThrowsException()
        {
            // Arrange
            var year = 1800; // Invalid year
            _mockOrderService.Setup(s => s.GetProfitByMonthAsync(year, null))
                .ThrowsAsync(new ArgumentException("Year must be greater than 1900"));

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.GetProfitByMonth(year, null));
            _mockOrderService.Verify(s => s.GetProfitByMonthAsync(year, null), Times.Once);
        }

        #endregion

        #region Create Tests

        [Test]
        public async Task Create_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new CreateOrderRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>
                {
                    new CreateOrderItemRequest
                    {
                        ProductId = Guid.NewGuid(),
                        ServiceId = Guid.NewGuid(),
                        Quantity = 2
                    }
                }
            };

            _mockOrderService.Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderDto>()))
                .ReturnsAsync(orderId);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual(201, createdResult.StatusCode);
            Assert.AreEqual(nameof(_controller.GetOrderById), createdResult.ActionName);
            
            var routeValues = createdResult.RouteValues;
            Assert.IsNotNull(routeValues);
            Assert.AreEqual(orderId, routeValues["orderId"]);
            
            var responseValue = createdResult.Value;
            Assert.IsNotNull(responseValue);
            
            _mockOrderService.Verify(s => s.CreateOrderAsync(It.Is<CreateOrderDto>(dto => 
                dto.ResellerId == request.ResellerId &&
                dto.CustomerId == request.CustomerId &&
                dto.Items.Count == request.Items.Count)), Times.Once);
        }

        [Test]
        public async Task Create_ServiceThrowsArgumentException_ThrowsException()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                ResellerId = Guid.Empty, // Invalid
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemRequest>()
            };

            _mockOrderService.Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderDto>()))
                .ThrowsAsync(new ArgumentException("Reseller ID cannot be empty"));

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.Create(request));
            _mockOrderService.Verify(s => s.CreateOrderAsync(It.IsAny<CreateOrderDto>()), Times.Once);
        }

        [Test]
        public async Task Create_ServiceThrowsArgumentNullException_ThrowsException()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items = null
            };

            _mockOrderService.Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderDto>()))
                .ThrowsAsync(new ArgumentNullException("orderDto", "Create order DTO cannot be null"));

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _controller.Create(request));
            _mockOrderService.Verify(s => s.CreateOrderAsync(It.IsAny<CreateOrderDto>()), Times.Once);
        }

        #endregion

        #region UpdateOrderStatus Tests

        [Test]
        public async Task UpdateOrderStatus_ValidRequest_ReturnsOkWithMessage()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new UpdateOrderStatusRequest
            {
                StatusName = OrderStatusType.Processing.GetStatusName()
            };

            _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(orderId, request.StatusName))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateOrderStatus(orderId, request);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            
            var responseValue = okResult.Value;
            Assert.IsNotNull(responseValue);
            
            // Verify that OrderId was set from route parameter
            Assert.AreEqual(orderId, request.OrderId);
            
            _mockOrderService.Verify(s => s.UpdateOrderStatusAsync(orderId, request.StatusName), Times.Once);
        }

        [Test]
        public async Task UpdateOrderStatus_OrderNotFound_ReturnsProblemDetails()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new UpdateOrderStatusRequest
            {
                StatusName = OrderStatusType.Processing.GetStatusName()
            };

            _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(orderId, request.StatusName))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateOrderStatus(orderId, request);

            // Assert
            var problemResult = result as ObjectResult;
            Assert.IsNotNull(problemResult);
            Assert.AreEqual(404, problemResult.StatusCode);
            
            var problemDetails = problemResult.Value as ProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual("Order not found", problemDetails.Title);
            Assert.AreEqual($"No order found with ID: {orderId} to update status", problemDetails.Detail);
            Assert.AreEqual(404, problemDetails.Status);
            
            _mockOrderService.Verify(s => s.UpdateOrderStatusAsync(orderId, request.StatusName), Times.Once);
        }

        [Test]
        public async Task UpdateOrderStatus_ServiceThrowsArgumentException_ThrowsException()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new UpdateOrderStatusRequest
            {
                StatusName = "InvalidStatus"
            };

            _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(orderId, request.StatusName))
                .ThrowsAsync(new ArgumentException("Status 'InvalidStatus' not found"));

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.UpdateOrderStatus(orderId, request));
            _mockOrderService.Verify(s => s.UpdateOrderStatusAsync(orderId, request.StatusName), Times.Once);
        }

        [Test]
        public async Task UpdateOrderStatus_ServiceThrowsInvalidOperationException_ThrowsException()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new UpdateOrderStatusRequest
            {
                StatusName = OrderStatusType.Processing.GetStatusName()
            };

            _mockOrderService.Setup(s => s.UpdateOrderStatusAsync(orderId, request.StatusName))
                .ThrowsAsync(new InvalidOperationException("Failed to update order status due to a database error"));

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _controller.UpdateOrderStatus(orderId, request));
            _mockOrderService.Verify(s => s.UpdateOrderStatusAsync(orderId, request.StatusName), Times.Once);
        }

        #endregion

        #region Edge Cases and Additional Tests

        [Test]
        public async Task GetOrdersByStatus_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var statusName = "NonExistentStatus";
            var emptyOrders = new List<OrderSummary>();

            _mockOrderService.Setup(s => s.GetOrdersByStatusAsync(statusName))
                .ReturnsAsync(emptyOrders);

            // Act
            var result = await _controller.GetOrdersByStatus(statusName);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(emptyOrders, okResult.Value);
            _mockOrderService.Verify(s => s.GetOrdersByStatusAsync(statusName), Times.Once);
        }

        [Test]
        public async Task GetProfitByMonth_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var year = 2025; // Future year with no data
            var emptyProfitData = new List<ProfitByMonthDto>();

            _mockOrderService.Setup(s => s.GetProfitByMonthAsync(year, null))
                .ReturnsAsync(emptyProfitData);

            // Act
            var result = await _controller.GetProfitByMonth(year, null);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(emptyProfitData, okResult.Value);
            _mockOrderService.Verify(s => s.GetProfitByMonthAsync(year, null), Times.Once);
        }

        [Test]
        public async Task Get_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var emptyOrders = new List<OrderSummary>();
            _mockOrderService.Setup(s => s.GetOrdersAsync())
                .ReturnsAsync(emptyOrders);

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(emptyOrders, okResult.Value);
            _mockOrderService.Verify(s => s.GetOrdersAsync(), Times.Once);
        }

        #endregion
    }
}
