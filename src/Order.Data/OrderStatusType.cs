using System;
using System.ComponentModel;

namespace Order.Data
{
    /// <summary>
    /// Enum representing all possible order status types
    /// </summary>
    public enum OrderStatusType
    {
        [Description("Created")]
        Created,

        [Description("Pending")]
        Pending,

        [Description("Processing")]
        Processing,

        [Description("In Progress")]
        InProgress,

        [Description("Shipped")]
        Shipped,

        [Description("Delivered")]
        Delivered,

        [Description("Completed")]
        Completed,

        [Description("Cancelled")]
        Cancelled,

        [Description("Failed")]
        Failed
    }

    /// <summary>
    /// Extension methods for OrderStatusType enum
    /// </summary>
    public static class OrderStatusTypeExtensions
    {
        /// <summary>
        /// Gets the string representation of the order status
        /// </summary>
        /// <param name="status">The order status enum value</param>
        /// <returns>The string representation of the status</returns>
        public static string GetStatusName(this OrderStatusType status)
        {
            var field = status.GetType().GetField(status.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute?.Description ?? status.ToString();
        }

        /// <summary>
        /// Parses a string to OrderStatusType enum
        /// </summary>
        /// <param name="statusName">The status name string</param>
        /// <returns>The corresponding OrderStatusType enum value</returns>
        /// <exception cref="ArgumentException">Thrown when the status name is not recognized</exception>
        public static OrderStatusType ParseStatusName(string statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                throw new ArgumentException("Status name cannot be null or empty", nameof(statusName));
            }

            foreach (OrderStatusType status in Enum.GetValues(typeof(OrderStatusType)))
            {
                if (string.Equals(status.GetStatusName(), statusName, StringComparison.OrdinalIgnoreCase))
                {
                    return status;
                }
            }

            throw new ArgumentException($"Unknown order status: {statusName}", nameof(statusName));
        }

        /// <summary>
        /// Tries to parse a string to OrderStatusType enum
        /// </summary>
        /// <param name="statusName">The status name string</param>
        /// <param name="status">The output OrderStatusType if parsing succeeds</param>
        /// <returns>True if parsing succeeds, false otherwise</returns>
        public static bool TryParseStatusName(string statusName, out OrderStatusType status)
        {
            status = default;

            if (string.IsNullOrWhiteSpace(statusName))
            {
                return false;
            }

            foreach (OrderStatusType statusValue in Enum.GetValues(typeof(OrderStatusType)))
            {
                if (string.Equals(statusValue.GetStatusName(), statusName, StringComparison.OrdinalIgnoreCase))
                {
                    status = statusValue;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all valid status names as strings
        /// </summary>
        /// <returns>Array of all valid status names</returns>
        public static string[] GetAllStatusNames()
        {
            var statusNames = new string[Enum.GetValues(typeof(OrderStatusType)).Length];
            var values = Enum.GetValues(typeof(OrderStatusType));
            
            for (int i = 0; i < values.Length; i++)
            {
                statusNames[i] = ((OrderStatusType)values.GetValue(i)).GetStatusName();
            }

            return statusNames;
        }
    }
}
