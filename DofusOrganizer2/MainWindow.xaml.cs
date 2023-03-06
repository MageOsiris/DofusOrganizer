using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;

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

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //(none)
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        //CAPS LOCK:
        private const uint LEFT = 0x25;
        private const uint UP = 0x26;
        private const uint RIGHT = 0x27;
        private const uint X1 = 0x05;
        private const uint X2 = 0x06;

        private IntPtr _windowHandle;
        private HwndSource _source;

        private List<DofusElement> dofusElements = new List<DofusElement>();

        private int currentIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnDetectClick(object sender, RoutedEventArgs e)
        {
            dofusElements = new List<DofusElement>();

            Process[] processCollection = Process.GetProcesses().Where(x => x.ProcessName == "Dofus").ToArray();

            Canvas canvas;
            Label label;
            TextBox textBox;
            CheckBox checkBox;

            var cpt = 1;

            foreach (Process p in processCollection)
            {
                if (cpt != 8)
                {
                    canvas = (Canvas)FindName("Account" + cpt);
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

                            dofusElements.Add(new DofusElement() { Process = p, Name = p.MainWindowTitle, Rank = 0, Cpt = (cpt + 1).ToString() });
                        }
                    }
                    cpt++;
                }
            }
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, LEFT);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, RIGHT);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, UP);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, X1);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, X2);
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
                            if (vkey == LEFT || vkey == X2)
                            {
                                GoNextWindow(true);
                            }
                            else if (vkey == RIGHT || vkey == X1)
                            {
                                GoNextWindow(false);
                            }
                            else if (vkey == UP)
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
                    currentIndex = dofusElements.Count - 1;
                else if (isAscend)
                    currentIndex--;
                else if (!isAscend && currentIndex == (dofusElements.Count - 1))
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
                SetForegroundWindow(dofusElements[currentIndex].Process.MainWindowHandle);
                ShowWindow(dofusElements[currentIndex].Process.MainWindowHandle, SW_MAXIMIZE);
            }
        }

        private void GoCurrentWindow()
        {
            SetForegroundWindow(dofusElements[currentIndex].Process.MainWindowHandle);
            ShowWindow(dofusElements[currentIndex].Process.MainWindowHandle, SW_MAXIMIZE);
        }

        private class DofusElement
        {
            public Process Process;
            public string Name;
            public int Rank;
            public string Cpt;
            public bool Enable;
        }

        protected override void OnClosed(EventArgs e)
        {
            //_source.RemoveHook(HwndHook);
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
    }
}
