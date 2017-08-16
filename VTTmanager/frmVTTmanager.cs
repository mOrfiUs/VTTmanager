using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;

namespace VTTmanager
{

    public class cVTT : Form
    {
        private DialogResult showDialog(Form f, Form frm)
        {
            if (frm.InvokeRequired)
            {
                DialogResult dr = DialogResult.Cancel;
                frm.Invoke((MethodInvoker)delegate() { dr = showDialog(f, frm); });
                return dr;
            }
            else
                return f.ShowDialog((IWin32Window)frm);
        }

        public cVTT()
        {
            this.BackColor = Color.White;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new System.Drawing.Size(1, 1);
            this.Activated += cVTT_Activated;
        }

        private void inputVTT()
        {
            string sVoid = string.Empty;
            int iId = 0;
            bool bGo = false;
            if (DialogResult.OK == new util().modernInputBox(this, Application.ProductName + Environment.NewLine + Environment.NewLine + "Alpha Version, just for test" + Environment.NewLine + "VTT id?", sVoid, SystemIcons.Question, out sVoid, false))
                if (Int32.TryParse(sVoid, out iId))
                    if (iId > 0)
                        bGo = true;
            sVoid = string.Empty;
            if (bGo)
                if (DialogResult.OK != new util().modernInputBox(this, Application.ProductName + Environment.NewLine + Environment.NewLine + "Alpha Version, just for test" + Environment.NewLine + "VTT name?", sVoid, SystemIcons.Question, out sVoid, false))
                    bGo = false;
                else
                    if (string.IsNullOrEmpty(sVoid))
                        bGo = false;
            if (bGo)
                dotpVtt2Srt(sVoid, new int[] { iId });
        }

        private void cVTT_Activated(object sender, System.EventArgs e)
        {
            this.Activated -= cVTT_Activated;
            inputVTT();
            this.Close();
        }

        private string dotpVtt2Srt(string title, int[] iIds)
        {
            int iCountEpisode = 0;
            string sTrasBox = string.Empty;

            foreach (int idVídeo in iIds)
                using (trasbox tb = new trasbox())
                {
                    string titEpisode = title + (++iCountEpisode).ToString("00");
                    string s = tb.getTrasBox(idVídeo, true);
                    if (string.IsNullOrEmpty(s))
                        continue;

                    foreach (string[] arrTmp in new string[][] {
                            new string[]{".spa.vtt", s.Replace("\n", Environment.NewLine)},
                        })
                        if (!string.IsNullOrEmpty(arrTmp[1]))
                        {
                            string sTestLf = arrTmp[1].Replace("\r\n", "");
                            if (sTestLf.Contains("\n"))
                                Debugger.Break();//sin Lf
                            if (sTestLf.Contains("\r"))
                                Debugger.Break();//sin Cr
                            using (TextWriter tw = new StreamWriter(Path.Combine(Application.StartupPath, titEpisode) + arrTmp[0], false, Encoding.GetEncoding(1252)))
                                tw.Write(arrTmp[1]);
                        }
                }
            return "correcto";
        }
    }

    internal sealed class util
    {
        public string UserAgent { get { return "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.65 Safari/537.36"; } }

        public void ForceForegroundWindow(IntPtr hWnd)
        {
            uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(),
                IntPtr.Zero);
            uint appThread = GetCurrentThreadId();
            const uint SW_SHOW = 5;

            if (foreThread != appThread)
            {
                AttachThreadInput(foreThread, appThread, true);
                BringWindowToTop(hWnd);
                ShowWindow(hWnd, SW_SHOW);
                AttachThreadInput(foreThread, appThread, false);
            }
            else
            {
                BringWindowToTop(hWnd);
                ShowWindow(hWnd, SW_SHOW);
            }
        }

