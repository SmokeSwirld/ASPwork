using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace ASPwork.Models.Shop
{
    public class ShopIndexFormModel
    {
        [FromForm(Name = "productName")]  // <input ... name="productName" ...
        public String Title { get; set; }

        [FromForm(Name = "productDescription")]
        public String? Description { get; set; }

        [FromForm(Name = "productGroup")]
        public Guid ProductGroupId { get; set; }

        [FromForm(Name = "productPrice")]
        public float Price { get; set; }

        [FromForm(Name = "productImage")]
        public IFormFile? Image { get; set; }
    }
}
