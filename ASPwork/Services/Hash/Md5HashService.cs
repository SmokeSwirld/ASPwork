namespace ASPwork.Services.Hash
{
    public class Md5HashService : IHashService
    {
        public string HashString(string source)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            return Convert.ToHexString(
                md5.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(source)
            ));
        }

    }
}
//Цей код є реалізацією інтерфейсу IHashService і містить метод HashString,
//який отримує рядок source і повертає хеш-значення цього рядка, обчислене за допомогою алгоритму MD5.

//using var md5 = System.Security.Cryptography.MD5.Create(); -цей рядок створює екземпляр об'єкту MD5,
//який використовується для обчислення хеш-значення.

//md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source)) -цей рядок обчислює хеш-значення рядка source.
//Спочатку рядок перетворюється на байтовий масив, використовуючи кодування UTF-8,
//а потім цей масив передається до методу ComputeHash, який повертає хеш-значення.

//Convert.ToHexString - цей метод перетворює байтовий масив хеш-значення на шістнадцятковий рядок.

//Таким чином, метод HashString повертає шістнадцяткове рядкове представлення хеш-значення рядка source,
//обчисленого за допомогою алгоритму MD5.