namespace Olimpiadnic.Services
{
    public interface IInviteService
    {
        /// <summary>
        /// Генерирует инвайт ссылку и её токен + записывает в БД эту ссылку
        /// </summary>
        /// <param name="email"></param>
        /// <param name="role"></param>
        /// <param name="expiresInDays"></param>
        /// <returns></returns>
        Task<string> CreateInviteToken(string email, int role = 2, int? expiresInDays = 7);

        /// <summary>
        /// Проверяет срок годности инвайт-ссылки по токену
        /// </summary>
        /// <param name="token"> (string)Код токена</param>
        /// <returns></returns>
        Task<bool> ValidateInviteToken(string token);

        /// <summary>
        /// Помечает инвайт-ссылку как использованную после проверок.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        Task UseInviteToken(string token);

        /// <summary>
        /// Возвращает строку почты из таблицы инвайт-ссылки для авто-вставки форму регистрации
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<string> GetInviteEmail(string token);
    }
}
