using PhoneDirectoryBlazor.Services.Api;

namespace PhoneDirectoryBlazor.Services.Api.Errors
{
    public interface IApiErrorHandler
    {
        string GetErrorMessage(ApiException ex);

        bool IsValidationError(ApiException ex);
    }
}