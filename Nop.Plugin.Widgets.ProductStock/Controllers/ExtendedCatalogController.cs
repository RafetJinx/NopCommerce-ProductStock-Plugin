using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Widgets.ProductStock.Models.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Widgets.ProductStock.Controllers
{
    public partial class ExtendedCatalogController : CatalogController
    {
        #region Fields

        private readonly IAclService _aclService;
        private readonly ICategoryService _categoryService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IExtendedCatalogModelFactory _extendedCatalogModelFactory;

        #endregion

        #region Ctor
        public ExtendedCatalogController(
            CatalogSettings catalogSettings,
            IAclService aclService,
            ICatalogModelFactory catalogModelFactory,
            ICategoryService categoryService,
            ICustomerActivityService customerActivityService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            INopUrlHelper nopUrlHelper,
            IPermissionService permissionService,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IProductTagService productTagService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlRecordService urlRecordService,
            IVendorService vendorService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            VendorSettings vendorSettings,
            IExtendedCatalogModelFactory extendedCatalogModelFactory)
            : base(catalogSettings, aclService, catalogModelFactory, categoryService, customerActivityService, genericAttributeService, localizationService, manufacturerService, nopUrlHelper, permissionService, productModelFactory, productService, productTagService, storeContext, storeMappingService, urlRecordService, vendorService, webHelper, workContext, mediaSettings, vendorSettings)
        {
            _aclService = aclService;
            _categoryService = categoryService;
            _permissionService = permissionService;
            _storeMappingService = storeMappingService;
            _extendedCatalogModelFactory = extendedCatalogModelFactory;
        }

        #endregion

        #region Methods

        public async override Task<IActionResult> GetCategoryProducts(int categoryId, CatalogProductsCommand command)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (!await CheckCategoryAvailabilityAsync(category))
                return NotFound();

            ExtendedCatalogProductsCommand extendedCatalogProductsCommand = new ExtendedCatalogProductsCommand(command);

            bool isBoxChecked = false;
            if (Request.Query.TryGetValue("isBoxChecked", out var value) && bool.TryParse(value, out var parsedValue))
            {
                isBoxChecked = parsedValue;
            }

            extendedCatalogProductsCommand.IsBoxChecked = isBoxChecked;

            var model = await _extendedCatalogModelFactory.PrepareCategoryProductsModelAsync(category, extendedCatalogProductsCommand);

            return PartialView("_ProductsInGridOrLines", model);
        }

        #endregion

        #region Utilities

        private async Task<bool> CheckCategoryAvailabilityAsync(Category category)
        {
            if (category is null)
                return false;

            var isAvailable = true;

            if (category.Deleted)
                isAvailable = false;

            var notAvailable =
                //published?
                !category.Published ||
                //ACL (access control list) 
                !await _aclService.AuthorizeAsync(category) ||
                //Store mapping
                !await _storeMappingService.AuthorizeAsync(category);
            //Check whether the current user has a "Manage categories" permission (usually a store owner)
            //We should allows him (her) to use "Preview" functionality
            var hasAdminAccess = await _permissionService.AuthorizeAsync(StandardPermissionProvider.AccessAdminPanel) && await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCategories);
            if (notAvailable && !hasAdminAccess)
                isAvailable = false;

            return isAvailable;
        }

        #endregion
    }
}
