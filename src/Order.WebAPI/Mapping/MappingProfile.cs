using AutoMapper;
using Order.Model;
using OrderService.WebAPI.Models;

namespace OrderService.WebAPI.Mapping
{
    /// <summary>
    /// AutoMapper profile for mapping between API request models and domain DTOs
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Map CreateOrderRequest to CreateOrderDto
            CreateMap<CreateOrderRequest, CreateOrderDto>();
            
            // Map CreateOrderItemRequest to CreateOrderItemDto
            CreateMap<CreateOrderItemRequest, CreateOrderItemDto>();
            
            // Additional mappings can be added here as needed
            // Example: CreateMap<UpdateOrderStatusRequest, UpdateOrderStatusDto>();
        }
    }
}
