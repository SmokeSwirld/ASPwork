using ASPwork.Data;
using ASPwork.Models.User;
using ASPwork.Services.Hash;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ASPwork.Controllers
{
    public class UserController : Controller
    {
        // Підключення БД - інжекція залежності від контексту (зареєстрованого у Program.cs)
        private readonly DataContext _dataContext;
        private readonly IHashService _hashService;

        public UserController(DataContext dataContext, IHashService hashService)
        {
            _dataContext = dataContext;
            _hashService = hashService;
        }

        public IActionResult SignUp(UserSignupModel? model)
        {
            if (HttpContext.Request.Method == "POST")   // є передані з форми дані
            {
                ViewData["form"] = _ValidateModel(model);
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult LogIn([FromForm] String login, [FromForm] String password)
        {
            var user = _dataContext.Users.FirstOrDefault(u => u.Login == login);
            if (user != null)
            {
                if (user.PasswordHash == _hashService.HashString(password))
                {
                    // Автентифікацію пройдено
                    // зберігаємо у сесії id користувача
                    HttpContext.Session.SetString("AuthUserId", user.Id.ToString());
                    return Json(new { status = "OK" });
                }
            }
            return Json(new { status = "NO" });
        }
        private bool IsPasswordComplex(string password)
        {
            // метод IsPasswordComplex перевіряє, чи відповідає пароль заданим правилам складності.
            // Наведений шаблон регулярного виразу ^(?=.*[a - z])(?=.*[A - Z])(?=.*\d).{ 8,}$ гарантує,
            // що пароль містить принаймні одну малу літеру, одну велику літеру, одну цифру і має довжину не менше 8 символів.
            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

            return Regex.IsMatch(password, passwordPattern);
        }
        private bool IsLoginAlreadyUsed(string login)
        {
            // Запит до бази даних, щоб перевірити, чи існує користувач з таким самим логіном
            bool loginExists = _dataContext.Users.Any(u => u.Login == login);

            return loginExists;
        }
        // Перевіряє валідність даних у моделі, прийнятої з форми
        // Повертає повідомлення про помилку валідації або String.Empty
        // якщо перевірка успішна
        private String _ValidateModel(UserSignupModel? model)
        {
            if (model == null) { return "Дані не передані"; }
            if (String.IsNullOrEmpty(model.Login)) { return "Логін не може бути порожним"; }
            if (IsLoginAlreadyUsed(model.Login))
            {
                return "Цей логін вже використовується.";
            }
            if (String.IsNullOrEmpty(model.Password)) { return "Пароль не може бути порожним"; }
            if (!IsPasswordComplex(model.Password))
            {
                return "Пароль не відповідає вимогам складності.пароль повинен містити принаймні одну малу літеру, одну велику літеру, одну цифру і має довжину не менше 8 символів.";
            }
            if (String.IsNullOrEmpty(model.RepeatPassword)) { return "Повтор паролю не може бути порожнім"; }
            if (model.Password != model.RepeatPassword) { return "Пароль та повтор паролю не співпадають"; }
            if (String.IsNullOrEmpty(model.Email)) { return "Email не може бути порожним"; }
            // У цьому прикладі для перевірки формату листа використовується шаблон
            // регулярного виразу ^[a - zA - Z0 - 9_.+ -] +@[a-zA - Z0 - 9 -]+\.[a-zA - Z0 - 9 -.]+$.
            // Він гарантує, що лист містить алфавітно - цифрові символи, крапки, підкреслення, знаки плюс і дефіс,
            // за якими слідує символ "@", ім'я домену та домен верхнього рівня (TLD).
            string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
            if (!Regex.IsMatch(model.Email, emailPattern))  //using System.Text.RegularExpressions;
            {
                return "Введений некоректний формат електронної пошти.";
            }
            if (!model.Agree) { return "Необхідно дотримуватись правил сайту"; }
            // завантажуємо файл-аватарку
            String? newName = null;
            if (model.AvatarFile != null)  // є файл
            {
                // Перевіряємо, чи дозволено тип файлу
                string[] allowedFileTypes = { ".jpg", ".jpeg", ".png" };
                string ext = Path.GetExtension(model.AvatarFile.FileName).ToLower();
                if (!allowedFileTypes.Contains(ext))
                {
                    return "Неприпустимий тип файлу для аватарки. Дозволені формати: JPG, JPEG, PNG.";
                }

                // Створюємо каталог, якщо він не існує
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Створюємо нове унікальне ім'я файлу
                newName = Guid.NewGuid().ToString() + ext;

                // Зберігаємо файл до каталогу завантажень
                string filePath = Path.Combine(uploadPath, newName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.AvatarFile.CopyTo(stream);
                }
            }
            // додаємо користувача до БД
            _dataContext.Users.Add(new Data.Entity.User
            {
                Id = Guid.NewGuid(),
                Login = model.Login,
                PasswordHash = _hashService.HashString(model.Password),
                Email = model.Email,
                Avatar = newName,
                RealName = model.RealName,
                RegisteredDt = DateTime.Now,
            });
            // зберігаємо внесені зміни
            _dataContext.SaveChanges();  // PlanetScale не підтримує асинхронні запити

            return String.Empty;
        }
        [HttpPost]
        public IActionResult SignOut()
        {
            HttpContext.Session.Remove("AuthUserId"); // видалення ключа з сесії
            HttpContext.SignOutAsync();


            //HttpContext.Session.Clear();
            // HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            return RedirectToAction("Index", "Home");
        }
    }
}
