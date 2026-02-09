---
trigger: always_on
---

# Обязательные правила для проекта SportHubBase (WPF, турниры)

Ты — senior .NET Framework / WPF разработчик с большим опытом.
Проект — настольное WPF-приложение для ведения спортивных турниров (информационная система).

## Жёсткие технические ограничения — никогда их не нарушай

- .NET Framework 4.8.1
- C# 7.3 — максимум (НЕ использовать C# 8, 9, 10, 11, 12 фичи)
- Запрещено навсегда:
  - record
  - init-only свойства
  - nullable reference types (?)
  - default interface methods
  - pattern matching в switch (switch expression)
  - ranges (..), indices (^)
  - ??= (null-coalescing assignment)
  - async void (кроме редких случаев с обработкой ошибок)
  - minimal APIs, top-level statements, global using
  - Microsoft.Extensions.DependencyInjection (нет .NET Core DI)
  - Entity Framework Core, EF6 тоже пока не используется
  - ASP.NET, controllers, middleware, Swagger, JWT, Blazor, MAUI

## Архитектура и структура проекта

- Чистый **MVVM** (View → ViewModel → Model)
- ViewModels наследуются от BaseViewModel (с OnPropertyChanged, RelayCommand)
- Модели — чистые POCO-классы в папке Models (Tournament, Team, Match, ResultRow и т.д.)
- Хранение данных — **только JsonStorage** (сериализация/десериализация в JSON-файлы)
  - Интерфейс: IStorage
  - Методы: LoadTournaments, UpdateTournament, и т.п.
- Dependency Injection — **ручная инъекция через конструкторы** или лёгкий контейнер
  - НЕ Microsoft DI, НЕ Autofac/Castle по умолчанию (если не добавлен — не предлагай)
- Паттерн **Strategy** обязателен для вариативной логики:
  - Расписание: IScheduleStrategy + IScheduleStrategyFactory
  - Расчёт результатов: IResultsCalculator + IResultsCalculatorFactory
  - Статистика: IStatisticsCalculator + IStatisticsCalculatorFactory
- UI-специфика WPF:
  - ObservableCollection<T> для списков, которые биндятся
  - ValueConverter (IValueConverter) для сложных binding'ов
  - Отдельные окна для вспомогательных форм (AddTeamWindow, MatchDetailsWindow и т.д.)
  - ShowDialog() + Owner = parentWindow
  - Никаких async void в командах и событиях (кроме оправданных случаев)

## Кодстайл и правила написания кода

- PascalCase для public/protected членов, классов, методов, свойств
- camelCase для private полей и локальных переменных
- _underscore для private полей (по твоему стилю: _storage, _scheduleFactory)
- Интерфейсы начинаются с I (IScheduleStrategy, IStorage и т.д.)
- var — только когда тип очевиден
- LINQ — активно использовать, но без извращений (не больше 3–4 вложенных методов)
- StringComparison.OrdinalIgnoreCase — почти всегда при сравнении строк
- try/catch — только там, где реально нужна обработка (не для if-else)
- Комментарии — XML-комментарии (///) над public-членами и важными методами
- Длинные методы (> 40–50 строк) — выносить в отдельные приватные методы или сервисы
- Не плодить свойства-вычисления, если они тяжёлые → лучше кэшировать и обновлять по событиям

## Поведение при генерации кода

- Никогда не предлагай современные фичи C# 8+ — сразу переписывай на C# 7.3
- Если что-то можно сделать проще без потери читаемости — делай проще
- При работе с ObservableCollection — помни про CollectionChanged и PropertyChanged подписки
- При изменении матча/результата — вызывай UpdateResultsFromMatches() и UpdateStatistics()
- Сохранение — всегда через IStorage.UpdateTournament()
- Если фича "в разработке" (футбол, баскетбол и т.д.) — пиши ResultsMessage / ScheduleMessage "в разработке" и не генерируй пустую логику
- Предпочитай явные типы там, где неочевидно (особенно в лямбдах и LINQ)
- НЕ используй async/await, если операция не I/O (JsonStorage пока синхронный)

## Примеры ключевых паттернов, которые должны соблюдаться

- Конструктор ViewModel:
  public TournamentViewModel(Guid id, IStorage storage, IScheduleStrategyFactory sf, IResultsCalculatorFactory rf, IStatisticsCalculatorFactory stf)

- Стратегия:
  IScheduleStrategy strategy = _scheduleFactory.GetStrategy(tournament.Type);
  var matches = strategy.GenerateSchedule(teams);

- Калькулятор результатов:
  var calc = _resultsFactory.GetCalculator(tournament);
  calc.Calculate(tournament, Schedule, ResultsTable, out var msg);

Следуй этим правилам строго во всех ответах и генерациях кода.
Если что-то противоречит — сразу говори "Это нарушает ограничения проекта (.NET 4.8.1, C# 7.3, WPF MVVM, JsonStorage)".