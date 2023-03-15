using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using DofusOrganizer2.Models;
using DofusOrganizer2.Enums;

namespace DofusOrganizer2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MAXIMIZE = 3;
        private const int HOTKEY_ID = 9000;

        private IntPtr _windowHandle;
        private HwndSource _source;

        private List<DofusElement> dofusElements = new List<DofusElement>();
        private List<DofusElement> filterDofusElements = new List<DofusElement>();

        private int currentIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
        }

        private SliderValue LeftValue = SliderValue.ArrowKeys;
        private SliderValue RightValue = SliderValue.ArrowKeys;
        private SliderValue CenterValue = SliderValue.Nothing;

        private enum SliderValue
        {
            ArrowKeys = 0,
            Both = 1,
            FKeys = 2,
            Nothing = 3
        }

        private enum SliderType
        {
            Left = 0,
            Middle = 1,
            Right = 2
        }

        private void OnDetectClick(object sender, RoutedEventArgs e)
        {
            // Reset layout

            Canvas canvas;
            Label label;
            TextBox textBox;
            CheckBox checkBox;

            int canvasCpt = 1;

            for (canvasCpt = 1; canvasCpt <= 8; canvasCpt++)
            {
                canvas = (Canvas)FindName("Account" + canvasCpt);
                if (canvas is not null)
                {
                    canvas.Visibility = Visibility.Visible;
                    label = FindChild<Label>(canvas, "");
                    if (label is not null)
                    {
                        label.Content = "Account" + canvasCpt;
                    }
                    textBox = FindChild<TextBox>(canvas, "");
                    if (textBox is not null)
                    {
                        textBox.Text = "0";
                    }
                    checkBox = FindChild<CheckBox>(canvas, "");
                    if (checkBox is not null)
                    {
                        checkBox.IsChecked = true;
                    }
                }
            }

            Label labelInformation = (Label)FindName("Information");

            Label labelLeftBase = (Label)FindName("LeftInfoBase");
            Label labelRightBase = (Label)FindName("RightInfoBase");
            Label labelMiddleBase = (Label)FindName("CenterInfoBase");

            Label labelLeft = (Label)FindName("LeftInfo");
            Label labelRight = (Label)FindName("RightInfo");
            Label labelMiddle = (Label)FindName("CenterInfo");
            Slider sliderLeft = (Slider)FindName("LeftSlider");
            Slider sliderRight = (Slider)FindName("RightSlider");
            Slider sliderMiddle = (Slider)FindName("CenterSlider");
            Button buttonApply = (Button)FindName("Apply");

            if (labelInformation is null
                || labelLeft is null
                || labelRight is null
                || labelMiddle is null
                || buttonApply is null)
            {
                // Todo throw exception
                return;
            }

            labelInformation.Visibility = Visibility.Hidden;
            labelLeft.Visibility = Visibility.Hidden;
            labelRight.Visibility = Visibility.Hidden;
            labelMiddle.Visibility = Visibility.Hidden;

            labelLeftBase.Visibility = Visibility.Hidden;
            labelRightBase.Visibility = Visibility.Hidden;
            labelMiddleBase.Visibility = Visibility.Hidden;

            sliderLeft.Visibility = Visibility.Hidden;
            sliderRight.Visibility = Visibility.Hidden;
            sliderMiddle.Visibility = Visibility.Hidden;
            buttonApply.Visibility = Visibility.Hidden;

            dofusElements = new List<DofusElement>();

            Process[] processCollection = Process.GetProcesses().Where(x => x.ProcessName == "Dofus").ToArray();

            if (processCollection is null || processCollection.Length == 0)
            {
                labelInformation.Content = "No dofus instances detected";
                labelInformation.Visibility = Visibility.Visible;
            }
            else if (processCollection.Length > 8)
            {
                labelInformation.Content = "Too much dofus instances detected. Fuck you xoxo";
                labelInformation.Visibility = Visibility.Visible;
            }
            else
            {
                labelInformation.Content = "All of your dofus instance(s)";
                labelInformation.Visibility = Visibility.Visible;

                GetSliderValueData data = GetSliderValue(LeftValue, SliderType.Left);
                labelLeft.Content = data.SliderLabel;
                data = GetSliderValue(CenterValue, SliderType.Middle);
                labelMiddle.Content = data.SliderLabel;
                data = GetSliderValue(RightValue, SliderType.Right);
                labelRight.Content = data.SliderLabel;

                labelLeft.Visibility = Visibility.Visible;
                labelRight.Visibility = Visibility.Visible;
                labelMiddle.Visibility = Visibility.Visible;

                labelLeftBase.Visibility = Visibility.Visible;
                labelRightBase.Visibility = Visibility.Visible;
                labelMiddleBase.Visibility = Visibility.Visible;

                buttonApply.Visibility = Visibility.Visible;

                sliderLeft.Visibility = Visibility.Visible;
                sliderRight.Visibility = Visibility.Visible;
                sliderMiddle.Visibility = Visibility.Visible;


                canvasCpt = 1;

                foreach (Process p in processCollection)
                {
                    if (canvasCpt != 8)
                    {
                        canvas = (Canvas)FindName("Account" + canvasCpt);
                        if (canvas is not null)
                        {
                            label = FindChild<Label>(canvas, "");
                            textBox = FindChild<TextBox>(canvas, "");
                            checkBox = FindChild<CheckBox>(canvas, "");

                            if (label is not null
                                && textBox is not null
                                && checkBox is not null)
                            {
                                label.Content = p.MainWindowTitle.Split(" - ")[0];

                                canvas.Visibility = Visibility.Visible;

                                dofusElements.Add(new DofusElement() { Process = p, Name = p.MainWindowTitle, Rank = 0, Cpt = canvasCpt.ToString() });
                            }
                        }
                        canvasCpt++;
                    }
                }
            }
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            Canvas canvas;
            Label label;
            TextBox textBox;
            CheckBox checkBox;

            foreach (DofusElement dofusElement in dofusElements)
            {
                canvas = (Canvas)FindName("Account" + dofusElement.Cpt);
                if (canvas is not null && canvas.Visibility == Visibility.Visible)
                {
                    textBox = FindChild<TextBox>(canvas, "");
                    checkBox = FindChild<CheckBox>(canvas, "");

                    if (textBox is not null
                        && checkBox is not null)
                    {
                        dofusElement.Enable = checkBox.IsChecked ?? false;
                        dofusElement.Rank = int.TryParse(textBox.Text, out int value) ? value : 0;
                    }
                }
            }

            filterDofusElements = dofusElements.Where(x => x.Enable).OrderBy(x => x.Rank).ThenBy(x => x.Name).ToList();

            UnregisterHotKey(_windowHandle, HOTKEY_ID);

            if (LeftValue == SliderValue.ArrowKeys || LeftValue == SliderValue.Both)
                RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)ModifiersKeys.NONE, (uint)Keys.LEFT);
            if (RightValue == SliderValue.ArrowKeys || RightValue == SliderValue.Both)
                RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)ModifiersKeys.NONE, (uint)Keys.RIGHT);
            if (CenterValue == SliderValue.ArrowKeys || CenterValue == SliderValue.Both)
                RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)ModifiersKeys.NONE, (uint)Keys.UP);
            if (CenterValue == SliderValue.FKeys || LeftValue == SliderValue.Both)
                RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)ModifiersKeys.CONTROL, (uint)Keys.F10);
            if (LeftValue == SliderValue.FKeys || LeftValue == SliderValue.Both)
                RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)ModifiersKeys.CONTROL, (uint)Keys.F11);
            if (RightValue == SliderValue.FKeys || LeftValue == SliderValue.Both)
                RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)ModifiersKeys.CONTROL, (uint)Keys.F12);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == (uint)Keys.LEFT || vkey == (uint)Keys.F11)
                            {
                                GoNextWindow(true);
                            }
                            else if (vkey == (uint)Keys.RIGHT || vkey == (uint)Keys.F12)
                            {
                                GoNextWindow(false);
                            }
                            else if (vkey == (uint)Keys.UP || vkey == (uint)Keys.F10)
                            {
                                GoCurrentWindow();
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void GoNextWindow(bool isAscend)
        {
            if (currentIndex > -1)
            {
                if (isAscend && currentIndex == 0)
                    currentIndex = filterDofusElements.Count - 1;
                else if (isAscend)
                    currentIndex--;
                else if (!isAscend && currentIndex == (filterDofusElements.Count - 1))
                    currentIndex = 0;
                else
                    currentIndex++;
            }
            else
            {
                currentIndex = 0;
            }

            if (currentIndex > -1)
            {
                SetForegroundWindow(filterDofusElements[currentIndex].Process.MainWindowHandle);
                ShowWindow(filterDofusElements[currentIndex].Process.MainWindowHandle, SW_MAXIMIZE);
            }
        }

        private void GoCurrentWindow()
        {
            if (currentIndex == -1)
                currentIndex = 0;
            SetForegroundWindow(filterDofusElements[currentIndex].Process.MainWindowHandle);
            ShowWindow(filterDofusElements[currentIndex].Process.MainWindowHandle, SW_MAXIMIZE);
        }

        protected override void OnClosed(EventArgs e)
        {
            _source?.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        private void SliderLeft_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Label label = (Label)FindName("LeftInfo");
            GetSliderValueData data = GetSliderValue((int)e.NewValue, SliderType.Left);
            LeftValue = data.Value;
            label.Content = data.SliderLabel;
        }

        private void SliderRight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Label label = (Label)FindName("RightInfo");
            GetSliderValueData data = GetSliderValue((int)e.NewValue, SliderType.Right);
            RightValue = data.Value;
            label.Content = data.SliderLabel;
        }

        private void SliderCenter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Label label = (Label)FindName("CenterInfo");
            GetSliderValueData data = GetSliderValue((int)e.NewValue, SliderType.Middle);
            CenterValue = data.Value;
            label.Content = data.SliderLabel;
        }

        private GetSliderValueData GetSliderValue(int value, SliderType type)
        {
            switch (value)
            {
                case (int)SliderValue.Nothing:
                    return new GetSliderValueData() { Value = SliderValue.FKeys, SliderLabel = "Unset" };
                case (int)SliderValue.FKeys:
                    return new GetSliderValueData() { Value = SliderValue.FKeys, SliderLabel = (type == SliderType.Left ? "F11 key" : (type == SliderType.Right ? "F12 key" : "F10 key")) };
                case (int)SliderValue.Both:
                    return new GetSliderValueData() { Value = SliderValue.Both, SliderLabel = (type == SliderType.Left ? "F11 or < key" : (type == SliderType.Right ? "F12 or > key" : "F10 or ^ key")) };
                case (int)SliderValue.ArrowKeys:
                default:
                    return new GetSliderValueData() { Value = SliderValue.ArrowKeys, SliderLabel = (type == SliderType.Left ? "< key" : (type == SliderType.Right ? "> key" : "^ key")) };
            }
        }

        private GetSliderValueData GetSliderValue(SliderValue value, SliderType type)
        {
            return GetSliderValue((int)value, type);
        }

        private class GetSliderValueData 
        {
            public SliderValue Value { get; set; }
            public string SliderLabel { get; set; }

        }
    }
}
