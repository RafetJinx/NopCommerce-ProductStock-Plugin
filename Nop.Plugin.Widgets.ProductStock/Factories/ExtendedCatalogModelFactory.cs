using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Core.Events;
using Nop.Plugin.Widgets.ProductStock.Models.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Topics;
using Nop.Services.Vendors;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Factories
{
    public partial class ExtendedCatalogModelFactory : CatalogModelFactory, IExtendedCatalogModelFactory
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IStoreContext _storeContext;
        private readonly IExtendedProductService _extendedProductService;

        #endregion

        #region Ctor

        public ExtendedCatalogModelFactory(
            BlogSettings blogSettings,
            CatalogSettings catalogSettings,
            DisplayDefaultMenuItemSettings displayDefaultMenuItemSettings,
            ForumSettings forumSettings,
            ICategoryService categoryService,
            ICategoryTemplateService categoryTemplateService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            IManufacturerTemplateService manufacturerTemplateService,
            INopUrlHelper nopUrlHelper,
            IPictureService pictureService,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IProductTagService productTagService,
            ISearchTermService searchTermService,
            ISpecificationAttributeService specificationAttributeService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            ITopicService topicService,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            VendorSettings vendorSettings,
            IExtendedProductService extendedProductService) : base(blogSettings, catalogSettings, displayDefaultMenuItemSettings, forumSettings, categoryService, categoryTemplateService, currencyService, customerService, eventPublisher, httpContextAccessor, localizationService, manufacturerService, manufacturerTemplateService, nopUrlHelper, pictureService, productModelFactory, productService, productTagService, searchTermService, specificationAttributeService, staticCacheManager, storeContext, topicService, urlRecordService, vendorService, webHelper, workContext, mediaSettings, vendorSettings)
        {
            _catalogSettings = catalogSettings;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            _specificationAttributeService = specificationAttributeService;
            _storeContext = storeContext;
            _extendedProductService = extendedProductService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepares the category products model
        /// </summary>
        /// <param name="category">Category</param>
        /// <param name="command">Model to get the extended catalog products</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the category products model
        /// </returns>
        public virtual async Task<CatalogProductsModel> PrepareCategoryProductsModelAsync(Category category, ExtendedCatalogProductsCommand command)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var model = new CatalogProductsModel
            {
                UseAjaxLoading = _catalogSettings.UseAjaxCatalogProductsLoading
            };

            var currentStore = await _storeContext.GetCurrentStoreAsync();

            //sorting
            await PrepareSortingOptionsAsync(model, command);
            //view mode
            await PrepareViewModesAsync(model, command);
            //page size
            await PreparePageSizeOptionsAsync(model, command, category.AllowCustomersToSelectPageSize,
                category.PageSizeOptions, category.PageSize);

            var categoryIds = new List<int> { category.Id };

            //include subcategories
            if (_catalogSettings.ShowProductsFromSubcategories)
                categoryIds.AddRange(await _categoryService.GetChildCategoryIdsAsync(category.Id, currentStore.Id));

            //price range
            PriceRangeModel selectedPriceRange = null;
            if (_catalogSettings.EnablePriceRangeFiltering && category.PriceRangeFiltering)
            {
                selectedPriceRange = await GetConvertedPriceRangeAsync(command);

                PriceRangeModel availablePriceRange = null;
                if (!category.ManuallyPriceRange)
                {
                    async Task<decimal?> getProductPriceAsync(ProductSortingEnum orderBy)
                    {
                        var products = await _productService.SearchProductsAsync(0, 1,
                            categoryIds: categoryIds,
                            storeId: currentStore.Id,
                            visibleIndividuallyOnly: true,
                            excludeFeaturedProducts: !_catalogSettings.IgnoreFeaturedProducts && !_catalogSettings.IncludeFeaturedProductsInNormalLists,
                            orderBy: orderBy);

                        return products?.FirstOrDefault()?.Price ?? 0;
                    }

                    availablePriceRange = new PriceRangeModel
                    {
                        From = await getProductPriceAsync(ProductSortingEnum.PriceAsc),
                        To = await getProductPriceAsync(ProductSortingEnum.PriceDesc)
                    };
                }
                else
                {
                    availablePriceRange = new PriceRangeModel
                    {
                        From = category.PriceFrom,
                        To = category.PriceTo
                    };
                }

                model.PriceRangeFilter = await PreparePriceRangeFilterAsync(selectedPriceRange, availablePriceRange);
            }

            //filterable options
            var filterableOptions = await _specificationAttributeService
                .GetFiltrableSpecificationAttributeOptionsByCategoryIdAsync(category.Id);

            if (_catalogSettings.EnableSpecificationAttributeFiltering)
            {
                model.SpecificationFilter = await PrepareSpecificationFilterModel(command.SpecificationOptionIds, filterableOptions);
            }

            //filterable manufacturers
            if (_catalogSettings.EnableManufacturerFiltering)
            {
                var manufacturers = await _manufacturerService.GetManufacturersByCategoryIdAsync(category.Id);

                model.ManufacturerFilter = await PrepareManufacturerFilterModel(command.ManufacturerIds, manufacturers);
            }

            var filteredSpecs = command.SpecificationOptionIds is null ? null : filterableOptions.Where(fo => command.SpecificationOptionIds.Contains(fo.Id)).ToList();

            //products
            var products = await _extendedProductService.SearchProductsAsync(
                command.PageNumber - 1,
                command.PageSize,
                categoryIds: categoryIds,
                storeId: currentStore.Id,
                visibleIndividuallyOnly: true,
                excludeFeaturedProducts: !_catalogSettings.IgnoreFeaturedProducts && !_catalogSettings.IncludeFeaturedProductsInNormalLists,
                priceMin: selectedPriceRange?.From,
                priceMax: selectedPriceRange?.To,
                manufacturerIds: command.ManufacturerIds,
                filteredSpecOptions: filteredSpecs,
                orderBy: (ProductSortingEnum)command.OrderBy,
                isBoxChecked: command.IsBoxChecked); // isBoxChecked parameter added 

            var isFiltering = filterableOptions.Any() || selectedPriceRange?.From is not null;

            await PrepareCatalogProductsAsync(model, products, isFiltering);

            return model;
        }

        #endregion
    }
}
