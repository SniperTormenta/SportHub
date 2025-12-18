// Services/Scheduling/StubStrategies.cs
using System.Collections.Generic;
using SportHubBase.Models;

namespace SportHubBase.Services.Scheduling
{
    /// <summary>
    /// Базовый класс-заглушка для нереализованных форматов турниров.
    /// </summary>
    public abstract class BaseStubScheduleStrategy : IScheduleStrategy
    {
        protected BaseStubScheduleStrategy(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool IsImplemented => false;

        public IEnumerable<Match> GenerateSchedule(IList<Team> teams)
        {
            // Пока что возвращаем пустой список — UI покажет сообщение "в разработке".
            return new List<Match>();
        }
    }

    public class SwissScheduleStrategy : BaseStubScheduleStrategy
    {
        public SwissScheduleStrategy() : base("Швейцарский") { }
    }

    public class OlympicScheduleStrategy : BaseStubScheduleStrategy
    {
        public OlympicScheduleStrategy() : base("Олимпийский") { }
    }

    public class StagedScheduleStrategy : BaseStubScheduleStrategy
    {
        public StagedScheduleStrategy() : base("Поэтапный") { }
    }
}


