using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Widgets.ProductStock.Components
{
    public class ProductStockViewComponent : NopViewComponent
    {
        #region Methods

        /// <summary>
        /// Invoke the widget view component
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="additionalData">Additional parameters</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the view component result
        /// </returns>
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            var model = new CatalogProductsModel();

            if (additionalData is CategoryModel categoryModel)
            {
                var catalogProductsModel = categoryModel.CatalogProductsModel;
                if (catalogProductsModel != null)
                {
                    model = catalogProductsModel;
                }
            }

            return await Task.FromResult(View("~/Plugins/Widgets.ProductStock/Views/ProductStock.cshtml", model));
        }

        #endregion
    }
}
