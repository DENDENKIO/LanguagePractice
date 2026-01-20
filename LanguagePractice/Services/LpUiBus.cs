using System;

namespace LanguagePractice.Services
{
    public static class LpUiBus
    {
        public static event Action? LibraryInvalidated;

        public static void InvalidateLibrary()
        {
            LibraryInvalidated?.Invoke();
        }
    }
}
