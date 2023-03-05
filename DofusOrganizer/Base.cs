using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DofusOrganizer
{
    public partial class Base : Form
    {
        KeyboardHook hookLeft = new KeyboardHook();
        KeyboardHook hookRight = new KeyboardHook();
        KeyboardHook hookCurrent = new KeyboardHook();

        List<DofusElement> dofusElements = new List<DofusElement>();

        int currentIndex = 0;

        public Base()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dofusElements = new List<DofusElement> ();

            Process[] processCollection = Process.GetProcesses().Where(x => x.ProcessName == "Dofus").ToArray();

            Label[] labels = Controls.OfType<Label>().Where(x => x.AccessibleName is not null).Where(x => x.AccessibleName.Contains("Account")).ToArray();
            NumericUpDown[] textBoxes = Controls.OfType<NumericUpDown>().Where(x => x.Name.Contains("textBox")).ToArray();

            NumericUpDown textBox;
            Label label;

            var cpt = 0;

            foreach (Process p in processCollection)
            {
                if (cpt != 7)
                {
                    label = labels.FirstOrDefault(x => x.AccessibleName == "Account" + (cpt + 1));
                    textBox = textBoxes.FirstOrDefault(x => x.Name == "textBox" + (cpt + 1));

                    label.Text = p.MainWindowTitle.Split(" - ")[0];

                    dofusElements.Add(new DofusElement() { Process = p, Name = p.MainWindowTitle, Rank = 0, Cpt = (cpt+1).ToString() });

                    label.Visible = true;
                    textBox.Visible = true;
                    cpt++;
                }

                Console.WriteLine(p.ProcessName);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            hookLeft.Dispose();
            hookRight.Dispose();
            hookCurrent.Dispose();
            hookLeft = new KeyboardHook();
            hookRight = new KeyboardHook();
            hookCurrent = new KeyboardHook();

            NumericUpDown[] textBoxes = Controls.OfType<NumericUpDown>().Where(x => x.Name.Contains("textBox")).ToArray();

            foreach (NumericUpDown p in textBoxes)
            {
                DofusElement dofusElement = dofusElements.FirstOrDefault(x => x.Cpt == p.Name.Split("textBox")[1]);
                if (dofusElement is not null)
                {
                    dofusElement.Rank = (int)p.Value;
                }
            }

            dofusElements = dofusElements.OrderBy(x => x.Rank).ThenBy(x => x.Name).ToList();

            // register the event that is fired after the key press.
            hookRight.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(Descend);
            // register the control + alt + F12 combination as hot key.
            hookRight.RegisterHotKey(DofusOrganizer.ModifierKeys.None, Keys.Right);

            // register the event that is fired after the key press.
            hookLeft.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(Ascend);
            // register the control + alt + F12 combination as hot key.
            hookLeft.RegisterHotKey(DofusOrganizer.ModifierKeys.None, Keys.Left);

            // register the event that is fired after the key press.
            hookCurrent.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(Current);
            // register the control + alt + F12 combination as hot key.
            hookCurrent.RegisterHotKey(DofusOrganizer.ModifierKeys.Control, Keys.Up);

        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MAXIMIZE = 3;

        private void Ascend(object sender, KeyPressedEventArgs e)
        {
            GoNextWindow(true);
        }

        private void Descend(object sender, KeyPressedEventArgs e)
        {
            GoNextWindow(false);
        }

        private void Current(object sender, KeyPressedEventArgs e)
        {
            SetForegroundWindow(dofusElements[currentIndex].Process.MainWindowHandle);
            ShowWindow(dofusElements[currentIndex].Process.MainWindowHandle, SW_MAXIMIZE);
        }

        public void GoNextWindow(bool isAscend)
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
    }

    public class DofusElement
    {
        public Process Process;
        public string Name;
        public int Rank;
        public string Cpt;
    }
}
