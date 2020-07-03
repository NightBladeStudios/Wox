namespace Wox
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Core.Resource;
    using Infrastructure.Hotkey;
    using NHotkey.Wpf;

    public partial class HotkeyControl : UserControl
    {
        public event EventHandler HotkeyChanged;
        public HotkeyModel CurrentHotkey { get; private set; }
        public bool CurrentHotkeyAvailable { get; private set; }

        public new bool IsFocused => tbHotkey.IsFocused;

        public HotkeyControl()
        {
            InitializeComponent();
        }

        #region Public

        public void SetHotkey(HotkeyModel keyModel, bool triggerValidate = true)
        {
            CurrentHotkey = keyModel;

            tbHotkey.Text = CurrentHotkey.ToString();
            tbHotkey.Select(tbHotkey.Text.Length, 0);

            if (triggerValidate)
            {
                CurrentHotkeyAvailable = CheckHotkeyAvailability();
                if (!CurrentHotkeyAvailable)
                {
                    tbMsg.Foreground = new SolidColorBrush(Colors.Red);
                    tbMsg.Text = InternationalizationManager.Instance.GetTranslation("hotkeyUnavailable");
                }
                else
                {
                    tbMsg.Foreground = new SolidColorBrush(Colors.Green);
                    tbMsg.Text = InternationalizationManager.Instance.GetTranslation("success");
                }

                tbMsg.Visibility = Visibility.Visible;
                OnHotkeyChanged();
            }
        }

        public void SetHotkey(string keyStr, bool triggerValidate = true)
        {
            SetHotkey(new HotkeyModel(keyStr), triggerValidate);
        }

        #endregion

        #region Protected

        protected virtual void OnHotkeyChanged()
        {
            var handler = HotkeyChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion

        #region Private

        private void TbHotkey_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            tbMsg.Visibility = Visibility.Hidden;

            //when alt is pressed, the real key should be e.SystemKey
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            var specialKeyState = GlobalHotkey.Instance.CheckModifiers();

            var hotkeyModel = new HotkeyModel(
                specialKeyState.AltPressed,
                specialKeyState.ShiftPressed,
                specialKeyState.WinPressed,
                specialKeyState.CtrlPressed,
                key);

            var hotkeyString = hotkeyModel.ToString();

            if (hotkeyString == tbHotkey.Text) return;

            Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(500);
                SetHotkey(hotkeyModel);
            });
        }

        private bool CheckHotkeyAvailability()
        {
            try
            {
                HotkeyManager.Current.AddOrReplace("HotkeyAvailabilityTest", CurrentHotkey.CharKey, CurrentHotkey.ModifierKeys, (sender, e) => { });

                return true;
            }
            catch
            {
            }
            finally
            {
                HotkeyManager.Current.Remove("HotkeyAvailabilityTest");
            }

            return false;
        }

        #endregion
    }
}