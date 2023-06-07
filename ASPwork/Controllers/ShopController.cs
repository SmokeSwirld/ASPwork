using ASPwork.Data;
using ASPwork.Models.Shop;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using ASPwork.Models.User;
using ASPwork.Services.Hash;



namespace ASPwork.Controllers
{
    public class ShopController : Controller
    {
        private readonly DataContext _dataContext;

        public ShopController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public IActionResult Index()
        {
            ShopIndexViewModel model = new()
            {
                ProductGroups = _dataContext.ProductGroups
                    .Where(g => g.DeleteDt == null).ToList(),
                Products = _dataContext.Products
                    .Where(p => p.DeleteDt == null).ToList(),
            };
            if (HttpContext.Session.Keys.Contains("AddMessage"))
            {
                model.AddMessage = HttpContext.Session.GetString("AddMessage");
                HttpContext.Session.Remove("AddMessage");
            }
            return View(model);
        }

        [HttpPost]
        public RedirectToActionResult AddProduct(ShopIndexFormModel model)
        {
            // перевіряємо модель, зберігаємо файл, додаємо до БД, повертаємо повідомлення
            String errorMessage = _ValidateModel(model);
            if (errorMessage != String.Empty)
            {
                // є помилка валідації
                HttpContext.Session.SetString("AddMessage", errorMessage);
            }
            else
            {
                // перевірка успішна
                HttpContext.Session.SetString("AddMessage", "Додано успішно");
            }
            return RedirectToAction(nameof(Index));
        }
        private bool IsProductNameUnique(string productName)
        {
            bool isUnique = !_dataContext.Products.Any(p => p.Title == productName && p.DeleteDt == null);

            return isUnique;
        }
        private String _ValidateModel(ShopIndexFormModel model)
        {
            if (model == null) { return "Дані не передані"; }
            if (String.IsNullOrEmpty(model.Title)) { return "Назва не може бути порожним"; }
            bool isUniqueName = IsProductNameUnique(model.Title);
            if (!isUniqueName)
            {
                return "Товар з такою назвою вже існує";
            }

            if (model.Price == 0)
            {
                
                // можливо, помилка декодування через локалізацію (1.5/1,5)
                model.Price = Convert.ToSingle(
                    Request.Form["productPrice"].First()?.Replace(',', '.'),
                    CultureInfo.InvariantCulture);
            }
            if (!float.TryParse(model.Price.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out float price) || price <= 0)
            {
                return "Ціна повинна бути числом більше за 0";
            }

            if (model.Image != null)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                string fileExtension = Path.GetExtension(model.Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return "Недопустимий тип файлу. Дозволені розширення файлів: .jpg, .jpeg, .png, .gif";
                }

                
            }
            else
            {
                return "Файл-картинка необхідний";
            }
            String? newName = null;
            if (model.Image != null)  // є файл
            {
                String ext = Path.GetExtension(model.Image.FileName);
                newName = Guid.NewGuid().ToString() + ext;
                model.Image.CopyTo(new FileStream(
                        $"wwwroot/uploads/{newName}",
                        FileMode.Create));
            }
            else { return "Файл-картинка необхідний"; }
            _dataContext.Products.Add(new Data.Entity.Product
            {
                Id = Guid.NewGuid(),
                Title = model.Title,
                Description = model.Description,
                ProductGroupId = model.ProductGroupId,
                CreateDt = DateTime.Now,
                Price = model.Price,
                ImageUrl = newName
            });
            _dataContext.SaveChanges();  // PlanetScale не підтримує асинхронні запити
            return String.Empty;
        }
    }
}
