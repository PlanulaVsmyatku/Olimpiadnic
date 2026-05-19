using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Entities;

namespace Olimpiadnic.Services
{
    public class InviteService : IInviteService
    {
         
        private readonly AppDbContext _context;
        public InviteService(AppDbContext context)
        {
            _context = context;
        }

        
        public async Task<string> CreateInviteToken(string email, int role = 2, int? expiresInDays = 7)
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_").Replace("+", "-");

            

            var invite = new Invite
            {
                Token = token,
                Email = email,
                RoleId = role,
                ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays ?? 7),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            _context.Invites.Add(invite);
            await _context.SaveChangesAsync();

            return token;
        }

        
        public async Task<string> GetInviteEmail(string token)
        {
            //Для отображения в представлении
            var inviteData = await _context.Invites
                .FirstOrDefaultAsync(i => i.Token == token);

            return inviteData?.Email ?? "Почта не найдена";
        }

        
        public async Task UseInviteToken(string token)
        {
            var invite = await _context.Invites
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invite == null)
            {
                throw new InvalidOperationException("Приглашение не найдено");
            }

            if (invite.IsUsed)
            {
                throw new InvalidOperationException("Приглашение уже было использовано");
            }

            if (invite.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Срок действия приглашения истёк");
            }

            // Помечаем токен как использованный
            invite.IsUsed = true;

            await _context.SaveChangesAsync();
        }

        
        public async Task<bool> ValidateInviteToken(string token)
        {
            var invite = await _context.Invites
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invite == null) return false;
            if (invite.IsUsed) return false;
            if (invite.ExpiresAt < DateTime.UtcNow) return false;

            return true;
        }

        /// <summary>
        /// Дополнительный полезный метод для получения информации о приглашении
        /// </summary>
        /// <param name="token"> (string)Код токена</param>
        /// <returns></returns>
        public async Task<Invite?> GetInviteByToken(string token)
        {
            return await _context.Invites
                .FirstOrDefaultAsync(i => i.Token == token);
        }


    }
}
