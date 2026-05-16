using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.OlympiadModels;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class OlympiadController : Controller
    {
        private readonly ILogger<OlympiadController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OlympiadController(ILogger<OlympiadController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        

        #region Просмотр олимпиады (для всех)

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // TODO: Получить данные из БД
            var olympiad = GetMockOlympiadDetails(id);
            
            if (olympiad == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            olympiad.IsRegistered = false; // TODO: Проверить в БД
            olympiad.CanParticipate = DateTime.Now >= olympiad.RegistOpen && 
                                       DateTime.Now <= olympiad.RegistClosed;

            return View(olympiad);
        }

        #endregion

        #region Прохождение олимпиады (для участников)

        [Authorize(Roles = "Участник")]
        [HttpGet]
        public async Task<IActionResult> Participate(int id)
        {
            // TODO: Проверить, зарегистрирован ли пользователь и доступна ли олимпиада

            // Получаем сохранённые ответы пользователя из сессии или БД
            var sessionKey = $"Olympiad_{id}_Participant_{User.FindFirstValue(ClaimTypes.NameIdentifier)}";
            var savedAnswers = HttpContext.Session.GetObject<Dictionary<int, SavedAnswer>>(sessionKey) ?? new Dictionary<int, SavedAnswer>();

            var participation = GetMockParticipation(id);

            // Загружаем сохранённые ответы для текущего вопроса
            if (savedAnswers.ContainsKey(participation.CurrentQuestion.QuestionId))
            {
                var savedAnswer = savedAnswers[participation.CurrentQuestion.QuestionId];
                if (participation.CurrentQuestion.Type == "auto")
                {
                    participation.CurrentQuestion.SelectedOptionIds = savedAnswer.SelectedOptionIds ?? new List<int>();
                    // Отмечаем выбранные варианты
                    foreach (var option in participation.CurrentQuestion.Options)
                    {
                        option.IsSelected = savedAnswer.SelectedOptionIds?.Contains(option.OptionId) ?? false;
                    }
                }
                else
                {
                    participation.CurrentQuestion.ManualAnswer = savedAnswer.ManualAnswer ?? string.Empty;
                }
            }

            return View(participation);
        }

        [Authorize(Roles = "Участник")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAnswer(OlympiadParticipationViewModel model)
        {
            // Получаем ID пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionKey = $"Olympiad_{model.OlympiadId}_Participant_{userId}";

            // Получаем существующие ответы из сессии
            var savedAnswers = HttpContext.Session.GetObject<Dictionary<int, SavedAnswer>>(sessionKey) ?? new Dictionary<int, SavedAnswer>();

            // Сохраняем текущий ответ
            var savedAnswer = new SavedAnswer
            {
                QuestionId = model.CurrentQuestion.QuestionId,
                Type = model.CurrentQuestion.Type
            };

            if (model.CurrentQuestion.Type == "auto")
            {
                savedAnswer.SelectedOptionIds = model.CurrentQuestion.SelectedOptionIds;
            }
            else
            {
                savedAnswer.ManualAnswer = model.CurrentQuestion.ManualAnswer;
            }

            savedAnswers[model.CurrentQuestion.QuestionId] = savedAnswer;

            // Сохраняем в сессию
            HttpContext.Session.SetObject(sessionKey, savedAnswers);

            // Определяем, куда переходить дальше
            if (Request.Form.ContainsKey("next"))
            {
                // Переход к следующему вопросу
                model.CurrentQuestionIndex++;

                if (model.CurrentQuestionIndex >= model.TotalQuestions)
                {
                    // Завершаем олимпиаду
                    return RedirectToAction("Complete", new { id = model.OlympiadId });
                }

                // Загружаем следующий вопрос с сохранёнными ответами
                model.CurrentQuestion = GetMockQuestion(model.OlympiadId, model.CurrentQuestionIndex);

                // Загружаем сохранённые ответы для нового вопроса
                if (savedAnswers.ContainsKey(model.CurrentQuestion.QuestionId))
                {
                    var prevAnswer = savedAnswers[model.CurrentQuestion.QuestionId];
                    if (model.CurrentQuestion.Type == "auto")
                    {
                        model.CurrentQuestion.SelectedOptionIds = prevAnswer.SelectedOptionIds ?? new List<int>();
                        foreach (var option in model.CurrentQuestion.Options)
                        {
                            option.IsSelected = prevAnswer.SelectedOptionIds?.Contains(option.OptionId) ?? false;
                        }
                    }
                    else
                    {
                        model.CurrentQuestion.ManualAnswer = prevAnswer.ManualAnswer ?? string.Empty;
                    }
                }

                return View("Participate", model);
            }
            else if (Request.Form.ContainsKey("previous"))
            {
                // Переход к предыдущему вопросу
                model.CurrentQuestionIndex--;
                model.CurrentQuestion = GetMockQuestion(model.OlympiadId, model.CurrentQuestionIndex);

                // Загружаем сохранённые ответы
                if (savedAnswers.ContainsKey(model.CurrentQuestion.QuestionId))
                {
                    var prevAnswer = savedAnswers[model.CurrentQuestion.QuestionId];
                    if (model.CurrentQuestion.Type == "auto")
                    {
                        model.CurrentQuestion.SelectedOptionIds = prevAnswer.SelectedOptionIds ?? new List<int>();
                        foreach (var option in model.CurrentQuestion.Options)
                        {
                            option.IsSelected = prevAnswer.SelectedOptionIds?.Contains(option.OptionId) ?? false;
                        }
                    }
                    else
                    {
                        model.CurrentQuestion.ManualAnswer = prevAnswer.ManualAnswer ?? string.Empty;
                    }
                }

                return View("Participate", model);
            }

            return View("Participate", model);
        }

        // GET: /Olympiad/PreviousQuestion
        [Authorize(Roles = "Участник")]
        [HttpPost]
        public async Task<IActionResult> PreviousQuestion(int olympiadId, int currentIndex)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionKey = $"Olympiad_{olympiadId}_Participant_{userId}";
            var savedAnswers = HttpContext.Session.GetObject<Dictionary<int, SavedAnswer>>(sessionKey) ?? new Dictionary<int, SavedAnswer>();

            var previousIndex = currentIndex - 1;
            var participation = GetMockParticipation(olympiadId);
            participation.CurrentQuestionIndex = previousIndex;
            participation.CurrentQuestion = GetMockQuestion(olympiadId, previousIndex);

            // Загружаем сохранённые ответы
            if (savedAnswers.ContainsKey(participation.CurrentQuestion.QuestionId))
            {
                var savedAnswer = savedAnswers[participation.CurrentQuestion.QuestionId];
                if (participation.CurrentQuestion.Type == "auto")
                {
                    participation.CurrentQuestion.SelectedOptionIds = savedAnswer.SelectedOptionIds ?? new List<int>();
                    foreach (var option in participation.CurrentQuestion.Options)
                    {
                        option.IsSelected = savedAnswer.SelectedOptionIds?.Contains(option.OptionId) ?? false;
                    }
                }
                else
                {
                    participation.CurrentQuestion.ManualAnswer = savedAnswer.ManualAnswer ?? string.Empty;
                }
            }

            return View("Participate", participation);
        }

        [Authorize(Roles = "Участник")]
        [HttpGet]
        public async Task<IActionResult> Complete(int id)
        {
            // TODO: Сохранить все ответы в БД и показать результаты
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionKey = $"Olympiad_{id}_Participant_{userId}";

            // Получаем все ответы из сессии
            var savedAnswers = HttpContext.Session.GetObject<Dictionary<int, SavedAnswer>>(sessionKey) ?? new Dictionary<int, SavedAnswer>();

            // TODO: Сохранить ответы в БД
            // foreach (var answer in savedAnswers)
            // {
            //     // Сохраняем в Submission_items и связанные таблицы
            // }

            // Очищаем сессию
            HttpContext.Session.Remove(sessionKey);

            ViewBag.OlympiadId = id;
            return View();
        }

        #endregion

        // Вспомогательный класс для сохранения ответов
        public class SavedAnswer
        {
            public int QuestionId { get; set; }
            public string Type { get; set; } = string.Empty;
            public List<int>? SelectedOptionIds { get; set; }
            public string? ManualAnswer { get; set; }
        }

        #region Заглушки данных

        //Метод для получение общих данных о олимпиаде
        //TODO: Поиск олимпиады в БД по ID
        private OlympiadDetailsViewModel? GetMockOlympiadDetails(int id)
        {
            var tets_olympiads = new Dictionary<int, OlympiadDetailsViewModel>
            {
                {
                    1, new OlympiadDetailsViewModel
                    {
                        OlympiadId = 1,
                        Title = "Олимпиада по программированию 2025",
                        ImageUrl = "/images/programming-olympiad.jpg",
                        Description = @"
                            <h4>Добро пожаловать на олимпиаду по программированию!</h4>
                            <p>Эта олимпиада предназначена для школьников 9-11 классов и студентов младших курсов.</p>
                            <h5>Что вас ждёт:</h5>
                            <ul>
                                <li>10 задач разного уровня сложности</li>
                                <li>Задачи на алгоритмы и структуры данных</li>
                                <li>Возможность проверить свои знания</li>
                                <li>Сертификаты для победителей</li>
                            </ul>
                            <h5>Требования:</h5>
                            <ul>
                                <li>Базовые знания языка C++ или Python</li>
                                <li>Понимание основ алгоритмизации</li>
                                <li>Умение решать логические задачи</li>
                            </ul>
                        ",
                        Credentials = "Участие бесплатное. Требуется подтверждение личности.",
                        Status = "available",
                        EventStart = new DateTime(2025, 6, 10, 10, 0, 0),
                        EventEnd = new DateTime(2025, 6, 15, 18, 0, 0),
                        RegistOpen = new DateTime(2025, 5, 1, 0, 0, 0),
                        RegistClosed = new DateTime(2025, 6, 5, 23, 59, 0),
                        TotalQuestions = 10
                    }
                },
                {
                    2, new OlympiadDetailsViewModel
                    {
                        OlympiadId = 2,
                        Title = "Олимпиада по математике",
                        ImageUrl = "/images/math-olympiad.jpg",
                        Description = @"
                            <h4>Международная олимпиада по математике</h4>
                            <p>Приглашаем всех желающих проверить свои знания в области математики!</p>
                            <h5>Программа олимпиады:</h5>
                            <ul>
                                <li>Алгебра и теория чисел</li>
                                <li>Геометрия</li>
                                <li>Комбинаторика</li>
                                <li>Математический анализ</li>
                            </ul>
                        ",
                        Credentials = "Для участия необходимо иметь базовые знания школьной математики.",
                        Status = "available",
                        EventStart = new DateTime(2025, 5, 15, 10, 0, 0),
                        EventEnd = new DateTime(2025, 5, 20, 18, 0, 0),
                        RegistOpen = new DateTime(2025, 4, 1, 0, 0, 0),
                        RegistClosed = new DateTime(2025, 5, 10, 23, 59, 0),
                        TotalQuestions = 8
                    }
                }
            };

            return tets_olympiads.GetValueOrDefault(id);
        }

        private OlympiadParticipationViewModel GetMockParticipation(int olympiadId)
        {
            return new OlympiadParticipationViewModel
            {
                OlympiadId = olympiadId,
                OlympiadTitle = olympiadId == 1 ? "Олимпиада по программированию 2025" : "Олимпиада по математике",
                ParticipantId = 1,
                CurrentQuestionIndex = 0,
                TotalQuestions = 5,
                CurrentQuestion = GetMockQuestion(olympiadId, 0)
            };
        }

        //Метод для получения списка вопросов олимпиады
        //TODO: Получение ID олимпиады и нахождение списка вопросов на сервере
        private QuestionParticipationViewModel GetMockQuestion(int olympiadId, int index)
        {
            if (olympiadId == 1)
            {
                // Вопросы для олимпиады по программированию
                var questions = new List<QuestionParticipationViewModel>
                {
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 1,
                        OrderNumber = 1,
                        Description = "Что такое алгоритм?",
                        Type = "auto",
                        Attachments = new List<string>(),
                        Options = new List<AutoQuestionOptionParticipationViewModel>
                        {
                            new AutoQuestionOptionParticipationViewModel { OptionId = 1, OptionText = "Набор инструкций для решения задачи", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 2, OptionText = "Компьютерная программа", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 3, OptionText = "Язык программирования", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 4, OptionText = "Тип данных", IsSelected = false }
                        }
                    },
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 2,
                        OrderNumber = 2,
                        Description = "Какой из перечисленных языков является языком программирования? (Выберите все подходящие)",
                        Type = "auto",
                        Attachments = new List<string>(),
                        Options = new List<AutoQuestionOptionParticipationViewModel>
                        {
                            new AutoQuestionOptionParticipationViewModel { OptionId = 5, OptionText = "Python", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 6, OptionText = "HTML", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 7, OptionText = "Java", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 8, OptionText = "CSS", IsSelected = false }
                        }
                    },
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 3,
                        OrderNumber = 3,
                        Description = "Напишите функцию на Python, которая вычисляет факториал числа n.",
                        Type = "manual",
                        Attachments = new List<string>(),
                        ManualAnswer = string.Empty
                    },
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 4,
                        OrderNumber = 4,
                        Description = "Сопоставьте алгоритм сортировки с его сложностью в худшем случае:",
                        Type = "auto",
                        Attachments = new List<string>(),
                        Options = new List<AutoQuestionOptionParticipationViewModel>
                        {
                            new AutoQuestionOptionParticipationViewModel { OptionId = 9, OptionText = "Пузырьковая сортировка - O(n²)", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 10, OptionText = "Быстрая сортировка - O(n²)", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 11, OptionText = "Сортировка слиянием - O(n log n)", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 12, OptionText = "Сортировка выбором - O(n²)", IsSelected = false }
                        }
                    },
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 5,
                        OrderNumber = 5,
                        Description = "Что выведет следующий код?<br><code>print(2 ** 3 ** 2)</code>",
                        Type = "auto",
                        Attachments = new List<string>(),
                        Options = new List<AutoQuestionOptionParticipationViewModel>
                        {
                            new AutoQuestionOptionParticipationViewModel { OptionId = 13, OptionText = "64", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 14, OptionText = "512", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 15, OptionText = "256", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 16, OptionText = "128", IsSelected = false }
                        }
                    }
                };
                return questions[index];
            }
            else
            {
                // Вопросы для олимпиады по математике
                var questions = new List<QuestionParticipationViewModel>
                {
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 101,
                        OrderNumber = 1,
                        Description = "Решите уравнение: 2x + 5 = 15",
                        Type = "auto",
                        Attachments = new List<string>(),
                        Options = new List<AutoQuestionOptionParticipationViewModel>
                        {
                            new AutoQuestionOptionParticipationViewModel { OptionId = 101, OptionText = "x = 5", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 102, OptionText = "x = 10", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 103, OptionText = "x = 3", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 104, OptionText = "x = 7", IsSelected = false }
                        }
                    },
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 102,
                        OrderNumber = 2,
                        Description = "Найдите площадь круга с радиусом 5 см. (π ≈ 3.14)",
                        Type = "auto",
                        Attachments = new List<string>(),
                        Options = new List<AutoQuestionOptionParticipationViewModel>
                        {
                            new AutoQuestionOptionParticipationViewModel { OptionId = 105, OptionText = "78.5 см²", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 106, OptionText = "31.4 см²", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 107, OptionText = "15.7 см²", IsSelected = false },
                            new AutoQuestionOptionParticipationViewModel { OptionId = 108, OptionText = "314 см²", IsSelected = false }
                        }
                    },
                    new QuestionParticipationViewModel
                    {
                        QuestionId = 103,
                        OrderNumber = 3,
                        Description = "Решите задачу: В магазине было 120 кг яблок. За день продали 35% всех яблок. Сколько килограммов яблок осталось?",
                        Type = "manual",
                        Attachments = new List<string>(),
                        ManualAnswer = string.Empty
                    }
                };
                return questions[index];
            }
        }

        #endregion
    }
}