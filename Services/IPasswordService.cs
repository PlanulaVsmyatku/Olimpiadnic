namespace Olimpiadnic.Services
{
    public interface IPasswordService
    {
        /// <summary>
        /// Хеширует пароль
        /// </summary>
        /// <param name="password">Исходный пароль</param>
        /// <returns>Хеш пароля</returns>
        string HashPassword(string password);

        /// <summary>
        /// Проверяет соответствие пароля хешу
        /// </summary>
        /// <param name="password">Введенный пароль</param>
        /// <param name="hash">Хеш из БД</param>
        /// <returns>True - пароль верный, False - неверный</returns>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// Проверяет сложность пароля
        /// </summary>
        /// <param name="password">Пароль для проверки</param>
        /// <returns>Результат проверки с сообщением</returns>
        (bool IsValid, string Message) ValidatePasswordStrength(string password);
    }
}
