using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Entities;
using Olimpiadnic.Models.OlympiadModels;
namespace Olimpiadnic.Services.Repos
{
    public class OlympiadRepository : IOlympiadRepository
    {
        private readonly AppDbContext _context;

        public OlympiadRepository(AppDbContext context)
        {
            _context = context;
        }

        #region Олимпиады
        public async Task<IEnumerable<Olympiad>> GetAllOlympiadsAsync()
        {
            var allOlympiads = await _context.Olympiads
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync<Olympiad>();

            return allOlympiads;
        }

        public async Task<Olympiad> GetOlympiadByIdAsync(int olympId)
        {
            var olympiad = await _context.Olympiads
                            .FirstOrDefaultAsync(o => o.OlympId == olympId);
            return olympiad;
        }

        public async Task<IEnumerable<Olympiad>> GetActiveOlympiadsAsync()
        {
            var now = DateTime.UtcNow;

            var activeOlympiads = await _context.Olympiads
                .Where(o => o.EventEnd >= now)
                .OrderBy(o => o.EventStart)
                .ToListAsync();

            return activeOlympiads;
        }

        public async Task<OlympiadPagedResult> GetActiveOlympiadsPagedFilteredAsync(
            OlympiadSearchViewModel? searchModel,
            int pageNumber,
            int pageSize = 12)
        {
            var now = DateTime.UtcNow;

            // Базовый запрос - только активные олимпиады (не завершённые)
            var query = _context.Olympiads
                .Where(o => o.EventEnd >= now);

            // Применяем фильтры из searchModel
            if (searchModel != null)
            {
                // Фильтр по названию
                if (!string.IsNullOrWhiteSpace(searchModel.SearchTitle))
                {
                    query = query.Where(o => o.Title.Contains(searchModel.SearchTitle));
                }

                // Фильтр по дате начала (от)
                if (searchModel.StartDateFrom.HasValue)
                {
                    query = query.Where(o => o.EventStart >= searchModel.StartDateFrom.Value);
                }

                // Фильтр по дате начала (до)
                if (searchModel.StartDateTo.HasValue)
                {
                    query = query.Where(o => o.EventStart <= searchModel.StartDateTo.Value);
                }

                // Фильтр по дате окончания (от)
                if (searchModel.EndDateFrom.HasValue)
                {
                    query = query.Where(o => o.EventEnd >= searchModel.EndDateFrom.Value);
                }

                // Фильтр по дате окончания (до)
                if (searchModel.EndDateTo.HasValue)
                {
                    query = query.Where(o => o.EventEnd <= searchModel.EndDateTo.Value);
                }
            }

            // Сортировка: сначала те, что скоро начнутся
            query = query.OrderBy(o => o.EventStart);

            // Получаем общее количество после фильтрации
            var totalCount = await query.CountAsync();

            // Рассчитываем количество страниц
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Корректируем номер страницы
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            // Получаем элементы для текущей страницы
            // Получаем данные из БД
            var itemsData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OlympId,
                    o.Title,
                    o.Description,
                    o.ImageUrl,
                    o.Credentials,
                    o.EventStart,
                    o.EventEnd,
                    o.RegistOpen,
                    o.RegistClosed
                })
                .ToListAsync();

            // Вычисляем статус на клиенте (в памяти)
            var items = itemsData.Select(o => new OlympiadCardViewModel
            {
                OlympiadId = o.OlympId,
                Title = o.Title,
                Description = o.Description ?? string.Empty,
                ImageUrl = o.ImageUrl,
                Credentials = o.Credentials,
                EventStart = o.EventStart,
                EventEnd = o.EventEnd,
                RegistOpen = o.RegistOpen,
                RegistClosed = o.RegistClosed,
                Status = GetStatusForOlympiadStatic(o.EventStart, o.EventEnd, o.RegistOpen, o.RegistClosed),
                IsUserRegistered = false
            }).ToList();

            return new OlympiadPagedResult
            {
                Items = items,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                SearchModel = searchModel //сохраняем параметры поисковой модели
            };
        }

        // Вспомогательный метод для определения статуса олимпиады
        private static string GetStatusForOlympiadStatic(DateTime eventStart, DateTime eventEnd, DateTime registOpen, DateTime registClosed)
        {
            var now = DateTime.UtcNow;

            if (now >= registOpen && now <= registClosed && eventEnd >= now)
                return "Регистрация открыта";
            if (now >= eventStart && now <= eventEnd)
                return "Идёт";
            if (now > eventEnd)
                return "Завершена";
            if (now < registOpen)
                return "Регистрация скоро";

            return "Скоро";
        }

        public async Task<Olympiad?> GetOlympiadWithDetailsAsync(int olympId)
        {
            return await _context.Olympiads
                .Include(o => o.Questions.Where(q => q.IsActual))
                .FirstOrDefaultAsync(o => o.OlympId == olympId);
        }

        public async Task<int> GetTotalQuestionsCountAsync(int olympId)
        {
            return await _context.Questions
                .Where(q => q.OlympId == olympId && q.IsActual)
                .CountAsync();
        }

        #endregion

        #region Участники
        public async Task<IEnumerable<OlympiadParticipant>> GetParticipantsByOlympiadIdAsync(int olympId)
        {
            var participants = await _context.OlympiadParticipants
                .Where(p => p.OlympId == olympId)
                .ToListAsync();

            return participants;
        }

        public async Task<IEnumerable<OlympiadParticipant>> GetParticipantsByUserAndOlympiadIdsAsync(string userId, List<int> olympiadIds)
        {
            return await _context.OlympiadParticipants
                .Where(p => p.UserId == int.Parse(userId) && olympiadIds.Contains(p.OlympId))
                .ToListAsync();
        }
        #endregion
    }
}
