using System;

namespace Order.Model
{
    public class ProfitByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal TotalProfit { get; set; }
        public int OrderCount { get; set; }
    }
}
