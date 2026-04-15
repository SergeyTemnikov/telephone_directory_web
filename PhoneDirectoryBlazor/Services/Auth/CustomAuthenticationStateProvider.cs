using Microsoft.AspNetCore.Components.Authorization;
using PhoneDirectoryBlazor.Services.Storage;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(_anonymous);

        var identity = CreateClaimsIdentityFromJwt(token);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void MarkUserAsAuthenticated(string token)
    {
        var identity = CreateClaimsIdentityFromJwt(token);
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void MarkUserAsLoggedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private ClaimsIdentity CreateClaimsIdentityFromJwt(string jwt)
    {
        try
        {
            // простая декодировка payload (без валидации) — для UI claims
            var parts = jwt.Split('.');
            if (parts.Length < 2) return new ClaimsIdentity();

            var payload = parts[1];
            var json = Base64UrlDecode(payload);
            var claimsData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            var claims = new List<Claim>();
            if (claimsData != null)
            {
                foreach (var kv in claimsData)
                {
                    // стандартные поля
                    if (kv.Value == null) continue;
                    var value = kv.Value.ToString() ?? "";
                    if (kv.Key == "roles" || kv.Key == "role")
                    {
                        // если массив ролей
                        if (value.StartsWith("["))
                        {
                            var arr = JsonSerializer.Deserialize<string[]>(value);
                            if (arr != null) foreach (var r in arr) claims.Add(new Claim(ClaimTypes.Role, r));
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, value));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(kv.Key, value));
                    }
                }
            }
            return new ClaimsIdentity(claims, "jwt");
        }
        catch
        {
            return new ClaimsIdentity();
        }
    }

    private static string Base64UrlDecode(string input)
    {
        string s = input;
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        var bytes = Convert.FromBase64String(s);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
