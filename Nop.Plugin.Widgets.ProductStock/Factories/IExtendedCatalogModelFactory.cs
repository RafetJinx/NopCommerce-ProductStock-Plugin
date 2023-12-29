using Nop.Core.Domain.Catalog;
using Nop.Plugin.Widgets.ProductStock.Models.Catalog;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Factories
{
    public partial interface IExtendedCatalogModelFactory : ICatalogModelFactory
    {
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
        Task<CatalogProductsModel> PrepareCategoryProductsModelAsync(Category category, ExtendedCatalogProductsCommand command);

        #endregion
    }
}