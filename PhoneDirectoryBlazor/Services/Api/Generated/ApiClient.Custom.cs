namespace PhoneDirectoryBlazor.Services.Api
{
    public static class ApiClientBaseUrlFix
    {
        public static void ApplyBaseUrlFix<T>(T client, HttpClient http)
            where T : class
        {
            if (http.BaseAddress != null)
                (client as dynamic).BaseUrl = http.BaseAddress.ToString().TrimEnd('/');
        }
    }

    public partial class AuthClient
    {
        partial void Initialize()
        {
            ApiClientBaseUrlFix.ApplyBaseUrlFix(this, _httpClient);
        }
    }

    public partial class DepartmentsClient
    {
        partial void Initialize()
        {
            ApiClientBaseUrlFix.ApplyBaseUrlFix(this, _httpClient);
        }
    }

    public partial class EmployeesClient
    {
        partial void Initialize()
        {
            ApiClientBaseUrlFix.ApplyBaseUrlFix(this, _httpClient);
        }
    }

    public partial class UsersClient
    {
        partial void Initialize()
        {
            ApiClientBaseUrlFix.ApplyBaseUrlFix(this, _httpClient);
        }
    }

    public partial class OrganizationClient
    {
        partial void Initialize()
        {
            ApiClientBaseUrlFix.ApplyBaseUrlFix(this, _httpClient);
        }
    }

    public partial class AdminClient
    {
        partial void Initialize()
        {
            ApiClientBaseUrlFix.ApplyBaseUrlFix(this, _httpClient);
        }
    }
}