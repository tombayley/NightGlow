using NightGlow.ViewModels;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NightGlow.Views;

public partial class PopupWindow : Window
{

    private const double ANIM_TIME = 0.3;
    private const double DISPLAY_TIME = 2;
    private const int BOTTOM_OFFSET = 16; // Offest above the taskbar / bottom of screen

    private DispatcherTimer hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(DISPLAY_TIME) };

    private DoubleAnimation _slideDownAnimation;
    private EventHandler _slideDownCompleteHandler;

    private STATE state = STATE.HIDDEN;

    private enum STATE
    {
        HIDDEN = 1,
        APPEARING = 2,
        ACTIVE = 3,
        HIDING = 4
    }

    public PopupWindow(PopupViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        hideTimer.Tick += (s, args) =>
        {
            hideTimer.Stop();
            AnimateOut();
        };

        this.Loaded += Window_Loaded;
    }

    public void ShowPopup()
    {
        switch (state)
        {
            case STATE.HIDDEN:
                Debug.WriteLine("Show()");
                Show();
                AnimateIn();
                break;
            case STATE.APPEARING:
            case STATE.ACTIVE:
                ResetHideTimer();
                break;
            case STATE.HIDING:
                _slideDownAnimation.Completed -= _slideDownCompleteHandler;
                AnimateIn();
                break;
        }
    }

    private void ResetHideTimer()
    {
        hideTimer.Interval = TimeSpan.FromSeconds(DISPLAY_TIME);
    }

    private void AnimateIn()
    {
        state = STATE.APPEARING;
        ResetHideTimer();

        var workArea = SystemParameters.WorkArea;
        this.Left = (workArea.Width - this.Width) / 2;
        this.Top = workArea.Bottom - this.Height - BOTTOM_OFFSET;

        var slideAnimation = new DoubleAnimation
        {
            From = this.ActualHeight,
            To = 0,
            Duration = TimeSpan.FromSeconds(ANIM_TIME),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
        };
        slideAnimation.Completed += (s, args) => state = STATE.ACTIVE;
        AnimatedContainerTransform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);

        hideTimer.Start();
    }

    private void AnimateOut()
    {
        state = STATE.HIDING;

        _slideDownAnimation = new DoubleAnimation
        {
            From = 0,
            To = this.ActualHeight,
            Duration = TimeSpan.FromSeconds(ANIM_TIME),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseIn }
        };
        _slideDownCompleteHandler = (s, args) => HideNow();
        _slideDownAnimation.Completed += _slideDownCompleteHandler;
        AnimatedContainerTransform.BeginAnimation(TranslateTransform.YProperty, _slideDownAnimation);
    }

    private void HideNow()
    {
        state = STATE.HIDDEN;
        this.Hide();
    }

    private const int GWL_EX_STYLE = -20, WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080, WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(this).Handle;
        int currentStyle = GetWindowLong(helper, GWL_EX_STYLE);
        // Hide from Alt + Tab (WS_EX_TOOLWINDOW & WS_EX_APPWINDOW)
        // Prevent Window from taking focus when showing (WS_EX_NOACTIVATE)
        int newStyle = (currentStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE) & ~WS_EX_APPWINDOW;
        SetWindowLong(helper, GWL_EX_STYLE, newStyle);
    }

}
