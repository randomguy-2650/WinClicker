using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace WinClicker.Services
{
    public class SettingsService
    {
        private readonly string _filePath;
        private readonly Dictionary<string, object> _settings;

        public SettingsService()
        {
            string folder = AppContext.BaseDirectory;

            Directory.CreateDirectory(folder);

            _filePath = Path.Combine(folder, "settings.json");

            _settings = File.Exists(_filePath)
                ? JsonSerializer.Deserialize(File.ReadAllText(_filePath), AppJsonContext.Default.DictionaryStringObject) ?? []
                : [];
        }

        public void Save(string key, object value)
        {
            _settings[key] = value;

            File.WriteAllText(_filePath, JsonSerializer.Serialize(_settings, AppJsonContext.Default.DictionaryStringObject));
        }

        public void Load<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T viewModel) where T : class
        {
            var type = typeof(T);

            foreach (var property in type.GetProperties())
            {
                if (_settings.TryGetValue(property.Name, out var savedValue) && savedValue is JsonElement element)
                {
                    object? value = property.PropertyType switch
                    {
                        Type typ when typ == typeof(string) => element.GetString(),
                        Type typ when typ == typeof(int) => element.ValueKind == JsonValueKind.Number ? element.GetInt32() : 0,
                        Type typ when typ == typeof(double) => element.GetDouble(),
                        Type typ when typ == typeof(bool) => element.GetBoolean(),
                        _ => null
                    };

                    if (value != null)
                    {
                        property.SetValue(viewModel, value);
                    }
                }
            }
        }
    }
}
