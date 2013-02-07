using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SideMirror
{

    [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }

    public partial class MainForm : Form
    {

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        private bool IsCapturing;

        const Int32 CURSOR_SHOWING = 0x00000001;

        static Screen Target;

        private CancellationTokenSource cancelToken = new CancellationTokenSource();

        public MainForm()
        {
            InitializeComponent();
        }

        public static Bitmap CaptureScreen()
        {
            Bitmap result = new Bitmap(Target.Bounds.Width, Target.Bounds.Height, PixelFormat.Format24bppRgb);

            if(Target == null)
                Target = Screen.AllScreens.ElementAt(1);

            try
            {
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.CopyFromScreen(Target.Bounds.Left, Target.Bounds.Top, 0, 0, Target.Bounds.Size, CopyPixelOperation.SourceCopy);

                    CURSORINFO pci;
                    pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CURSORINFO));

                    if (GetCursorInfo(out pci))
                    {
                        if (pci.flags == CURSOR_SHOWING)
                        {
                            DrawIcon(g.GetHdc(), pci.ptScreenPos.x - Target.Bounds.Left - 9, pci.ptScreenPos.y - Target.Bounds.Top - 9, pci.hCursor);
                            g.ReleaseHdc();
                        }
                    }
                }
            }
            catch
            {
                result = null;
            }

            return result;
        }

        object Lock = new object();

        private void btnCapture_Click(object sender, EventArgs e)
        {
            if (IsCapturing)
            {
                    IsCapturing = false;
                    btnCapture.Text = "Start Capture";
            }
            else if (Screen.AllScreens.Count() == 1)
            {
                MessageBox.Show("No secondary monitor detected");
                btnCapture.Text = "Start Capture";
                IsCapturing = false;
            }
            else
            {
                btnCapture.Text = "Stop Capture";
                IsCapturing = true;

                Task.Factory.StartNew(() =>
                {
                    while (IsCapturing)
                    {
                        GC.Collect();
                        pctScreen.Image = CaptureScreen();
                        Thread.Sleep(Convert.ToInt32(1000 / numFPS.Value));

                    }

                    pctScreen.Image = null;
                }

                );

            }
        }

        private void chkAlwaysOnTop_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = chkAlwaysOnTop.Checked;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            foreach (Screen scr in Screen.AllScreens)
                lstScreens.Items.Add(scr.DeviceName);

            lstScreens.SelectedIndex = 1;
        }

        private void lstScreens_SelectedIndexChanged(object sender, EventArgs e)
        {
            Target = Screen.AllScreens.FirstOrDefault(scr => scr.DeviceName == lstScreens.Text);
        }

    }
}
