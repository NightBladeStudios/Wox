namespace Wox.Infrastructure.Hotkey
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Plugin;

    /// <summary>
    /// Listens keyboard globally.
    /// <remarks>Uses WH_KEYBOARD_LL.</remarks>
    /// </summary>
    public class GlobalHotkey : IDisposable
    {
        public event KeyboardCallback hookedKeyboardCallback;

        public delegate bool KeyboardCallback(KeyEvent keyEvent, int vkCode, SpecialKeyState state);

        public static GlobalHotkey Instance
        {
            get
            {
                if (instance == null) instance = new GlobalHotkey();
                return instance;
            }
        }

        //Modifier key constants
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_ALT = 0x12;
        private const int VK_WIN = 91;
        private static GlobalHotkey instance;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            var continues = true;

            if (nCode >= 0)
                if (wParam.ToUInt32() == (int) KeyEvent.WM_KEYDOWN ||
                    wParam.ToUInt32() == (int) KeyEvent.WM_KEYUP ||
                    wParam.ToUInt32() == (int) KeyEvent.WM_SYSKEYDOWN ||
                    wParam.ToUInt32() == (int) KeyEvent.WM_SYSKEYUP)
                    if (hookedKeyboardCallback != null)
                        continues = hookedKeyboardCallback((KeyEvent) wParam.ToUInt32(), Marshal.ReadInt32(lParam), CheckModifiers());

            if (continues) return InterceptKeys.CallNextHookEx(hookId, nCode, wParam, lParam);
            return (IntPtr) 1;
        }

        private readonly InterceptKeys.LowLevelKeyboardProc hookedLowLevelKeyboardProc;
        private readonly IntPtr hookId = IntPtr.Zero;

        private GlobalHotkey()
        {
            // We have to store the LowLevelKeyboardProc, so that it is not garbage collected runtime
            hookedLowLevelKeyboardProc = LowLevelKeyboardProc;
            // Set the hook
            hookId = InterceptKeys.SetHook(hookedLowLevelKeyboardProc);
        }

        #region Public

        public SpecialKeyState CheckModifiers()
        {
            var state = new SpecialKeyState();
            if ((InterceptKeys.GetKeyState(VK_SHIFT) & 0x8000) != 0)
                //SHIFT is pressed
                state.ShiftPressed = true;
            if ((InterceptKeys.GetKeyState(VK_CONTROL) & 0x8000) != 0)
                //CONTROL is pressed
                state.CtrlPressed = true;
            if ((InterceptKeys.GetKeyState(VK_ALT) & 0x8000) != 0)
                //ALT is pressed
                state.AltPressed = true;
            if ((InterceptKeys.GetKeyState(VK_WIN) & 0x8000) != 0)
                //WIN is pressed
                state.WinPressed = true;

            return state;
        }

        public void Dispose()
        {
            InterceptKeys.UnhookWindowsHookEx(hookId);
        }

        #endregion

        #region NONE

        ~GlobalHotkey()
        {
            Dispose();
        }

        #endregion
    }
}