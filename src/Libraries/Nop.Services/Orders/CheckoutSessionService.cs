using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Data;
using Nop.Services.Common;

namespace Nop.Services.Orders
{
    public partial class CheckoutSessionService : ICheckoutSessionService
    {
        #region Fields

        protected readonly IGenericAttributeService _genericAttributeService;
        protected readonly IRepository<Customer> _customerRepository;
        protected readonly TaxSettings _taxSettings;

        protected CheckoutSession _checkoutSession;

        #endregion

        #region Ctor

        public CheckoutSessionService(IGenericAttributeService genericAttributeService,
            IRepository<Customer> customerRepository,
            TaxSettings taxSettings)
        {
            _genericAttributeService = genericAttributeService;
            _customerRepository = customerRepository;
            _taxSettings = taxSettings;
        }

        #endregion

        #region Utilities

        protected virtual async Task<Customer> GetCustomerByIdAsync(int customerId)
        {
            return await _customerRepository.GetByIdAsync(customerId,
                cache => cache.PrepareKeyForShortTermCache(NopEntityCacheDefaults<Customer>.ByIdCacheKey, customerId));
        }

        #endregion

        #region Methods

        public virtual async Task<CheckoutSession> GetCustomerCheckoutSessionAsync(int customerId, int storeId)
        {
            if (_checkoutSession is not null)
                return _checkoutSession;

            var customer = await GetCustomerByIdAsync(customerId)
                ?? throw new NopException("Customer not found");

            var value = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CheckoutSession, storeId);
            var checkoutSession = !string.IsNullOrEmpty(value)
                ? JsonConvert
                    .DeserializeObject<CheckoutSession>(value, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                : null;
            if (checkoutSession is null)
            {
                checkoutSession = new CheckoutSession
                {
                    CustomerId = customerId,
                    BillingAddressId = customer.BillingAddressId ?? 0,
                    ShippingAddressId = customer.ShippingAddressId ?? 0,
                    StoreId = storeId
                };
                await InsertCheckoutSessionAsync(checkoutSession);
            }

            _checkoutSession = checkoutSession;

            return _checkoutSession;
        }

        public virtual async Task InsertCheckoutSessionAsync(CheckoutSession checkoutSession)
        {
            if (checkoutSession is null)
                throw new ArgumentNullException(nameof(checkoutSession));

            var customer = await GetCustomerByIdAsync(checkoutSession.CustomerId);
            if (customer is null)
                return;

            var value = JsonConvert
                .SerializeObject(checkoutSession, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CheckoutSession, value, checkoutSession.StoreId);

            _checkoutSession = checkoutSession;
        }

        public virtual async Task UpdateCheckoutSessionAsync(CheckoutSession checkoutSession)
        {
            if (checkoutSession is null)
                throw new ArgumentNullException(nameof(checkoutSession));

            var customer = await GetCustomerByIdAsync(checkoutSession.CustomerId);
            if (customer is null)
                return;

            var originalValue = await _genericAttributeService
                .GetAttributeAsync<string>(customer, NopCustomerDefaults.CheckoutSession, checkoutSession.StoreId);
            var originalSession = !string.IsNullOrEmpty(originalValue)
                ? JsonConvert
                    .DeserializeObject<CheckoutSession>(originalValue, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                : null;

            if (originalSession.BillingAddressId != checkoutSession.BillingAddressId)
            {
                checkoutSession.TaxTotal = null;
                checkoutSession.PaymentFeeTotal = null;
                if (_taxSettings.TaxBasedOn == TaxBasedOn.BillingAddress)
                {
                    checkoutSession.SubTotal = null;
                    checkoutSession.ShippingTotal = null;
                }
            }

            if (originalSession.ShippingAddressId != checkoutSession.ShippingAddressId)
            {
                checkoutSession.ShippingTotal = null;
                checkoutSession.PaymentFeeTotal = null;
                if (_taxSettings.TaxBasedOn == TaxBasedOn.ShippingAddress || _taxSettings.TaxBasedOnPickupPointAddress)
                {
                    checkoutSession.SubTotal = null;
                    checkoutSession.TaxTotal = null;
                }
            }

            if (!string.Equals(originalSession.CheckoutAttributes, checkoutSession.CheckoutAttributes, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(originalSession.DiscountCouponCodes, checkoutSession.DiscountCouponCodes, StringComparison.InvariantCultureIgnoreCase))
            {
                checkoutSession.SubTotal = null;
                checkoutSession.ShippingTotal = null;
                checkoutSession.TaxTotal = null;
                checkoutSession.PaymentFeeTotal = null;
            }

            if (!string.Equals(originalSession.GiftCardCouponCodes, checkoutSession.GiftCardCouponCodes, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(originalSession.SelectedPaymentMethod, checkoutSession.SelectedPaymentMethod, StringComparison.InvariantCultureIgnoreCase))
            {
                checkoutSession.PaymentFeeTotal = null;
            }

            var originalPickupPoint = JsonConvert
                .SerializeObject(originalSession.SelectedPickupPoint, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var pickupPoint = JsonConvert
                .SerializeObject(checkoutSession.SelectedPickupPoint, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var originalShippingOption = JsonConvert
                .SerializeObject(originalSession.SelectedShippingOption, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var shippingOption = JsonConvert
                .SerializeObject(checkoutSession.SelectedShippingOption, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            if (!string.Equals(originalPickupPoint, pickupPoint, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(originalShippingOption, shippingOption, StringComparison.InvariantCultureIgnoreCase))
            {
                checkoutSession.ShippingTotal = null;
                checkoutSession.TaxTotal = null;
                checkoutSession.PaymentFeeTotal = null;
            }

            var value = JsonConvert.SerializeObject(checkoutSession, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CheckoutSession, value, checkoutSession.StoreId);

            _checkoutSession = checkoutSession;
        }

        public virtual async Task DeleteCheckoutSessionAsync(CheckoutSession checkoutSession)
        {
            if (checkoutSession is null)
                throw new ArgumentNullException(nameof(checkoutSession));

            var customer = await GetCustomerByIdAsync(checkoutSession.CustomerId);
            if (customer is null)
                return;

            await _genericAttributeService
                .SaveAttributeAsync<string>(customer, NopCustomerDefaults.CheckoutSession, null, checkoutSession.StoreId);

            _checkoutSession = null;
        }

        #endregion
    }
}