using Nop.Core.Domain.Shipping;

namespace Nop.Core.Domain.Orders
{
    public partial class CheckoutSession
    {
        #region Properties

        public int CustomerId { get; set; }

        public int StoreId { get; set; }

        #region Totals

        public SubTotals SubTotal { get; set; }

        public ShippingTotals ShippingTotal { get; set; }

        public PaymentFeeTotals PaymentFeeTotal { get; set; }

        public TaxTotals TaxTotal { get; set; }

        //public OrderTotals OrderTotal { get; set; }

        #endregion

        #region Checkout parameters

        public int BillingAddressId { get; set; }

        public int ShippingAddressId { get; set; }

        public string CheckoutAttributes { get; set; }

        public string DiscountCouponCodes { get; set; }

        public string GiftCardCouponCodes { get; set; }

        public bool UseRewardPoints { get; set; }

        public string SelectedPaymentMethod { get; set; }

        public PickupPoint SelectedPickupPoint { get; set; }

        public ShippingOption SelectedShippingOption { get; set; }

        public List<ShippingOption> OfferedShippingOptions { get; set; }

        #endregion

        #endregion

        #region Nested classes

        public partial class Amount
        {
            public decimal ExcludingTax { get; set; }

            public decimal IncludingTax { get; set; }
        }

        public partial class BaseTotals
        {
            public Amount Amount { get; set; }

            public SortedDictionary<decimal, decimal> TaxRates { get; set; }

            public List<int> AppliedDiscountsIds { get; set; }
        }

        public partial class SubTotals : BaseTotals
        {
            public Amount AmountWithDiscount { get; set; }
            
            public Amount Discount { get; set; }
        }

        public partial class ShippingTotals : BaseTotals
        {
        }

        public partial class PaymentFeeTotals : BaseTotals
        {
        }

        public partial class TaxTotals : BaseTotals
        {
            public Amount AmountWithPaymentFee { get; set; }
        }

        #endregion
    }
}