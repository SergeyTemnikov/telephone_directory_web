using Microsoft.JSInterop;

namespace PhoneDirectoryBlazor.Services.Storage
{
    public interface ILocalStorageService
    {
        Task<T?> GetItemAsync<T>(string key);
        Task SetItemAsync<T>(string key, T value);
        Task RemoveItemAsync(string key);
        Task ClearAsync();
    }

    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            return await _js.InvokeAsync<T?>("localStorage.getItem", key);
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", key, value);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }

        public async Task ClearAsync()
        {
            await _js.InvokeVoidAsync("localStorage.clear");
        }
    }
}
