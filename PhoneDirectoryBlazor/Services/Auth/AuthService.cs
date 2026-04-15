using Microsoft.AspNetCore.Components;
using PhoneDirectoryBlazor.Services.Api;
using PhoneDirectoryBlazor.Services.Storage;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhoneDirectoryBlazor.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RefreshAsync(RefreshRequest request);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
    }

    // Services/Auth/AuthService.cs

    public class AuthService : IAuthService
    {
        private readonly IAuthClient _authClient;              // с обработчиком (для Login)
        private readonly IHttpClientFactory _httpClientFactory; // 🔹 для создания клиента без обработчика
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _navigation;
        private readonly CustomAuthenticationStateProvider _authStateProvider;

        public AuthService(
            IAuthClient authClient,
            IHttpClientFactory httpClientFactory,  // 🔹 НОВЫЙ параметр
            ILocalStorageService localStorage,
            NavigationManager navigation,
            CustomAuthenticationStateProvider authStateProvider)
        {
            _authClient = authClient;
            _httpClientFactory = httpClientFactory;
            _localStorage = localStorage;
            _navigation = navigation;
            _authStateProvider = authStateProvider;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var response = await _authClient.LoginAsync(request);
            if (response.Data == null) throw new Exception("Login failed");

            await _localStorage.SetItemAsync("authToken", response.Data.Access_token);
            await _localStorage.SetItemAsync("refreshToken", response.Data.Refresh_token);
            _authStateProvider.MarkUserAsAuthenticated(response.Data.Access_token);

            return response.Data;
        }

        public async Task<LoginResponse> RefreshAsync(RefreshRequest request)
        {
            try
            {
                // 🔹 Используем сгенерированный клиент — он сам сериализует запрос/ответ
                var response = await _authClient.RefreshAsync(request);

                // 🔹 Проверяем, что данные есть
                if (response?.Data == null)
                {
                    throw new ApiException(
                        message: "Refresh failed: no data in response",
                        statusCode: 500,
                        response: string.Empty,
                        headers: new Dictionary<string, IEnumerable<string>>(),
                        innerException: null);
                }

                var result = response.Data;

                // 🔹 Сохраняем новые токены
                await _localStorage.SetItemAsync("authToken", result.Access_token);
                await _localStorage.SetItemAsync("refreshToken", result.Refresh_token);

                return result;
            }
            catch (ApiException ex) when (ex.StatusCode is 401 or 403)
            {
                // 🔹 Если рефреш-токен не валиден — чистим хранилище
                await _localStorage.RemoveItemAsync("authToken");
                await _localStorage.RemoveItemAsync("refreshToken");
                throw; // Пробрасываем ошибку дальше для обработки в хендлере
            }
            catch (Exception ex)
            {
                // 🔹 Логируем или пробрасываем другие ошибки
                throw new ApiException(
                    message: $"Refresh error: {ex.Message}",
                    statusCode: 500,
                    response: string.Empty,
                    headers: new Dictionary<string, IEnumerable<string>>(),
                    innerException: ex);
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            _authStateProvider.MarkUserAsLoggedOut();
            _navigation.NavigateTo("/login");
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            return !string.IsNullOrEmpty(token);
        }
    }
}
