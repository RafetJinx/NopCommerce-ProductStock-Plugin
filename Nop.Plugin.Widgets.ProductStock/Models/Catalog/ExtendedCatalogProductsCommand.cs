using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Widgets.ProductStock.Models.Catalog
{
    /// <summary>
    /// Represents a model to get the extended catalog products (inherits from <see cref="CatalogProductsCommand"/>)
    /// </summary>
    public partial record ExtendedCatalogProductsCommand : CatalogProductsCommand
    {
        #region Fields

        /// <summary>
        /// Is Box Checked for product sorting (stock quantity)
        /// </summary>
        public bool IsBoxChecked { get; set; }

        #endregion

        #region Ctor

        public ExtendedCatalogProductsCommand(CatalogProductsCommand command) : base()
        {
            if (command != null)
            {
                // Copy properties from the base class
                this.Price = command.Price;
                this.OrderBy = command.OrderBy;
                this.ViewMode = command.ViewMode;

                // Copy BasePageableModel properties
                this.PageNumber = command.PageNumber;
                this.PageSize = command.PageSize;
                this.TotalItems = command.TotalItems;
                this.TotalPages = command.TotalPages;
                this.FirstItem = command.FirstItem;
                this.LastItem = command.LastItem;
                this.HasPreviousPage = command.HasPreviousPage;
                this.HasNextPage = command.HasNextPage;

                // Initialize SpecificationOptionIds with an empty list if it's null
                this.SpecificationOptionIds = command.SpecificationOptionIds != null
                    ? new List<int>(command.SpecificationOptionIds)
                    : new List<int>();

                // Initialize ManufacturerIds with an empty list if it's null
                this.ManufacturerIds = command.ManufacturerIds != null
                    ? new List<int>(command.ManufacturerIds)
                    : new List<int>();
            }
        }

        #endregion
    }
}
