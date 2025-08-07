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

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

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
