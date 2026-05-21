using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Olimpiadnic.Services
{
    public class PasswordService : IPasswordService
    {
        // Настройки сложности BCrypt (10-12 - хорошо)
        private const int WorkFactor = 12;
        
        /// <summary>
        /// Хеширует пароль с использованием BCrypt
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Пароль не может быть пустым", nameof(password));
            }
            
            // BCrypt автоматически генерирует соль и включает её в хеш
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }
        
        /// <summary>
        /// Проверяет соответствие пароля хешу
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
                
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            //string pas = HashPassword(password); 

            try
            {
                // BCrypt сравнивает пароль с хешем
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Проверяет сложность пароля
        /// </summary>
        public (bool IsValid, string Message) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return (false, "Пароль не может быть пустым");
            }
            
            if (password.Length < 6)
            {
                return (false, "Пароль должен содержать минимум 6 символов");
            }
            
            if (password.Length > 100)
            {
                return (false, "Пароль не должен превышать 100 символов");
            }
            
            // Проверка на наличие хотя бы одной цифры
            if (!Regex.IsMatch(password, @"\d"))
            {
                return (false, "Пароль должен содержать хотя бы одну цифру");
            }
            
            // Проверка на наличие хотя бы одной буквы в верхнем регистре
            if (!Regex.IsMatch(password, @"[A-ZА-Я]"))
            {
                return (false, "Пароль должен содержать хотя бы одну заглавную букву");
            }
            
            // Проверка на наличие хотя бы одной буквы в нижнем регистре
            if (!Regex.IsMatch(password, @"[a-zа-я]"))
            {
                return (false, "Пароль должен содержать хотя бы одну строчную букву");
            }
            
            // Проверка на наличие специального символа (опционально)
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            {
                return (false, "Пароль должен содержать хотя бы один специальный символ (!@#$%^&*() и т.д.)");
            }
            
            // Запрет на простые последовательности
            string[] commonPatterns = { "123456", "qwerty", "password", "admin", "12345" };
            if (commonPatterns.Any(p => password.ToLower().Contains(p)))
            {
                return (false, "Пароль содержит слишком простую комбинацию символов");
            }
            
            return (true, "Пароль надежный");
        }
    }
}