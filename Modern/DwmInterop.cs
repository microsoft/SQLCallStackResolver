// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    internal static class DwmInterop {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS {
            public int Left, Right, Top, Bottom;
        }

        // DWMWA_USE_IMMERSIVE_DARK_MODE (Win10 build 18985+)
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        // DWMWA_SYSTEMBACKDROP_TYPE (Win11 22H2+)
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        // DWMWA_MICA_EFFECT (Win11 pre-22H2)
        private const int DWMWA_MICA_EFFECT = 1029;
        // DWMWA_WINDOW_CORNER_PREFERENCE (Win11)
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        internal static void ApplyImmersiveDarkMode(Window window, bool isDark) {
            try {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;
                int value = isDark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
            } catch { /* best effort — not supported on older OS */ }
        }

        internal static void ApplyMicaOrAcrylic(Window window) {
            try {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Make the WPF window background transparent so the DWM backdrop shows through
                window.Background = Brushes.Transparent;
                var mainWindowSrc = HwndSource.FromHwnd(hwnd);
                if (mainWindowSrc?.CompositionTarget != null)
                    mainWindowSrc.CompositionTarget.BackgroundColor = Colors.Transparent;

                // Extend frame into client area
                var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);

                // Try DWMWA_SYSTEMBACKDROP_TYPE first (Win11 22H2+): 2 = Mica, 3 = Acrylic, 4 = Tabbed
                int backdropType = 2; // Mica
                int result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                if (result != 0) {
                    // Fallback: DWMWA_MICA_EFFECT (Win11 pre-22H2)
                    int micaValue = 1;
                    DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref micaValue, sizeof(int));
                }
            } catch { /* best effort — not supported on older OS */ }
        }

        internal static void ApplyRoundedCorners(Window window) {
            try {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;
                int preference = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            } catch { /* best effort — Win11 only */ }
        }

        internal static bool IsSystemDarkTheme() {
            try {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                if (value is int intVal) return intVal == 0;
            } catch { /* fall through */ }
            return false;
        }
    }
}
