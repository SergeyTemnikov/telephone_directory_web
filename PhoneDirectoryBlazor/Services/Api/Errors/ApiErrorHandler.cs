using PhoneDirectoryBlazor.Services.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhoneDirectoryBlazor.Services.Api.Errors
{
    public class ApiErrorHandler : IApiErrorHandler
    {
        private static readonly Dictionary<string, string> FieldNames = new()
        {
            { "email", "Email" },
            { "password", "Пароль" },
            { "full_name", "ФИО" },
            { "address", "Адрес" },
            { "comment", "Комментарий" },
            { "end_date_of_access", "Дата окончания доступа" },
            { "department_id", "Отдел" },
            { "manager_id", "Руководитель" },
            { "phone_number", "Номер телефона" },
            { "name", "Название" }
        };

        private static readonly Dictionary<string, string> ValidationMessages = new()
        {
            { "required", "обязательное поле" },
            { "email", "некорректный формат email" },
            { "min", "слишком короткое значение" },
            { "max", "слишком длинное значение" },
            { "minLength", "должно содержать минимум {0} символов" },
            { "maxLength", "должно содержать не более {0} символов" },
            { "gt", "должно быть больше {0}" },
            { "date_format", "неверный формат даты" }
        };

        public string GetErrorMessage(ApiException ex)
        {
            return ex.StatusCode switch
            {
                401 => "Неверный логин или пароль, либо аккаунт отключён",
                403 => "Доступ запрещён",
                404 => "Ресурс не найден",
                409 => ParseConflictError(ex),
                422 => ParseValidationError(ex),
                500 => "Внутренняя ошибка сервера",
                _ => $"Ошибка: {ex.Message}"
            };
        }

        public bool IsValidationError(ApiException ex) => ex.StatusCode == 422;

        private string ParseValidationError(ApiException ex)
        {
            if (string.IsNullOrWhiteSpace(ex.Response))
                return "Ошибка валидации данных";

            try
            {
                using var doc = JsonDocument.Parse(ex.Response);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("details", out var details) &&
                    details.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();

                    foreach (var prop in details.EnumerateObject())
                    {
                        var fieldName = LocalizeFieldName(prop.Name);
                        var validationMsg = LocalizeValidationMessage(prop.Value.GetString());
                        messages.Add($"{fieldName}: {validationMsg}");
                    }

                    if (messages.Any())
                        return string.Join("; ", messages);
                }

                if (root.TryGetProperty("error", out var err) &&
                    err.TryGetProperty("message", out var msg))
                {
                    return $"{msg.GetString()}";
                }
            }
            catch
            {

            }

            return "Ошибка валидации данных";
        }

        private string ParseConflictError(ApiException ex)
        {
            if (string.IsNullOrWhiteSpace(ex.Response))
                return "Конфликт данных";

            try
            {
                using var doc = JsonDocument.Parse(ex.Response);
                if (doc.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("code", out var code))
                {
                    return code.GetString() switch
                    {
                        "user_exists" => "Пользователь с таким Email уже существует",
                        "email_exists" => "Сотрудник с таким Email уже существует",
                        "position_exists" => "Должность с таким названием уже существует",
                        "parent_already_has_child" => "У отдела уже есть подчинённый отдел",
                        "role_already_assigned" => "Эта роль уже назначена пользователю",
                        _ => "Конфликт данных"
                    };
                }
            }
            catch { }

            return "Конфликт данных";
        }

        private string LocalizeFieldName(string fieldName)
        {
            var key = FieldNames.Keys.FirstOrDefault(k =>
                k.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (key != null && FieldNames.TryGetValue(key, out var localized))
                return localized;

            return char.ToUpper(fieldName[0]) + fieldName[1..];
        }

        private string LocalizeValidationMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "некорректное значение";

            var lower = message.ToLowerInvariant();

            if (lower.Contains("at least") && lower.Contains("characters"))
            {
                var parts = message.Split(' ');
                var idx = Array.FindIndex(parts, p => p == "least");
                if (idx >= 0 && idx + 1 < parts.Length && int.TryParse(parts[idx + 1], out var num))
                    return $"должно содержать минимум {num} символов";
            }

            if (lower.Contains("no more than") && lower.Contains("characters"))
            {
                var parts = message.Split(' ');
                var idx = Array.FindIndex(parts, p => p == "than");
                if (idx >= 0 && idx + 1 < parts.Length && int.TryParse(parts[idx + 1], out var num))
                    return $"должно содержать не более {num} символов";
            }

            foreach (var kvp in ValidationMessages)
            {
                if (lower.Contains(kvp.Key))
                {
                    var msg = kvp.Value;

                    if (msg.Contains("{0}"))
                    {
                        var numbers = System.Text.RegularExpressions.Regex.Matches(message, @"\d+")
                            .Select(m => m.Value).FirstOrDefault();
                        if (!string.IsNullOrEmpty(numbers))
                            return string.Format(msg, numbers);
                    }
                    return msg;
                }
            }

            return message switch
            {
                var m when m.Contains("required") => "обязательное поле",
                var m when m.Contains("email") => "некорректный формат email",
                var m when m.Contains("date") => "неверный формат даты",
                _ => message
            };
        }
    }
}