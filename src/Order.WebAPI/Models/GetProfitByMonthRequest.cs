using System;

namespace OrderService.WebAPI.Models
{
    public class GetProfitByMonthRequest
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
    }
}
