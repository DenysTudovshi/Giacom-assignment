namespace Order.Data
{
    public enum CreateOrderResult
    {
        Success,
        ResellerNotFound,
        CustomerNotFound,
        ProductNotFound,
        ServiceNotFound,
        CreationFailed
    }
}
