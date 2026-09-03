// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;

using Sarafan.Core.Observability;
using Sarafan.Core.RestModels;

namespace Sarafan.Core.Services;

public sealed class SarafanProblemDetailsFactory(
    ILogger<SarafanProblemDetailsFactory>? logger = null)
{
    public const string MediaType = "application/problem+json";
    public const string TypeBase = "https://sarafan.sw.consulting/problems/";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly IReadOnlyDictionary<string, ProblemDefinition> Definitions =
        new Dictionary<string, ProblemDefinition>(StringComparer.Ordinal)
        {
            ["validation_failed"] = new(
                StatusCodes.Status400BadRequest,
                "Ошибка проверки данных",
                "Проверьте корректность указанных данных."),
            ["invalid_phone"] = new(
                StatusCodes.Status400BadRequest,
                "Некорректный номер телефона",
                "Укажите корректный номер телефона."),
            ["invalid_purpose"] = new(
                StatusCodes.Status400BadRequest,
                "Некорректное назначение запроса",
                "Назначение запроса должно быть register или login."),
            ["consent_required"] = new(
                StatusCodes.Status400BadRequest,
                "Требуется согласие",
                "Необходимо принять условия и согласие на обработку персональных данных."),
            ["invalid_photo_size"] = new(
                StatusCodes.Status400BadRequest,
                "Некорректный размер фотографии",
                "Размер фотографии должен быть от 1 байта до 5 МиБ."),
            ["invalid_photo_type"] = new(
                StatusCodes.Status400BadRequest,
                "Неподдерживаемый формат фотографии",
                "Используйте фотографию в формате JPEG, PNG или WebP."),
            ["invalid_photo_content"] = new(
                StatusCodes.Status400BadRequest,
                "Некорректное содержимое фотографии",
                "Содержимое фотографии не соответствует указанному формату."),
            ["invalid_code"] = new(
                StatusCodes.Status401Unauthorized,
                "Некорректный код подтверждения",
                "Код подтверждения неверен или срок его действия истёк."),
            ["invalid_access_token"] = new(
                StatusCodes.Status401Unauthorized,
                "Недействительный токен доступа",
                "Войдите в систему повторно."),
            ["invalid_refresh_token"] = new(
                StatusCodes.Status401Unauthorized,
                "Сеанс завершён",
                "Сеанс истёк или больше недействителен. Войдите в систему повторно."),
            ["login_failed"] = new(
                StatusCodes.Status401Unauthorized,
                "Не удалось войти",
                "Номер телефона или код подтверждения неверен."),
            ["access_denied"] = new(
                StatusCodes.Status403Forbidden,
                "Доступ запрещён",
                "У вас нет прав для выполнения этого действия."),
            ["customer_not_found"] = new(
                StatusCodes.Status404NotFound,
                "Пользователь не найден",
                "Запрошенный пользователь не найден."),
            ["photo_not_found"] = new(
                StatusCodes.Status404NotFound,
                "Фотография не найдена",
                "Фотография пользователя ещё не загружена."),
            ["account_exists"] = new(
                StatusCodes.Status409Conflict,
                "Учётная запись уже существует",
                "Для этого номера телефона уже зарегистрирована учётная запись."),
            ["rate_limited"] = new(
                StatusCodes.Status429TooManyRequests,
                "Слишком много запросов",
                "Повторите попытку позже."),
            ["internal_error"] = new(
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка",
                "Не удалось обработать запрос. Повторите попытку позже."),
            ["verification_unavailable"] = new(
                StatusCodes.Status503ServiceUnavailable,
                "Подтверждение временно недоступно",
                "Сервис подтверждения телефона временно недоступен."),
            ["resource_not_found"] = new(
                StatusCodes.Status404NotFound,
                "Ресурс не найден",
                "Запрошенный ресурс не найден."),
            ["method_not_allowed"] = new(
                StatusCodes.Status405MethodNotAllowed,
                "Метод не поддерживается",
                "Этот метод нельзя использовать для запрошенного ресурса."),
            ["request_too_large"] = new(
                StatusCodes.Status413PayloadTooLarge,
                "Запрос слишком большой",
                "Уменьшите размер отправляемых данных."),
            ["unsupported_media_type"] = new(
                StatusCodes.Status415UnsupportedMediaType,
                "Неподдерживаемый формат данных",
                "Используйте поддерживаемый формат данных запроса."),
            ["bad_request"] = new(
                StatusCodes.Status400BadRequest,
                "Некорректный запрос",
                "Проверьте параметры запроса."),
            ["service_unavailable"] = new(
                StatusCodes.Status503ServiceUnavailable,
                "Сервис временно недоступен",
                "Повторите попытку позже.")
        };

    public SarafanProblemDetails Create(
        HttpContext context,
        int statusCode,
        string code,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (!Definitions.TryGetValue(code, out var definition)
            || definition.StatusCode != statusCode)
        {
            code = "internal_error";
            definition = Definitions[code];
        }

        var traceId = SarafanTraceIdentifiers.GetOrCreate(context);
        context.Response.Headers.ContentLanguage = "ru";
        var details = new SarafanProblemDetails
        {
            Type = $"{TypeBase}{code.Replace('_', '-')}",
            Title = definition.Title,
            Status = definition.StatusCode,
            Detail = definition.Detail,
            Instance = $"urn:sarafan:problem:{traceId}",
            Code = code,
            Errors = errors,
            TraceId = traceId
        };
        SarafanEvents.ProblemEmitted(
            logger ?? NullLogger<SarafanProblemDetailsFactory>.Instance,
            details.Status.Value,
            details.Type,
            details.Code,
            details.Instance,
            traceId);
        return details;
    }

    public ObjectResult CreateResult(HttpContext context, int statusCode, string code)
        => Result(Create(context, statusCode, code));

    public ObjectResult CreateValidationResult(HttpContext context, ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(item => item.Value?.ValidationState == ModelValidationState.Invalid)
            .ToDictionary(
                item => JsonNamingPolicy.CamelCase.ConvertName(item.Key),
                item => ValidationMessages(item.Value),
                StringComparer.Ordinal);
        return Result(Create(
            context,
            StatusCodes.Status400BadRequest,
            "validation_failed",
            errors));
    }

    public async ValueTask WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        CancellationToken cancellationToken = default)
    {
        var details = Create(context, statusCode, code);
        context.Response.StatusCode = details.Status!.Value;
        context.Response.ContentType = MediaType;
        context.Response.Headers.ContentLanguage = "ru";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            details,
            SerializerOptions,
            cancellationToken);
    }

    public static string CodeForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "bad_request",
        StatusCodes.Status401Unauthorized => "invalid_access_token",
        StatusCodes.Status403Forbidden => "access_denied",
        StatusCodes.Status404NotFound => "resource_not_found",
        StatusCodes.Status405MethodNotAllowed => "method_not_allowed",
        StatusCodes.Status413PayloadTooLarge => "request_too_large",
        StatusCodes.Status415UnsupportedMediaType => "unsupported_media_type",
        StatusCodes.Status429TooManyRequests => "rate_limited",
        StatusCodes.Status503ServiceUnavailable => "service_unavailable",
        >= 500 and < 600 => "internal_error",
        _ => "internal_error"
    };

    private static ObjectResult Result(SarafanProblemDetails details)
    {
        var result = new ObjectResult(details)
        {
            StatusCode = details.Status
        };
        result.ContentTypes.Add(MediaType);
        return result;
    }

    private static string[] ValidationMessages(ModelStateEntry? entry)
    {
        if (entry is null || entry.Errors.Count == 0)
        {
            return ["Значение заполнено некорректно."];
        }

        return entry.Errors
            .Select(error => error.Exception is null && ContainsCyrillic(error.ErrorMessage)
                ? error.ErrorMessage.Trim()
                : "Значение заполнено некорректно.")
            .ToArray();
    }

    private static bool ContainsCyrillic(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Any(character => character is >= '\u0400' and <= '\u04ff');

    private sealed record ProblemDefinition(int StatusCode, string Title, string Detail);
}
