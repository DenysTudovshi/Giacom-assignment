using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.Service;
using OrderService.WebAPI.Models;
using System;
using System.Threading.Tasks;

namespace OrderService.WebAPI.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IValidator<GetOrdersByStatusRequest> _getOrdersByStatusValidator;
        private readonly IValidator<UpdateOrderStatusRequest> _updateOrderStatusValidator;

        public OrderController(IOrderService orderService, 
            IValidator<GetOrdersByStatusRequest> getOrdersByStatusValidator,
            IValidator<UpdateOrderStatusRequest> updateOrderStatusValidator)
        {
            _orderService = orderService;
            _getOrdersByStatusValidator = getOrdersByStatusValidator;
            _updateOrderStatusValidator = updateOrderStatusValidator;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
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
            
            var validationResult = await _getOrdersByStatusValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
                return BadRequest(ModelState);
            }

            var orders = await _orderService.GetOrdersByStatusAsync(request.StatusName);
            return Ok(orders);
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
            
            var validationResult = await _updateOrderStatusValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
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
