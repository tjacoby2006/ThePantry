using Microsoft.JSInterop;

namespace ThePantry.Services;

public class ToastService
{
    private readonly IJSRuntime _jsRuntime;

    public ToastService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ShowSuccess(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showToast", message, "success");
    }

    public async Task ShowError(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showToast", message, "danger");
    }

    public async Task ShowInfo(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showToast", message, "info");
    }

    public async Task ShowWarning(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showToast", message, "warning");
    }
}
