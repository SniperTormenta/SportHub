// Services/Scheduling/StubStrategies.cs
using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling
{
    
    /// Базовый класс-заглушка для нереализованных форматов турниров.
    /// Реализует IScheduleStrategy с IsImplemented = false; возвращает пустое расписание.
    /// В MVVM: В ViewModels проверять IsImplemented для показа сообщений в UI (e.g. вспомогательное окно "В разработке").
    /// Улучшение: Добавить логгирование или исключение в GenerateSchedule. В будущем
    
    public abstract class BaseStubScheduleStrategy : IScheduleStrategy
    {
        /// Конструктор с именем стратегии.
        protected BaseStubScheduleStrategy(string name)
        {
            Name = name;
        }

        
        /// Имя стратегии.
        public string Name { get; }

        /// Флаг реализации (false для заглушек).
        public bool IsImplemented => false;
        
        /// Генерация расписания (пустая для заглушек).
        
        public IEnumerable<Match> GenerateSchedule(IList<Team> teams)
        {
            // Пока что возвращаем пустой список — UI покажет сообщение "в разработке".
            return new List<Match>();
        }

        public TournamentBracket GenerateBracket(IList<Team> teams)
        {
            return null;
        }
    }

    /// Заглушка для швейцарской системы (парование по рейтингу после каждого тура).
    public class SwissScheduleStrategy : BaseStubScheduleStrategy
    {
        public SwissScheduleStrategy() : base("Швейцарский") { }
    }

    /// Заглушка для поэтапного турнира (группы + плей-офф).
    public class StagedScheduleStrategy : BaseStubScheduleStrategy
    {
        public StagedScheduleStrategy() : base("Поэтапный") { }
    }
}