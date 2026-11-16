using System;

namespace SportHubBase.Services
{
    public sealed class StorageConflictException : InvalidOperationException
    {
        public StorageConflictException() : base("Турнир изменён в другом окне или на сервере. Изменения не сохранены. Обновите турнир и повторите ввод.") { }
    }
}
