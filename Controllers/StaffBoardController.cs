using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.OlympiadModels;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class StaffBoardController : Controller
    {
        private readonly ILogger<StaffBoardController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StaffBoardController(ILogger<StaffBoardController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        #region Создание олимпиады (для сотрудника и админа)

        [Authorize(Roles = "Сотрудник,Администратор")]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateOlympiadViewModel
            {
                RegistOpen = DateTime.Now.AddDays(7),
                RegistClosed = DateTime.Now.AddDays(30),
                EventStart = DateTime.Now.AddDays(31),
                EventEnd = DateTime.Now.AddDays(38),
                Status = "available",
                Questions = new List<QuestionViewModel>
                {
                    // Добавляем один пустой вопрос для примера
                    new QuestionViewModel
                    {
                        TempId = 1,
                        OrderNumber = 1,
                        Type = "auto",
                        Options = new List<AutoQuestionOptionViewModel>
                        {
                            new AutoQuestionOptionViewModel { TempId = 1, SortOrder = 1 }
                        }
                    }
                }
            };
            return View(model);
        }

        [Authorize(Roles = "Сотрудник,Администратор")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOlympiad(CreateOlympiadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Сохранить в БД
            // 1. Сохранить основную информацию об олимпиаде
            // 2. Сохранить вопросы
            // 3. Сохранить варианты ответов для авто-вопросов
            // 4. Сохранить конфигурации для ручных вопросов
            // 5. Сохранить вложения

            TempData["SuccessMessage"] = "Олимпиада успешно создана!";
            return RedirectToAction("MyOlympiads", "Dashboard");
        }

        [HttpPost]
        public IActionResult AddQuestion(CreateOlympiadViewModel model)
        {
            var newId = model.Questions.Any() ? model.Questions.Max(q => q.TempId) + 1 : 1;
            model.Questions.Add(new QuestionViewModel
            {
                TempId = newId,
                OrderNumber = model.Questions.Count + 1,
                Type = "auto",
                Options = new List<AutoQuestionOptionViewModel>
                {
                    new AutoQuestionOptionViewModel { TempId = 1, SortOrder = 1 }
                }
            });
            return View("Create", model);
        }

        [HttpPost]
        public IActionResult RemoveQuestion(CreateOlympiadViewModel model, int tempId)
        {
            var question = model.Questions.FirstOrDefault(q => q.TempId == tempId);
            if (question != null)
            {
                model.Questions.Remove(question);
                // Перенумеровать оставшиеся вопросы
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    model.Questions[i].OrderNumber = i + 1;
                }
            }
            return View("Create", model);
        }

        [HttpPost]
        public IActionResult AddOption(CreateOlympiadViewModel model, int questionTempId)
        {
            var question = model.Questions.FirstOrDefault(q => q.TempId == questionTempId);
            if (question != null)
            {
                var newId = question.Options.Any() ? question.Options.Max(o => o.TempId) + 1 : 1;
                question.Options.Add(new AutoQuestionOptionViewModel
                {
                    TempId = newId,
                    SortOrder = question.Options.Count + 1
                });
            }
            return View("Create", model);
        }

        [HttpPost]
        public IActionResult RemoveOption(CreateOlympiadViewModel model, int questionTempId, int optionTempId)
        {
            var question = model.Questions.FirstOrDefault(q => q.TempId == questionTempId);
            if (question != null)
            {
                var option = question.Options.FirstOrDefault(o => o.TempId == optionTempId);
                if (option != null)
                {
                    question.Options.Remove(option);
                    for (int i = 0; i < question.Options.Count; i++)
                    {
                        question.Options[i].SortOrder = i + 1;
                    }
                }
            }
            return View("Create", model);
        }

        #endregion

    }
}
