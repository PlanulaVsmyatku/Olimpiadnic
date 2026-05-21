using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Olimpiadnic.Extensions
{
    public static class SessionExtensions
    {
        // Сохранение объекта в сессию
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            session.SetString(key, json);
        }

        // Получение объекта из сессии
        public static T? GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return json == null ? default : JsonSerializer.Deserialize<T>(json);
        }

        // Проверка существования ключа
        public static bool ContainsKey(this ISession session, string key)
        {
            return session.Keys.Contains(key);
        }
    }
}