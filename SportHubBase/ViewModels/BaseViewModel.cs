// ViewModels/BaseViewModel.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SportHubBase.ViewModels
{
    /// Абстрактный базовый класс для всех ViewModels в MVVM.
    /// Реализует INotifyPropertyChanged для уведомлений UI о изменениях свойств (биндинг в WPF).
    /// Включает встроенный RelayCommand для команд (ICommand) — без внешних библиотек, для простоты.
    /// В архитектуре: Наследуется всеми VM (e.g. TournamentViewModel); свойства в производных классах вызывают OnPropertyChanged.
    /// Связь: VM оборачивают Models (e.g. ObservableCollection<Match> для Matches), вызывают Services (JsonStorage для сохранения, Scheduling для генерации).
    /// Улучшение: Если нужно async-команды — добавить AsyncRelayCommand позже (без await в C# 7.3, через Task.Run).
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        /// Событие для уведомления о изменении свойств (стандарт MVVM для WPF-биндинга).
        public event PropertyChangedEventHandler PropertyChanged;

        /// Защищённый метод для вызова уведомления. Использует CallerMemberName для автоматического имени свойства.
        /// Пример использования в VM: set { _prop = value; OnPropertyChanged(); } — обновит UI автоматически.
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Встроенный RelayCommand — без отдельного файла
        /// Вложенная реализация ICommand для команд в VM (e.g. кнопки в Views).
        /// Поддерживает execute (действие) и canExecute (условие); авто-обновление через CommandManager.
        /// В MVVM: Используется для команд вроде public ICommand GenerateScheduleCommand { get; } = new RelayCommand(param => Generate());
        /// Связь: Команды вызывают бизнес-логику (e.g. фабрику стратегий, storage.Save), с уведомлениями для UI.
        /// Улучшение: Для параметризованных — использовать object parameter; добавить RaiseCanExecuteChanged если нужно ручное обновление.
        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            /// Конструктор без canExecute (всегда true).
            public RelayCommand(Action<object> execute)
                : this(execute, null) { }

            /// Конструктор с execute и canExecute.
            public RelayCommand(Action<object> execute, Predicate<object> canExecute)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            /// Проверяет, можно ли выполнить команду.
            public bool CanExecute(object parameter)
                => _canExecute == null || _canExecute(parameter);

            /// Выполняет команду.
            public void Execute(object parameter)
                => _execute(parameter);

            // Автоматическое обновление кнопок при изменении свойств
            /// Событие для уведомления об изменении CanExecute (привязка к CommandManager.RequerySuggested).
            /// Обеспечивает авто-обновление UI (e.g. кнопка disabled если canExecute false).

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}