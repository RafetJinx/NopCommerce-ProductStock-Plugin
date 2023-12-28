using LinqToDB;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Data;

namespace Nop.Services.Catalog
{
    public static class ExtendedProductExtensions
    {
        #region Methods

        /// <summary>
        /// Sorts the elements of a sequence in order according to a product sorting rule
        /// </summary>
        /// <param name="productsQuery">A sequence of products to order</param>
        /// <param name="currentLanguage">Current language</param>
        /// <param name="orderBy">Product sorting rule</param>
        /// <param name="localizedPropertyRepository">Localized property repository</param>
        /// <param name="isChecked">A boolean value that determines the sorting priority based on stock status. When 'true', products in stock are listed first; when 'false', products out of stock are listed first.</param>
        /// <returns>An System.Linq.IOrderedQueryable`1 whose elements are sorted according to a rule.</returns>
        /// <remarks>
        /// If <paramref name="orderBy"/> is set to <c>Position</c> and passed <paramref name="productsQuery"/> is
        /// ordered sorting rule will be skipped
        /// </remarks>
        public static IQueryable<Product> OrderBy(
            this IQueryable<Product> productsQuery,
            IRepository<LocalizedProperty> localizedPropertyRepository,
            Language currentLanguage,
            ProductSortingEnum orderBy,
            bool isChecked)
        {
            if (orderBy == ProductSortingEnum.NameAsc || orderBy == ProductSortingEnum.NameDesc)
            {
                var currentLanguageId = currentLanguage.Id;

                var query =
                    from product in productsQuery
                    join localizedProperty in localizedPropertyRepository.Table on new
                    {
                        product.Id,
                        languageId = currentLanguageId,
                        keyGroup = nameof(Product),
                        key = nameof(Product.Name)
                    }
                        equals new
                        {
                            Id = localizedProperty.EntityId,
                            languageId = localizedProperty.LanguageId,
                            keyGroup = localizedProperty.LocaleKeyGroup,
                            key = localizedProperty.LocaleKey
                        } into localizedProperties
                    from localizedProperty in localizedProperties.DefaultIfEmpty(new LocalizedProperty { LocaleValue = product.Name })
                    select new
                    {
                        sortName = localizedProperty == null ? product.Name : localizedProperty.LocaleValue,
                        product
                    };

                if (orderBy == ProductSortingEnum.NameAsc)
                    productsQuery = from item in query
                                    orderby item.sortName
                                    select item.product;
                else
                    productsQuery = from item in query
                                    orderby item.sortName descending
                                    select item.product;

                var inStockProductsQuery = productsQuery.Where(p => p.StockQuantity > 0);
                var outOfStockProductsQuery = productsQuery.Where(p => p.StockQuantity <= 0);

                return ConcatQueries(inStockProductsQuery, outOfStockProductsQuery, isChecked);
            }

            return Result(productsQuery, orderBy, isChecked);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Orders a sequence of products based on a specified sorting criterion.
        /// </summary>
        /// <param name="query">The sequence of products to be sorted.</param>
        /// <param name="orderBy">The sorting criterion to be applied, as defined in the ProductSortingEnum.</param>
        /// <returns>
        /// An IQueryable<Product> sequence that has been ordered based on the specified sorting criterion. 
        /// </returns>
        private static IQueryable<Product> OrderBy(IQueryable<Product> query, ProductSortingEnum orderBy, bool isChecked)
        {
            // Apply the primary sorting based on the orderBy parameter.
            query = orderBy switch
            {
                ProductSortingEnum.PriceAsc => query.OrderBy(p => p.Price),
                ProductSortingEnum.PriceDesc => query.OrderByDescending(p => p.Price),
                ProductSortingEnum.CreatedOn => query.OrderByDescending(p => p.CreatedOnUtc),
                ProductSortingEnum.Position when query is IOrderedQueryable => query,
                _ => query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            };

            // If isChecked is true, additionally sort by stock status.
            if (isChecked)
            {
                query = query.OrderByDescending(p => p.StockQuantity);
            }

            return query;
        }

        /// <summary>
        /// Combines and orders two product queries based on stock status and a boolean flag.
        /// </summary>
        /// <param name="inStockQuery">The query containing products that are in stock.</param>
        /// <param name="outOfStockQuery">The query containing products that are out of stock.</param>
        /// <param name="isChecked">A boolean flag determining the order of combination. If true, in-stock products are listed before out-of-stock products; if false, the order is reversed.</param>
        /// <returns>An IQueryable<Product> containing the combined list of products, ordered based on stock status as dictated by the isChecked parameter.</returns>
        private static IQueryable<Product> ConcatQueries(IQueryable<Product> inStockQuery, IQueryable<Product> outOfStockQuery, bool isChecked)
        {
            var combinedProductList = isChecked
                ? inStockQuery.ToList().Concat(outOfStockQuery.ToList())
                : outOfStockQuery.ToList().Concat(inStockQuery.ToList());

            return combinedProductList.AsQueryable();
        }

        /// <summary>
        /// Processes a product query by applying a sorting order and further organizing based on stock status.
        /// </summary>
        /// <param name="query">The initial product query to process.</param>
        /// <param name="orderBy">The sorting criterion to apply from ProductSortingEnum.</param>
        /// <param name="isChecked">A boolean flag determining the final order based on stock status. If true, in-stock products are prioritized; if false, out-of-stock products are prioritized.</param>
        /// <returns>An IQueryable<Product> with products sorted first by the specified criterion and then grouped based on their stock status in accordance with the isChecked parameter.</returns>
        private static IQueryable<Product> Result(IQueryable<Product> query, ProductSortingEnum orderBy, bool isChecked)
        {
            var inStockProductsQuery = query.Where(p => p.StockQuantity > 0);
            var outOfStockProductsQuery = query.Where(p => p.StockQuantity <= 0);

            var orderedInStockProductsQuery = OrderBy(inStockProductsQuery, orderBy, isChecked);
            var orderedOutOfStockProductsQuery = OrderBy(outOfStockProductsQuery, orderBy, isChecked);

            var orderedProductsQuery = ConcatQueries(orderedInStockProductsQuery, orderedOutOfStockProductsQuery, isChecked);

            return orderedProductsQuery;
        }

        #endregion
    }
}