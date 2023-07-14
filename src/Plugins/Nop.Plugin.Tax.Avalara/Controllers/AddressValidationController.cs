using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Tax.Avalara.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AddressValidationController : BaseController
    {
        #region Fields

        protected readonly IAddressService _addressService;
        protected readonly ICheckoutSessionService _checkoutSessionService;
        protected readonly ICustomerService _customerService;
        protected readonly IStoreContext _storeContext;
        protected readonly IWorkContext _workContext;
        protected readonly TaxSettings _taxSettings;

        #endregion

        #region Ctor

        public AddressValidationController(IAddressService addressService,
            CheckoutSessionService checkoutSessionService,
            ICustomerService customerService,
            IStoreContext storeContext,
            IWorkContext workContext,
            TaxSettings taxSettings)
        {
            _addressService = addressService;
            _checkoutSessionService = checkoutSessionService;
            _customerService = customerService;
            _storeContext = storeContext;
            _workContext = workContext;
            _taxSettings = taxSettings;
        }

        #endregion

        #region Methods

        [HttpPost]
        public async Task<IActionResult> UseValidatedAddress(int addressId, bool isNewAddress)
        {
            //try to get an address by the passed identifier
            if (await _addressService.GetAddressByIdAsync(addressId) is Address address)
            {
                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var checkoutSession = await _checkoutSessionService.GetCustomerCheckoutSessionAsync(customer.Id, store.Id);

                //add address to customer collection if it's a new
                if (isNewAddress)
                    await _customerService.InsertCustomerAddressAsync(customer, address);

                //and update appropriate customer address
                if (_taxSettings.TaxBasedOn == TaxBasedOn.BillingAddress)
                {
                    customer.BillingAddressId = address.Id;
                    checkoutSession.BillingAddressId = address.Id;
                }
                if (_taxSettings.TaxBasedOn == TaxBasedOn.ShippingAddress)
                {
                    customer.ShippingAddressId = address.Id;
                    checkoutSession.ShippingAddressId = address.Id;
                }
                await _customerService.UpdateCustomerAsync(customer);
                await _checkoutSessionService.UpdateCheckoutSessionAsync(checkoutSession);
            }

            //nothing to return
            return Content(string.Empty);
        }

        #endregion
    }
}