using FluentMigrator;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Migrations.UpgradeTo470
{
    [NopUpdateMigration("2023-01-01 00:00:00", "4.70.0", UpdateMigrationType.Data)]
    public class DataMigration : Migration
    {
        private readonly INopDataProvider _dataProvider;

        public DataMigration(INopDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            //#5312 new activity log type
            var activityLogTypeTable = _dataProvider.GetTable<ActivityLogType>();

            if (!activityLogTypeTable.Any(alt =>
                    string.Compare(alt.SystemKeyword, "ImportCustomers", StringComparison.InvariantCultureIgnoreCase) ==
                    0))
                _dataProvider.InsertEntity(
                    new ActivityLogType
                    {
                        SystemKeyword = "ImportCustomers", Enabled = true, Name = "Customers were imported"
                    }
                );

            //6660 new activity log type for update plugin
            if (!activityLogTypeTable.Any(alt =>
                    string.Compare(alt.SystemKeyword, "UpdatePlugin", StringComparison.InvariantCultureIgnoreCase) ==
                    0))
                _dataProvider.InsertEntity(
                    new ActivityLogType { SystemKeyword = "UpdatePlugin", Enabled = true, Name = "Update a plugin" }
                );

            //1934
            int pageIndex;
            var pageSize = 500;
            var productAttributeCombinationTableName = nameof(ProductAttributeCombination);

            var pac = Schema.Table(productAttributeCombinationTableName);
            var columnName = "PictureId";

            if (pac.Column(columnName).Exists())
            {
                #pragma warning disable CS0618
                var combinationQuery =
                    from c in _dataProvider.GetTable<ProductAttributeCombination>()
                    join p in _dataProvider.GetTable<Picture>() on c.PictureId equals p.Id
                    select c;
                #pragma warning restore CS0618

                pageIndex = 0;

                while (true)
                {
                    var combinations = combinationQuery.ToPagedListAsync(pageIndex, pageSize).Result;

                    if (!combinations.Any())
                        break;

                    #pragma warning disable CS0618
                    foreach (var combination in combinations)
                    {
                        if (!combination.PictureId.HasValue)
                            continue;

                        _dataProvider.InsertEntity(new ProductAttributeCombinationPicture
                        {
                            PictureId = combination.PictureId.Value, ProductAttributeCombinationId = combination.Id
                        });

                        combination.PictureId = null;
                    }
                    #pragma warning restore CS0618

                    _dataProvider.UpdateEntitiesAsync(combinations);

                    pageIndex++;
                }
            }

            var productAttributeValueTableName = nameof(ProductAttributeValue);
            var pav = Schema.Table(productAttributeValueTableName);

            if (pav.Column(columnName).Exists())
            {
                #pragma warning disable CS0618
                var valueQuery =
                    from c in _dataProvider.GetTable<ProductAttributeValue>()
                    join p in _dataProvider.GetTable<Picture>() on c.PictureId equals p.Id
                    select c;
                #pragma warning restore CS0618

                pageIndex = 0;

                while (true)
                {
                    var values = valueQuery.ToPagedListAsync(pageIndex, pageSize).Result;

                    if (!values.Any())
                        break;

                    #pragma warning disable CS0618
                    foreach (var value in values)
                    {
                        if (!value.PictureId.HasValue)
                            continue;

                        _dataProvider.InsertEntity(new ProductAttributeValuePicture
                        {
                            PictureId = value.PictureId.Value, ProductAttributeValueId = value.Id
                        });

                        value.PictureId = null;
                    }
                    #pragma warning restore CS0618

                    _dataProvider.UpdateEntitiesAsync(values);

                    pageIndex++;
                }
            }

            //#
            pageIndex = 0;
            var customerTable = _dataProvider.GetTable<Customer>();
            var genericAttributeTable = _dataProvider.GetTable<GenericAttribute>();
            var attributeKeys = new[] { "CheckoutAttributes", "DiscountCouponCode", "GiftCardCouponCodes", "UseRewardPointsDuringCheckout", 
                "SelectedPaymentMethod", "SelectedShippingOption", "SelectedPickupPoint", "OfferedShippingOptions" };
            while (true)
            {
                var customers = customerTable.Where(customer => !customer.Deleted).ToPagedListAsync(pageIndex++, pageSize).Result;
                if (!customers.Any())
                    break;

                var customerIds = customers.Select(customer => customer.Id).ToList();
                var genericAttributes = genericAttributeTable
                    .Where(attribute => attribute.KeyGroup == nameof(Customer) && customerIds.Contains(attribute.EntityId) && attributeKeys.Contains(attribute.Key))
                    .ToList();
                if (!genericAttributes.Any())
                    continue;

                var newAttributes = new List<GenericAttribute>();

                foreach (var customer in customers)
                {
                    var customerAttributes = genericAttributes.Where(attribute => attribute.EntityId == customer.Id).ToList();
                    if (!customerAttributes.Any())
                        continue;

                    var storeIds = customerAttributes.Select(attribute => attribute.StoreId).Distinct().ToList();
                    foreach (var storeId in storeIds)
                    {
                        var attributes = customerAttributes.Where(attribute => attribute.StoreId == storeId).ToList();
                        if (!attributes.Any())
                            continue;

                        var useRewardPoints = attributes.FirstOrDefault(attribute => attribute.Key == "UseRewardPointsDuringCheckout")?.Value;
                        var pickupPoint = attributes.FirstOrDefault(attribute => attribute.Key == "SelectedPickupPoint")?.Value;
                        var shippingOption = attributes.FirstOrDefault(attribute => attribute.Key == "SelectedShippingOption")?.Value;
                        var shippingOptions = attributes.FirstOrDefault(attribute => attribute.Key == "OfferedShippingOptions")?.Value;
                        var checkoutSession = new CheckoutSession
                        {
                            CustomerId = customer.Id,
                            StoreId = storeId,
                            BillingAddressId = customer.BillingAddressId ?? 0,
                            ShippingAddressId = customer.ShippingAddressId ?? 0,
                            CheckoutAttributes = attributes.FirstOrDefault(attribute => attribute.Key == "CheckoutAttributes")?.Value,
                            DiscountCouponCodes = attributes.FirstOrDefault(attribute => attribute.Key == "DiscountCouponCode")?.Value,
                            GiftCardCouponCodes = attributes.FirstOrDefault(attribute => attribute.Key == "GiftCardCouponCodes")?.Value,
                            SelectedPaymentMethod = attributes.FirstOrDefault(attribute => attribute.Key == "SelectedPaymentMethod")?.Value,
                            UseRewardPoints = string.Equals(useRewardPoints, true.ToString(), StringComparison.InvariantCultureIgnoreCase),
                            SelectedPickupPoint = CommonHelper.To<PickupPoint>(pickupPoint),
                            SelectedShippingOption = CommonHelper.To<ShippingOption>(shippingOption),
                            OfferedShippingOptions = CommonHelper.To<List<ShippingOption>>(shippingOptions)
                        };
                        var value = JsonConvert.SerializeObject(checkoutSession, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        newAttributes.Add(new() 
                        {
                            Key = NopCustomerDefaults.CheckoutSession,
                            KeyGroup = nameof(Customer),
                            EntityId = customer.Id,
                            StoreId = storeId,
                            Value = value,
                            CreatedOrUpdatedDateUTC = DateTime.UtcNow
                        });
                    }
                }

                _dataProvider.BulkInsertEntities(newAttributes);
                _dataProvider.BulkDeleteEntities(genericAttributes);
            }
        }

        public override void Down()
        {
            //add the downgrade logic if necessary 
        }
    }
}
