namespace Hubbly.Mobile.Services;

public class NullKeyboardService : IKeyboardService
{
    public void HideKeyboard()
    {
        // No-op implementation for non-Android platforms
        // On platforms like iOS, Windows, etc., keyboard handling is different
        // and may not need explicit hiding or is handled by the OS
    }
}
