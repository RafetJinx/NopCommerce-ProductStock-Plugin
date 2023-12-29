using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;

namespace Nop.Services.Catalog
{
    public partial class ExtendedProductService : ProductService, IExtendedProductService
    {
        #region Ctor

        public ExtendedProductService(CatalogSettings catalogSettings,
            CommonSettings commonSettings,
            IAclService aclService,
            ICustomerService customerService,
            IDateRangeService dateRangeService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IRepository<Category> categoryRepository,
            IRepository<CrossSellProduct> crossSellProductRepository,
            IRepository<DiscountProductMapping> discountProductMappingRepository,
            IRepository<LocalizedProperty> localizedPropertyRepository,
            IRepository<Manufacturer> manufacturerRepository,
            IRepository<Product> productRepository,
            IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<ProductManufacturer> productManufacturerRepository,
            IRepository<ProductPicture> productPictureRepository,
            IRepository<ProductProductTagMapping> productTagMappingRepository,
            IRepository<ProductReview> productReviewRepository,
            IRepository<ProductReviewHelpfulness> productReviewHelpfulnessRepository,
            IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
            IRepository<ProductTag> productTagRepository,
            IRepository<ProductVideo> productVideoRepository,
            IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
            IRepository<RelatedProduct> relatedProductRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
            IRepository<TierPrice> tierPriceRepository,
            ISearchPluginManager searchPluginManager,
            IStaticCacheManager staticCacheManager,
            IStoreService storeService,
            IStoreMappingService storeMappingService,
            IWorkContext workContext,
            LocalizationSettings localizationSettings) : base(catalogSettings, commonSettings, aclService, customerService, dateRangeService, languageService, localizationService, productAttributeParser, productAttributeService, categoryRepository, crossSellProductRepository, discountProductMappingRepository, localizedPropertyRepository, manufacturerRepository, productRepository, productAttributeCombinationRepository, productAttributeMappingRepository, productCategoryRepository, productManufacturerRepository, productPictureRepository, productTagMappingRepository, productReviewRepository, productReviewHelpfulnessRepository, productSpecificationAttributeRepository, productTagRepository, productVideoRepository, productWarehouseInventoryRepository, relatedProductRepository, shipmentRepository, stockQuantityHistoryRepository, tierPriceRepository, searchPluginManager, staticCacheManager, storeService, storeMappingService, workContext, localizationSettings)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Search products
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="categoryIds">Category identifiers</param>
        /// <param name="manufacturerIds">Manufacturer identifiers</param>
        /// <param name="storeId">Store identifier; 0 to load all records</param>
        /// <param name="vendorId">Vendor identifier; 0 to load all records</param>
        /// <param name="warehouseId">Warehouse identifier; 0 to load all records</param>
        /// <param name="productType">Product type; 0 to load all records</param>
        /// <param name="visibleIndividuallyOnly">A values indicating whether to load only products marked as "visible individually"; "false" to load all records; "true" to load "visible individually" only</param>
        /// <param name="excludeFeaturedProducts">A value indicating whether loaded products are marked as featured (relates only to categories and manufacturers); "false" (by default) to load all records; "true" to exclude featured products from results</param>
        /// <param name="priceMin">Minimum price; null to load all records</param>
        /// <param name="priceMax">Maximum price; null to load all records</param>
        /// <param name="productTagId">Product tag identifier; 0 to load all records</param>
        /// <param name="keywords">Keywords</param>
        /// <param name="searchDescriptions">A value indicating whether to search by a specified "keyword" in product descriptions</param>
        /// <param name="searchManufacturerPartNumber">A value indicating whether to search by a specified "keyword" in manufacturer part number</param>
        /// <param name="searchSku">A value indicating whether to search by a specified "keyword" in product SKU</param>
        /// <param name="searchProductTags">A value indicating whether to search by a specified "keyword" in product tags</param>
        /// <param name="languageId">Language identifier (search for text searching)</param>
        /// <param name="filteredSpecOptions">Specification options list to filter products; null to load all records</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="overridePublished">
        /// <param name="isBoxChecked">A boolean variable that determines the grouping order of products based on stock availability in search results. When set to 'true', products are grouped first by those in stock followed by those out of stock. When set to 'false', the order is reversed, starting with products out of stock followed by those in stock.</param>
        /// null - process "Published" property according to "showHidden" parameter
        /// true - load only "Published" products
        /// false - load only "Unpublished" products
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the products
        /// </returns>
        public async Task<IPagedList<Product>> SearchProductsAsync(int pageIndex = 0, int pageSize = int.MaxValue, IList<int> categoryIds = null, IList<int> manufacturerIds = null, int storeId = 0, int vendorId = 0, int warehouseId = 0, ProductType? productType = null, bool visibleIndividuallyOnly = false, bool excludeFeaturedProducts = false, decimal? priceMin = null, decimal? priceMax = null, int productTagId = 0, string keywords = null, bool searchDescriptions = false, bool searchManufacturerPartNumber = true, bool searchSku = true, bool searchProductTags = false, int languageId = 0, IList<SpecificationAttributeOption> filteredSpecOptions = null, ProductSortingEnum orderBy = ProductSortingEnum.Position, bool showHidden = false, bool? overridePublished = null, bool isBoxChecked = false)
        {
            //some databases don't support int.MaxValue
            if (pageSize == int.MaxValue)
                pageSize = int.MaxValue - 1;

            var productsQuery = _productRepository.Table;

            if (!showHidden)
                productsQuery = productsQuery.Where(p => p.Published);
            else if (overridePublished.HasValue)
                productsQuery = productsQuery.Where(p => p.Published == overridePublished.Value);

            var customer = await _workContext.GetCurrentCustomerAsync();

            if (!showHidden || storeId > 0)
            {
                //apply store mapping constraints
                productsQuery = await _storeMappingService.ApplyStoreMapping(productsQuery, storeId);
            }

            if (!showHidden)
            {
                //apply ACL constraints
                productsQuery = await _aclService.ApplyAcl(productsQuery, customer);
            }

            productsQuery =
                from p in productsQuery
                where !p.Deleted &&
                    (!visibleIndividuallyOnly || p.VisibleIndividually) &&
                    (vendorId == 0 || p.VendorId == vendorId) &&
                    (
                        warehouseId == 0 ||
                        (
                            !p.UseMultipleWarehouses ? p.WarehouseId == warehouseId :
                                _productWarehouseInventoryRepository.Table.Any(pwi => pwi.WarehouseId == warehouseId && pwi.ProductId == p.Id)
                        )
                    ) &&
                    (productType == null || p.ProductTypeId == (int)productType) &&
                    (showHidden ||
                            (DateTime.UtcNow >= (p.AvailableStartDateTimeUtc ?? DateTime.MinValue) &&
                            DateTime.UtcNow <= (p.AvailableEndDateTimeUtc ?? DateTime.MaxValue))
                    ) &&
                    (priceMin == null || p.Price >= priceMin) &&
                    (priceMax == null || p.Price <= priceMax)
                select p;

            if (!string.IsNullOrEmpty(keywords))
            {
                var langs = await _languageService.GetAllLanguagesAsync(showHidden: true);

                //Set a flag which will to points need to search in localized properties. If showHidden doesn't set to true should be at least two published languages.
                var searchLocalizedValue = languageId > 0 && langs.Count >= 2 && (showHidden || langs.Count(l => l.Published) >= 2);
                IQueryable<int> productsByKeywords;

                var activeSearchProvider = await _searchPluginManager.LoadPrimaryPluginAsync(customer, storeId);

                if (activeSearchProvider is not null)
                {
                    productsByKeywords = (await activeSearchProvider.SearchProductsAsync(keywords, searchLocalizedValue)).AsQueryable();
                }
                else
                {
                    productsByKeywords =
                        from p in _productRepository.Table
                        where p.Name.Contains(keywords) ||
                            (searchDescriptions &&
                                (p.ShortDescription.Contains(keywords) || p.FullDescription.Contains(keywords))) ||
                            (searchManufacturerPartNumber && p.ManufacturerPartNumber == keywords) ||
                            (searchSku && p.Sku == keywords)
                        select p.Id;

                    if (searchLocalizedValue)
                    {
                        productsByKeywords = productsByKeywords.Union(
                            from lp in _localizedPropertyRepository.Table
                            let checkName = lp.LocaleKey == nameof(Product.Name) &&
                                            lp.LocaleValue.Contains(keywords)
                            let checkShortDesc = searchDescriptions &&
                                            lp.LocaleKey == nameof(Product.ShortDescription) &&
                                            lp.LocaleValue.Contains(keywords)
                            where
                                lp.LocaleKeyGroup == nameof(Product) && lp.LanguageId == languageId && (checkName || checkShortDesc)

                            select lp.EntityId);
                    }
                }

                //search by SKU for ProductAttributeCombination
                if (searchSku)
                {
                    productsByKeywords = productsByKeywords.Union(
                        from pac in _productAttributeCombinationRepository.Table
                        where pac.Sku == keywords
                        select pac.ProductId);
                }

                //search by category name if admin allows
                if (_catalogSettings.AllowCustomersToSearchWithCategoryName)
                {
                    var categoryQuery = _categoryRepository.Table;

                    if (!showHidden)
                        categoryQuery = categoryQuery.Where(p => p.Published);
                    else if (overridePublished.HasValue)
                        categoryQuery = categoryQuery.Where(p => p.Published == overridePublished.Value);

                    if (!showHidden || storeId > 0)
                        categoryQuery = await _storeMappingService.ApplyStoreMapping(categoryQuery, storeId);

                    if (!showHidden)
                        categoryQuery = await _aclService.ApplyAcl(categoryQuery, customer);

                    productsByKeywords = productsByKeywords.Union(
                        from pc in _productCategoryRepository.Table
                        join c in categoryQuery on pc.CategoryId equals c.Id
                        where c.Name.Contains(keywords) && !c.Deleted
                        select pc.ProductId
                    );

                    if (searchLocalizedValue)
                    {
                        productsByKeywords = productsByKeywords.Union(
                        from pc in _productCategoryRepository.Table
                        join lp in _localizedPropertyRepository.Table on pc.CategoryId equals lp.EntityId
                        where lp.LocaleKeyGroup == nameof(Category) &&
                              lp.LocaleKey == nameof(Category.Name) &&
                              lp.LocaleValue.Contains(keywords) &&
                              lp.LanguageId == languageId
                        select pc.ProductId);
                    }
                }

                //search by manufacturer name if admin allows
                if (_catalogSettings.AllowCustomersToSearchWithManufacturerName)
                {
                    var manufacturerQuery = _manufacturerRepository.Table;

                    if (!showHidden)
                        manufacturerQuery = manufacturerQuery.Where(p => p.Published);
                    else if (overridePublished.HasValue)
                        manufacturerQuery = manufacturerQuery.Where(p => p.Published == overridePublished.Value);

                    if (!showHidden || storeId > 0)
                        manufacturerQuery = await _storeMappingService.ApplyStoreMapping(manufacturerQuery, storeId);

                    if (!showHidden)
                        manufacturerQuery = await _aclService.ApplyAcl(manufacturerQuery, customer);

                    productsByKeywords = productsByKeywords.Union(
                        from pm in _productManufacturerRepository.Table
                        join m in manufacturerQuery on pm.ManufacturerId equals m.Id
                        where m.Name.Contains(keywords) && !m.Deleted
                        select pm.ProductId
                    );

                    if (searchLocalizedValue)
                    {
                        productsByKeywords = productsByKeywords.Union(
                        from pm in _productManufacturerRepository.Table
                        join lp in _localizedPropertyRepository.Table on pm.ManufacturerId equals lp.EntityId
                        where lp.LocaleKeyGroup == nameof(Manufacturer) &&
                              lp.LocaleKey == nameof(Manufacturer.Name) &&
                              lp.LocaleValue.Contains(keywords) &&
                              lp.LanguageId == languageId
                        select pm.ProductId);
                    }
                }

                if (searchProductTags)
                {
                    productsByKeywords = productsByKeywords.Union(
                        from pptm in _productTagMappingRepository.Table
                        join pt in _productTagRepository.Table on pptm.ProductTagId equals pt.Id
                        where pt.Name.Contains(keywords)
                        select pptm.ProductId
                    );

                    if (searchLocalizedValue)
                    {
                        productsByKeywords = productsByKeywords.Union(
                        from pptm in _productTagMappingRepository.Table
                        join lp in _localizedPropertyRepository.Table on pptm.ProductTagId equals lp.EntityId
                        where lp.LocaleKeyGroup == nameof(ProductTag) &&
                              lp.LocaleKey == nameof(ProductTag.Name) &&
                              lp.LocaleValue.Contains(keywords) &&
                              lp.LanguageId == languageId
                        select pptm.ProductId);
                    }
                }

                productsQuery =
                    from p in productsQuery
                    join pbk in productsByKeywords on p.Id equals pbk
                    select p;
            }

            if (categoryIds is not null)
            {
                if (categoryIds.Contains(0))
                    categoryIds.Remove(0);

                if (categoryIds.Any())
                {
                    var productCategoryQuery =
                        from pc in _productCategoryRepository.Table
                        where (!excludeFeaturedProducts || !pc.IsFeaturedProduct) &&
                            categoryIds.Contains(pc.CategoryId)
                        group pc by pc.ProductId into pc
                        select new
                        {
                            ProductId = pc.Key,
                            DisplayOrder = pc.First().DisplayOrder
                        };

                    productsQuery =
                        from p in productsQuery
                        join pc in productCategoryQuery on p.Id equals pc.ProductId
                        orderby pc.DisplayOrder, p.Name
                        select p;
                }
            }

            if (manufacturerIds is not null)
            {
                if (manufacturerIds.Contains(0))
                    manufacturerIds.Remove(0);

                if (manufacturerIds.Any())
                {
                    var productManufacturerQuery =
                        from pm in _productManufacturerRepository.Table
                        where (!excludeFeaturedProducts || !pm.IsFeaturedProduct) &&
                            manufacturerIds.Contains(pm.ManufacturerId)
                        group pm by pm.ProductId into pm
                        select new
                        {
                            ProductId = pm.Key,
                            DisplayOrder = pm.First().DisplayOrder
                        };

                    productsQuery =
                        from p in productsQuery
                        join pm in productManufacturerQuery on p.Id equals pm.ProductId
                        orderby pm.DisplayOrder, p.Name
                        select p;
                }
            }

            if (productTagId > 0)
            {
                productsQuery =
                    from p in productsQuery
                    join ptm in _productTagMappingRepository.Table on p.Id equals ptm.ProductId
                    where ptm.ProductTagId == productTagId
                    select p;
            }

            if (filteredSpecOptions?.Count > 0)
            {
                var specificationAttributeIds = filteredSpecOptions
                    .Select(sao => sao.SpecificationAttributeId)
                    .Distinct();

                foreach (var specificationAttributeId in specificationAttributeIds)
                {
                    var optionIdsBySpecificationAttribute = filteredSpecOptions
                        .Where(o => o.SpecificationAttributeId == specificationAttributeId)
                        .Select(o => o.Id);

                    var productSpecificationQuery =
                        from psa in _productSpecificationAttributeRepository.Table
                        where psa.AllowFiltering && optionIdsBySpecificationAttribute.Contains(psa.SpecificationAttributeOptionId)
                        select psa;

                    productsQuery =
                        from p in productsQuery
                        where productSpecificationQuery.Any(pc => pc.ProductId == p.Id)
                        select p;
                }
            }

            return await productsQuery.OrderBy(_localizedPropertyRepository, await _workContext.GetWorkingLanguageAsync(), orderBy, isBoxChecked).ToPagedListAsync(pageIndex, pageSize);
        }

        #endregion
    }
}
