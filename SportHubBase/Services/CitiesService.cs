// Services/CitiesService.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SportHubBase.Services
{
    /// Сервис для загрузки списка городов из JSON файла.
    public class CitiesService
    {
        /// Путь к файлу с городами (в базовой директории приложения).
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cities.json");

        /// Загружает список городов из JSON. Если файл не существует — возвращает пустой список.
        public List<string> LoadCities()
        {
            if (!File.Exists(_filePath))
            {
                return new List<string>();
            }

            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
        }
    }
}