using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Watching_YouTube
{
    public partial class MainWindow : Window
    {
        public int MaxHour = 0;
        public int MaxMinute = 0;
        public int MaxSecond = 0;

        public int Hour = 0;
        public int Minute = 0;
        public int Second = 0;

        public DispatcherTimer Timer = new DispatcherTimer();
        public NotifyIcon TrayIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            var screen = Screen.PrimaryScreen.Bounds;
            Height = screen.Height;
            Width = screen.Width;
            Debug.WriteLine(screen.Height);
            Debug.WriteLine(screen.Width);

            var dir = new DirectoryInfo("config");
            if (!dir.Exists)
                dir.Create();

            if (!File.Exists("config/config.cfg"))
                File.WriteAllText("config/config.cfg", "2\n28\n19");
            var config = File.ReadAllLines("config/config.cfg");

            MaxHour = Convert.ToInt32(config[0]);
            MaxMinute = Convert.ToInt32(config[1]);
            MaxSecond = Convert.ToInt32(config[2]);

            var maxValue = MaxSecond;
            for (int i = 0; i < MaxMinute; i++)
                maxValue += 60;
            for (int i = 0; i < MaxHour; i++)
                maxValue += 3600;

            TimeSlider.Maximum = maxValue;
            TimeSlider.Minimum = 0;
            TimeSlider.Value = 0;

            TimeText.Text = $"{Minute}:{Second:D2} / {MaxMinute}:{MaxSecond:D2}";

            #region Tray Icon
            var menu = new ContextMenu();
            var exitMenu = new MenuItem();
            exitMenu.Index = 0;
            exitMenu.Text = "종료";

            exitMenu.Click += delegate (object click, EventArgs eClick)
            {
                Process.GetCurrentProcess().Kill();
            };

            menu.MenuItems.Add(exitMenu);

            TrayIcon.Icon = Properties.Resources.icon;
            TrayIcon.Visible = true;
            TrayIcon.ContextMenu = menu;
            TrayIcon.Text = "Watching YouTube";
            #endregion

            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        #region Timer
        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSlider.Value++;

            if (TimeSlider.Maximum == TimeSlider.Value)
            {
                Timer.Stop();
                var uri = new Uri("pack://application:,,,/Resources/play.png");
                PauseButton.Source = new BitmapImage(uri);
            }
            else
            {
                Timer.Start();
                var uri = new Uri("pack://application:,,,/Resources/pause.png");
                PauseButton.Source = new BitmapImage(uri);
            }
        }
        #endregion

        #region 창 로드될 때
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }
        #endregion

        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        #region 재생/일시 정지 버튼 클릭
        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Timer.IsEnabled)
            {
                Timer.Stop();
                var uri = new Uri("pack://application:,,,/Resources/play.png");
                PauseButton.Source = new BitmapImage(uri);
            }
            else
            {
                Timer.Start();
                var uri = new Uri("pack://application:,,,/Resources/pause.png");
                PauseButton.Source = new BitmapImage(uri);
            }
        }
        #endregion

        #region 슬라이더 값 변경 시
        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var tmp = new DateTime(1970, 1, 1, 0, 0, 0);
            tmp = tmp.AddSeconds(e.NewValue);
            Hour = tmp.Hour;
            Minute = tmp.Minute;
            Second = tmp.Second;

            if (MaxHour == 0)
                TimeText.Text = $"{Minute}:{Second:D2} / {MaxMinute}:{MaxSecond:D2}";
            else
                TimeText.Text = $"{Hour}:{Minute:D2}:{Second:D2} / {MaxHour}:{MaxMinute:D2}:{MaxSecond:D2}";
        }
        #endregion
    }
}