        public DialogResult modernInputBox(Form frm, string sTitle, string sIn, Icon inpuBoxIcon, out string sOut, bool isMsgBox = false)
        {
            string fName = "frmInputBox";
            DialogResult dr = DialogResult.Cancel;
            sOut = string.Empty;
            int factorH = sTitle.Split('\n').Length;
            int factorW = 0;
            foreach (string sLine in sTitle.Split('\n'))
                if (factorW < sLine.Length)
                    factorW = sLine.Length;
            int frmHeight = 200 + (factorH * 16);
            int frmWidth = 400 + (factorW * 4);
            if (frmHeight > 1000)
                frmHeight = 1000;
            if (frmWidth > 800)
                frmWidth = 800;
            using (Form f = new Form() { Icon = frm.Icon, ControlBox = false, Name = fName, BackColor = Color.White, Font = new Font("Segoe UI", 11F), ShowIcon = false, ShowInTaskbar = false, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedToolWindow, Width = frmWidth, Height = frmHeight, StartPosition = FormStartPosition.CenterScreen })
            {
                Label l = new Label() { Left = 30, Top = 20, Text = sTitle, BackColor = f.BackColor, AutoSize = true };
                Button b = new Button() { Text = "Ok", Left = f.Width - 260, Width = 100, Top = frmHeight - 50, Height = 30, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = true };
                Button c = new Button() { Text = "Cancel", Left = b.Left + b.Width + 30, Width = 100, Top = b.Top, Height = 30, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = true };
                TextBox t = new TextBox() { Left = 40, Top = frmHeight - 100, Width = c.Left + c.Width - 40, Height = 40, Text = sIn, BackColor = f.BackColor };
                PictureBox pb = new PictureBox() { SizeMode = PictureBoxSizeMode.AutoSize, Left = f.Width - 50, Top = 20, Image = (Image)inpuBoxIcon.ToBitmap() };
                b.Click += (sender, e) => { f.DialogResult = DialogResult.OK; f.Close(); };
                c.Click += (sender, e) => { f.Close(); };
                f.MouseDown += (sender, e) =>
                {
                    const int HT_CAPTION = 0x2;
                    const int WM_NCLBUTTONDOWN = 0xA1;

                    if (e.Button == MouseButtons.Left)
                    {
                        ReleaseCapture();
                        SendMessage(new HandleRef(null, ((Control)sender).Handle), (IntPtr)WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
                    }
                };
                f.Paint += (sender, e) =>
                {
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(255, 0, 174, 219)))
                        e.Graphics.FillRectangle(sb, new Rectangle(0, 0, ((Control)sender).Width, 5));
                };
                EventHandler ehActivated = null;
                ehActivated = (sender, e) =>
                {
                    f.Activated -= ehActivated;
                    ForceForegroundWindow(f.Handle);
                    //FlashWindowEx(f);
                };
                f.Activated += ehActivated;

                f.Controls.AddRange(new Control[] { t, b, c, l, pb });
                f.AcceptButton = b;
                f.CancelButton = c;
                if (isMsgBox)
                    t.Visible = false;
                dr = showDialog(f, frm);
                //if (dr == DialogResult.OK) incluso en Cancel devuelve el valor del texto
                sOut = t.Text;
            }
            return dr;
        }

        private DialogResult showDialog(Form f, Form frm)
        {
            if (frm.InvokeRequired)
            {
                DialogResult dr = DialogResult.Cancel;
                frm.Invoke((MethodInvoker)delegate() { dr = showDialog(f, frm); });
                return dr;
            }
            else
                return f.ShowDialog((IWin32Window)frm);
        }

        #region NativeWindows

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool ReleaseCapture();

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SendMessage(HandleRef hWnd, IntPtr msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        [ComVisible(false)]
        internal struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        [ComVisible(false)]
        internal enum FlashWindow : int
        {
            /// <summary>
            /// Stop flashing. The system restores the window to its original state. 
            /// </summary>    
            FLASHW_STOP = 0,

            /// <summary>
            /// Flash the window caption 
            /// </summary>
            FLASHW_CAPTION = 1,

            /// <summary>
            /// Flash the taskbar button. 
            /// </summary>
            FLASHW_TRAY = 2,

            /// <summary>
            /// Flash both the window caption and taskbar button.
            /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
            /// </summary>
            FLASHW_ALL = 3,

            /// <summary>
            /// Flash continuously, until the FLASHW_STOP flag is set.
            /// </summary>
            FLASHW_TIMER = 4,

            /// <summary>
            /// Flash continuously until the window comes to the foreground. 
            /// </summary>
            FLASHW_TIMERNOFG = 12
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // When you don't want the ProcessId, use this overload and pass 
        // IntPtr.Zero for the second parameter
        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        /// The GetForegroundWindow function returns a handle to the 
        /// foreground window.
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool BringWindowToTop(HandleRef hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, uint uFlags);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        #endregion NativeWindows


    }

    internal class trasbox : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~trasbox()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        public trasbox()
        {

        }

        private string finishWReq(IAsyncResult wRes, HttpWebRequest wReq, bool bLink = false)
        {
            string s = string.Empty;
            using (HttpWebResponse wr = (HttpWebResponse)wReq.EndGetResponse(wRes))
                if (wr.StatusCode == HttpStatusCode.OK)
                    using (Stream st = wr.GetResponseStream())
                    using (StreamReader sr = new StreamReader(st))
                        s = HttpUtility.HtmlDecode(sr.ReadToEnd());
            MatchCollection mc = new Regex(@"<src>(.*?)</src>", RegexOptions.IgnoreCase).Matches(s);
            if (!bLink)
                if (mc.Count > 0)
                    s = mc[0].Groups[1].Value;
            //if (string.IsNullOrEmpty(s))                throw new Exception("error");
            syncDown.Set();
            return s;
        }

        private ManualResetEvent syncDown = new ManualResetEvent(false);

        public string getTrasBox(int idVídeo, bool vtt = false, string url = "")
        {
            string sWeb = string.Empty;
            //foreach (char c in sWeb) Debug.Write((int)c + " ");
            foreach (int i in new int[] { 114, 116, 118, 101 })
                sWeb += (char)i;

            string sUri = @"http://www." + sWeb + @".es/api/videos/" + idVídeo.ToString() + (vtt ? "/subtitulos" : "") + ".xml";
            HttpWebRequest wReq = (HttpWebRequest)WebRequest.Create(new Uri(string.IsNullOrEmpty(url) ? sUri : url));

            wReq.Timeout = 200000;
            wReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
            wReq.Method = "GET";
            wReq.UserAgent = new util().UserAgent;
            wReq.Accept = "*/*";
            wReq.AllowAutoRedirect = true;
            string sRes = string.Empty;
            try
            {
                wReq.BeginGetResponse(wRes => { sRes = finishWReq(wRes, wReq, !string.IsNullOrEmpty(url)); }, null);
            }
            catch (Exception ex)
            {
                syncDown.Reset();
                return sRes;
            }
            syncDown.WaitOne();
            Application.DoEvents();
            syncDown.Reset();
            if (string.IsNullOrEmpty(sRes))
                return string.Empty;
            //throw new Exception("error");

            if (string.IsNullOrEmpty(url))
                sRes = getTrasBox(0, vtt, sRes);
            return sRes;
        }
    }
}
