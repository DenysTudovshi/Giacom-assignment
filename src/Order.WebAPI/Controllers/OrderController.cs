using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.Service;
using OrderService.WebAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderService.WebAPI.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Retrieves all orders in the system
        /// </summary>
        /// <returns>A list of all order summaries</returns>
        /// <response code="200">Returns the list of orders</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Creates a new order with the specified items
        /// </summary>
        /// <param name="request">The order creation request containing reseller, customer, and items</param>
        /// <returns>The created order ID and confirmation message</returns>
        /// <response code="201">Order created successfully</response>
        /// <response code="400">Invalid request data or validation errors</response>
        /// <response code="500">Internal server error occurred</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            // Validation happens automatically via FluentValidation pipeline
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Map request to DTO
            var orderDto = new Order.Model.CreateOrderDto
            {
                ResellerId = request.ResellerId,
                CustomerId = request.CustomerId,
                Items = request.Items.Select(item => new Order.Model.CreateOrderItemDto
                {
                    ProductId = item.ProductId,
                    ServiceId = item.ServiceId,
                    Quantity = item.Quantity
                }).ToList()
            };

            var orderId = await _orderService.CreateOrderAsync(orderDto);
            
            return CreatedAtAction(
                nameof(GetOrderById), 
                new { orderId = orderId }, 
                new { OrderId = orderId, Message = "Order created successfully" });
        }

        /// <summary>
        /// Retrieves a specific order by its unique identifier
        /// </summary>
        /// <param name="orderId">The unique identifier of the order</param>
        /// <returns>The detailed order information</returns>
        /// <response code="200">Order found and returned</response>
        /// <response code="404">Order not found</response>
        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                return Ok(order);
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Retrieves all orders with the specified status
        /// </summary>
        /// <param name="statusName">The status name to filter by (e.g., 'Pending', 'Processing', 'Completed')</param>
        /// <returns>A list of orders matching the specified status</returns>
        /// <response code="200">Orders found and returned</response>
        /// <response code="400">Invalid status name provided</response>
        [HttpGet("status/{statusName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetOrdersByStatus([FromRoute] string statusName)
        {
            var request = new GetOrdersByStatusRequest { StatusName = statusName };
            
            // Validation happens automatically via FluentValidation pipeline
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var orders = await _orderService.GetOrdersByStatusAsync(request.StatusName);
            return Ok(orders);
        }

        /// <summary>
        /// Calculates profit by month for all completed orders
        /// </summary>
        /// <param name="year">Optional year filter (e.g., 2024)</param>
        /// <param name="month">Optional month filter (1-12). Requires year to be specified.</param>
        /// <returns>Monthly profit data including total profit and order count</returns>
        /// <response code="200">Profit data calculated and returned</response>
        /// <response code="400">Invalid year or month parameters</response>
        [HttpGet("profit/monthly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfitByMonth([FromQuery] int? year, [FromQuery] int? month)
        {
            var request = new GetProfitByMonthRequest { Year = year, Month = month };
            
            // Validation happens automatically via FluentValidation pipeline
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var profitData = await _orderService.GetProfitByMonthAsync(request.Year, request.Month);
            return Ok(profitData);
        }

        /// <summary>
        /// Updates the status of an existing order
        /// </summary>
        /// <param name="orderId">The unique identifier of the order to update</param>
        /// <param name="request">The status update request containing the new status name</param>
        /// <returns>Confirmation message of the status update</returns>
        /// <response code="200">Order status updated successfully</response>
        /// <response code="400">Invalid request data or status name</response>
        /// <response code="404">Order not found</response>
        /// <response code="500">Internal server error occurred</response>
        [HttpPatch("{orderId}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrderStatus([FromRoute] Guid orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            // Set the OrderId from the route parameter
            request.OrderId = orderId;
            
            // Validation happens automatically via FluentValidation pipeline
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.UpdateOrderStatusAsync(request.OrderId, request.StatusName);
            if (result)
            {
                return Ok(new { Message = $"Order status updated successfully to '{request.StatusName}'" });
            }
            else
            {
                return NotFound(new { Message = "Order not found" });
            }
        }
    }
}
