namespace Wox
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using Helper;
    using Image;
    using Infrastructure;

    public partial class Msg : Window
    {
        private bool closing;
        private readonly Storyboard fadeOutStoryboard = new Storyboard();

        public Msg()
        {
            InitializeComponent();
            var screen = Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            var dipWorkingArea = WindowsInteropHelper.TransformPixelsToDIP(this,
                screen.WorkingArea.Width,
                screen.WorkingArea.Height);
            Left = dipWorkingArea.X - Width;
            Top = dipWorkingArea.Y;
            showAnimation.From = dipWorkingArea.Y;
            showAnimation.To = dipWorkingArea.Y - Height;

            // Create the fade out storyboard
            fadeOutStoryboard.Completed += fadeOutStoryboard_Completed;
            var fadeOutAnimation = new DoubleAnimation(dipWorkingArea.Y - Height, dipWorkingArea.Y, new Duration(TimeSpan.FromSeconds(5)))
            {
                AccelerationRatio = 0.2
            };
            Storyboard.SetTarget(fadeOutAnimation, this);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(TopProperty));
            fadeOutStoryboard.Children.Add(fadeOutAnimation);

            imgClose.Source = ImageLoader.Load(Path.Combine(Constant.ProgramDirectory, "Images\\close.png"));
            imgClose.MouseUp += imgClose_MouseUp;
        }

        #region Public

        public void Show(string title, string subTitle, string iconPath)
        {
            tbTitle.Text = title;
            tbSubTitle.Text = subTitle;
            if (string.IsNullOrEmpty(subTitle)) tbSubTitle.Visibility = Visibility.Collapsed;
            if (!File.Exists(iconPath))
                imgIco.Source = ImageLoader.Load(Path.Combine(Constant.ProgramDirectory, "Images\\app.png"));
            else
                imgIco.Source = ImageLoader.Load(iconPath);

            Show();

            Dispatcher.InvokeAsync(async () =>
            {
                if (!closing)
                {
                    closing = true;
                    await Dispatcher.InvokeAsync(fadeOutStoryboard.Begin);
                }
            });
        }

        #endregion

        #region Private

        private void imgClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!closing)
            {
                closing = true;
                fadeOutStoryboard.Begin();
            }
        }

        private void fadeOutStoryboard_Completed(object sender, EventArgs e)
        {
            Close();
        }

        #endregion
    }
}