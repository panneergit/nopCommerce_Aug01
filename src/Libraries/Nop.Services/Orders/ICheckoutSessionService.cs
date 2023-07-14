using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders
{
    public partial interface ICheckoutSessionService
    {
        Task<CheckoutSession> GetCustomerCheckoutSessionAsync(int customerId, int storeId);

        Task InsertCheckoutSessionAsync(CheckoutSession checkoutSession);

        Task UpdateCheckoutSessionAsync(CheckoutSession checkoutSession);

        Task DeleteCheckoutSessionAsync(CheckoutSession checkoutSession);
    }
}