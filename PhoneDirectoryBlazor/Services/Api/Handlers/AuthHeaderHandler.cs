// Services/Api/AuthHeaderHandler.cs
using PhoneDirectoryBlazor.Services.Auth;
using PhoneDirectoryBlazor.Services.Storage;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneDirectoryBlazor.Services.Api.Handlers;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static bool _isRefreshing;

    public AuthHeaderHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 🔹 Добавляем токен
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // 🔹 Если получили 401 — НЕ вызываем рефреш здесь!
        // Просто пробрасываем 401 дальше, и компонент/сервис сам решит, что делать
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return response; // 🔹 Просто возвращаем 401
        }

        return response;
    }
}