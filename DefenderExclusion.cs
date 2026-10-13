using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;

// ============================================
//  AMBIGUITY RESOLVERS
// ============================================
using Color = System.Drawing.Color;
using Brush = System.Drawing.Brush;
using Pen = System.Drawing.Pen;
using FontFamily = System.Drawing.FontFamily;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using ColorBlend = System.Drawing.Drawing2D.ColorBlend;
using GraphicsPath = System.Drawing.Drawing2D.GraphicsPath;
using StringFormat = System.Drawing.StringFormat;
using SolidBrush = System.Drawing.SolidBrush;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

// ===== Assembly Info =====
[assembly: AssemblyTitle("Defender Exclusion Manager")]
[assembly: AssemblyProduct("Defender Exclusion Manager")]
[assembly: AssemblyCompany("Sanil")]
[assembly: AssemblyCopyright("© 2026 Sanil")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace DefenderExclusion
{
    // =====================================================
    //  🎨 FORM ICON HELPER
    // =====================================================
    public static class FormHelper
    {
        public static void SetFormIcon(Form form)
        {
            try
            {
                string appDir = Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location);
                string icoPath = Path.Combine(appDir, "icon.ico");

                if (File.Exists(icoPath))
                {
                    form.Icon = new Icon(icoPath);
                }
            }
            catch { }
        }
    }

    // =====================================================
    //  🎬 SPLASH FORM (1280×720, VIDEO)
    // =====================================================
    public class SplashForm : Form
    {
        private ElementHost host;
        private System.Windows.Controls.MediaElement media;
        private Label btnSkip;

        public event EventHandler SkipClicked;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 720);
            BackColor = Color.Black;
            ShowInTaskbar = false;
            TopMost = true;

            FormHelper.SetFormIcon(this);

            // Rounded corners
            var path = new GraphicsPath();
            int r = 28;
            path.AddArc(0, 0, r, r, 180, 90);
            path.AddArc(Width - r, 0, r, r, 270, 90);
            path.AddArc(Width - r, Height - r, r, r, 0, 90);
            path.AddArc(0, Height - r, r, r, 90, 90);
            path.CloseAllFigures();
            Region = new Region(path);

            // VIDEO
            media = new System.Windows.Controls.MediaElement();
            media.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
            media.UnloadedBehavior = System.Windows.Controls.MediaState.Manual;
            media.Stretch = System.Windows.Media.Stretch.UniformToFill;
            media.IsMuted = false;
            media.Volume = 1.0;

            string videoPath = Path.Combine(AppFolder, "splash.mp4");
            if (File.Exists(videoPath))
            {
                media.Source = new Uri(videoPath);

                media.MediaEnded += (s, e) =>
                {
                    try
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (SkipClicked != null)
                                SkipClicked(this, EventArgs.Empty);
                            this.Close();
                        });
                    }
                    catch { }
                };

                media.MediaFailed += (s, e) =>
                {
                    try
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (SkipClicked != null)
                                SkipClicked(this, EventArgs.Empty);
                            this.Close();
                        });
                    }
                    catch { }
                };

                media.Play();
            }
            else
            {
                var t = new Timer();
                t.Interval = 3000;
                t.Tick += (s, e) =>
                {
                    t.Stop(); t.Dispose();
                    if (SkipClicked != null)
                        SkipClicked(this, EventArgs.Empty);
                    Close();
                };
                t.Start();
            }

            host = new ElementHost();
            host.Dock = DockStyle.Fill;
            host.Child = media;
            Controls.Add(host);

            // SKIP BUTTON
            btnSkip = new Label();
            btnSkip.Text = "Skip ▶";
            btnSkip.Font = new Font("Segoe UI", 12f,
                                    FontStyle.Bold | FontStyle.Underline);
            btnSkip.ForeColor = Color.FromArgb(251, 191, 36);
            btnSkip.BackColor = Color.FromArgb(180, 30, 30, 46);
            btnSkip.TextAlign = ContentAlignment.MiddleCenter;
            btnSkip.Size = new Size(110, 32);
            btnSkip.Location = new Point(Width - 130, Height - 45);
            btnSkip.Cursor = Cursors.Hand;

            btnSkip.MouseEnter += (s, e) =>
            {
                btnSkip.ForeColor = Color.FromArgb(253, 224, 71);
            };
            btnSkip.MouseLeave += (s, e) =>
            {
                btnSkip.ForeColor = Color.FromArgb(251, 191, 36);
            };

            btnSkip.Click += (s, e) =>
            {
                try { media.Stop(); media.Close(); } catch { }
                if (SkipClicked != null)
                    SkipClicked(this, EventArgs.Empty);
                this.Close();
            };
            btnSkip.BringToFront();
            Controls.Add(btnSkip);

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    try { media.Stop(); media.Close(); } catch { }
                    if (SkipClicked != null)
                        SkipClicked(this, EventArgs.Empty);
                    this.Close();
                }
            };
        }

        private string AppFolder
        {
            get
            {
                return Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (media != null)
                {
                    media.Stop();
                    media.Close();
                }
            }
            catch { }
            base.OnFormClosed(e);
        }
    }

    // =====================================================
    //  🏠 MAIN FORM (HUD + icon + slider + toggle)
    // =====================================================
    public class MainForm : Form
    {
        private TextBox txtPath;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnRefresh;
        private ListBox lstList;
        private Label lblStatus;
        private Label lblCredit;

        private ElementHost hudHost;
        private System.Windows.Controls.MediaElement hudVideo;
        private CheckBox chkHud;
        private Label lblOpacity;
        private TrackBar sliderOpacity;

        private Timer creditTimer;
        private float hueShift = 0f;
        private readonly string creditText = "✨ Created by Sanil ✨";

        private MediaPlayer byePlayer;

        public MainForm()
        {
            Text = "🛡️ Defender Exclusion Manager";
            Size = new Size(640, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 46);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            FormHelper.SetFormIcon(this);

            SetupHudBackground();

            // Title
            var lblTitle = new Label();
            lblTitle.Text = "Microsoft Defender — Exclusion Manager";
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(440, 30);
            lblTitle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(255, 100, 100);
            lblTitle.BackColor = Color.Transparent;
            Controls.Add(lblTitle);

            // HUD Toggle
            chkHud = new CheckBox();
            chkHud.Text = "HUD";
            chkHud.Checked = true;
            chkHud.Location = new Point(470, 15);
            chkHud.Size = new Size(70, 24);
            chkHud.ForeColor = Color.FromArgb(255, 200, 200);
            chkHud.BackColor = Color.Transparent;
            chkHud.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            chkHud.CheckedChanged += new EventHandler(chkHud_CheckedChanged);
            Controls.Add(chkHud);

            // Opacity Label
            lblOpacity = new Label();
            lblOpacity.Text = "35%";
            lblOpacity.Location = new Point(585, 17);
            lblOpacity.Size = new Size(40, 20);
            lblOpacity.ForeColor = Color.FromArgb(255, 200, 200);
            lblOpacity.BackColor = Color.Transparent;
            lblOpacity.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblOpacity.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(lblOpacity);

            // Opacity Slider
            sliderOpacity = new TrackBar();
            sliderOpacity.Minimum = 5;
            sliderOpacity.Maximum = 80;
            sliderOpacity.Value = 35;
            sliderOpacity.TickFrequency = 10;
            sliderOpacity.Location = new Point(545, 36);
            sliderOpacity.Size = new Size(80, 25);
            sliderOpacity.BackColor = Color.FromArgb(30, 30, 46);
            sliderOpacity.ValueChanged += new EventHandler(sliderOpacity_ValueChanged);
            Controls.Add(sliderOpacity);

            // Path label
            var lblPath = new Label();
            lblPath.Text = "Folder Path (paste here):";
            lblPath.Location = new Point(20, 70);
            lblPath.Size = new Size(300, 22);
            lblPath.ForeColor = Color.FromArgb(255, 220, 220);
            lblPath.BackColor = Color.Transparent;
            Controls.Add(lblPath);

            // Path TextBox
            txtPath = new TextBox();
            txtPath.Location = new Point(20, 95);
            txtPath.Size = new Size(460, 30);
            txtPath.BackColor = Color.FromArgb(220, 45, 45, 65);
            txtPath.ForeColor = Color.White;
            txtPath.BorderStyle = BorderStyle.FixedSingle;
            txtPath.Font = new Font("Consolas", 10.5f);
            txtPath.KeyDown += new KeyEventHandler(txtPath_KeyDown);
            Controls.Add(txtPath);

            // ADD button
            btnAdd = MakeButton("➕ ADD", 490, 94, 110, 32,
                                Color.FromArgb(34, 197, 94));
            btnAdd.Click += new EventHandler(btnAdd_Click);
            Controls.Add(btnAdd);

            // REMOVE button
            btnRemove = MakeButton("🗑️ REMOVE", 20, 140, 140, 32,
                                   Color.FromArgb(239, 68, 68));
            btnRemove.Click += new EventHandler(btnRemove_Click);
            Controls.Add(btnRemove);

            // REFRESH button
            btnRefresh = MakeButton("🔄 REFRESH", 170, 140, 140, 32,
                                    Color.FromArgb(107, 114, 128));
            btnRefresh.Click += new EventHandler(btnRefresh_Click);
            Controls.Add(btnRefresh);

            // List label
            var lblList = new Label();
            lblList.Text = "Current Exclusions:";
            lblList.Location = new Point(20, 185);
            lblList.Size = new Size(300, 22);
            lblList.ForeColor = Color.FromArgb(255, 220, 220);
            lblList.BackColor = Color.Transparent;
            Controls.Add(lblList);

            // ListBox
            lstList = new ListBox();
            lstList.Location = new Point(20, 210);
            lstList.Size = new Size(580, 200);
            lstList.BackColor = Color.FromArgb(230, 45, 45, 65);
            lstList.ForeColor = Color.FromArgb(255, 180, 180);
            lstList.BorderStyle = BorderStyle.FixedSingle;
            lstList.Font = new Font("Consolas", 9.5f);
            lstList.HorizontalScrollbar = true;
            Controls.Add(lstList);

            // Status
            lblStatus = new Label();
            lblStatus.Text = "Ready.";
            lblStatus.Location = new Point(20, 418);
            lblStatus.Size = new Size(580, 25);
            lblStatus.ForeColor = Color.FromArgb(251, 191, 36);
            lblStatus.BackColor = Color.Transparent;
            Controls.Add(lblStatus);

            // Animated Credit
            lblCredit = new Label();
            lblCredit.Text = creditText;
            lblCredit.Location = new Point(20, 450);
            lblCredit.Size = new Size(580, 34);
            lblCredit.Font = new Font("Segoe UI", 14f,
                                      FontStyle.Bold | FontStyle.Italic);
            lblCredit.TextAlign = ContentAlignment.MiddleCenter;
            lblCredit.BackColor = Color.Transparent;
            lblCredit.Paint += new PaintEventHandler(LblCredit_Paint);
            Controls.Add(lblCredit);

            // Animation Timer
            creditTimer = new Timer();
            creditTimer.Interval = 40;
            creditTimer.Tick += new EventHandler(creditTimer_Tick);
            creditTimer.Start();

            BringControlsToFront();
            LoadList();
        }

        private void SetupHudBackground()
        {
            try
            {
                string appDir = Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location);
                string videoPath = Path.Combine(appDir, "hud_background.mp4");

                if (!File.Exists(videoPath)) return;

                hudVideo = new System.Windows.Controls.MediaElement();
                hudVideo.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
                hudVideo.UnloadedBehavior = System.Windows.Controls.MediaState.Manual;
                hudVideo.Source = new Uri(videoPath);
                hudVideo.Stretch = System.Windows.Media.Stretch.UniformToFill;
                hudVideo.IsMuted = true;
                hudVideo.Opacity = 0.35;

                hudVideo.MediaEnded += (s, e) =>
                {
                    hudVideo.Position = TimeSpan.Zero;
                    hudVideo.Play();
                };

                hudVideo.Play();

                hudHost = new ElementHost();
                hudHost.Dock = DockStyle.Fill;
                hudHost.Child = hudVideo;
                hudHost.SendToBack();
                Controls.Add(hudHost);
            }
            catch { }
        }

        private void BringControlsToFront()
        {
            foreach (Control c in this.Controls)
            {
                if (c != hudHost)
                    c.BringToFront();
            }
        }

        private void chkHud_CheckedChanged(object sender, EventArgs e)
        {
            if (hudVideo == null) return;

            if (chkHud.Checked)
            {
                hudVideo.Visibility = System.Windows.Visibility.Visible;
                hudVideo.Play();
                sliderOpacity.Enabled = true;
                lblOpacity.Enabled = true;
            }
            else
            {
                hudVideo.Pause();
                hudVideo.Visibility = System.Windows.Visibility.Hidden;
                sliderOpacity.Enabled = false;
                lblOpacity.Enabled = false;
            }
        }

        private void sliderOpacity_ValueChanged(object sender, EventArgs e)
        {
            if (hudVideo == null) return;

            double opacity = sliderOpacity.Value / 100.0;
            hudVideo.Opacity = opacity;
            lblOpacity.Text = sliderOpacity.Value + "%";
        }

        private void txtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                DoAdd();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e) { DoAdd(); }
        private void btnRemove_Click(object sender, EventArgs e) { DoRemove(); }
        private void btnRefresh_Click(object sender, EventArgs e) { LoadList(); }

        private void creditTimer_Tick(object sender, EventArgs e)
        {
            hueShift += 1.2f;
            if (hueShift >= 360f) hueShift -= 360f;
            lblCredit.Invalidate();
        }

        private void LblCredit_Paint(object sender, PaintEventArgs e)
        {
            Label lbl = (Label)sender;
            e.Graphics.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color c1 = ColorFromHue(hueShift);
            Color c2 = ColorFromHue(hueShift + 120f);
            Color c3 = ColorFromHue(hueShift + 240f);

            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, lbl.Width, lbl.Height),
                c1, c3, LinearGradientMode.Horizontal))
            {
                var blend = new ColorBlend();
                blend.Colors = new Color[] { c1, c2, c3 };
                blend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = blend;

                using (var sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;

                    using (var shadowBrush = new SolidBrush(
                        Color.FromArgb(70, c1)))
                    {
                        e.Graphics.DrawString(lbl.Text, lbl.Font, shadowBrush,
                            new RectangleF(2, 2, lbl.Width, lbl.Height), sf);
                    }

                    e.Graphics.DrawString(lbl.Text, lbl.Font, brush,
                        new RectangleF(0, 0, lbl.Width, lbl.Height), sf);
                }
            }
        }

        private Color ColorFromHue(float hue)
        {
            hue = hue % 360f;
            float s = 1f;
            float v = 1f;

            int hi = (int)(hue / 60f) % 6;
            float f = hue / 60f - (int)(hue / 60f);
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            float r, g, b;
            switch (hi)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }
            return Color.FromArgb(
                (int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private Button MakeButton(string text, int x, int y, int w, int h, Color color)
        {
            var b = new Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.BackColor = color;
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            return b;
        }

        private void DoAdd()
        {
            string path = txtPath.Text.Trim().Trim('"');
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("❌ Path onnum illa. Paste cheyyuka.", Color.OrangeRed);
                return;
            }

            string safe = path.Replace("'", "''");
            string cmd = "Add-MpPreference -ExclusionPath '" + safe +
                         "' -ErrorAction Stop";

            string err;
            if (RunPS(cmd, out err))
            {
                SetStatus("✅ Exclusion add cheythu: " + path, Color.LightGreen);
                txtPath.Clear();
                LoadList();
            }
            else
            {
                SetStatus("❌ Fail: " + err, Color.OrangeRed);
            }
        }

        private void DoRemove()
        {
            string path = txtPath.Text.Trim().Trim('"');
            if (string.IsNullOrEmpty(path))
            {
                if (lstList.SelectedItem != null)
                    path = lstList.SelectedItem.ToString();
                else
                {
                    SetStatus("❌ Path illa. Paste cheyyuka allenkil select cheyyuka.",
                              Color.OrangeRed);
                    return;
                }
            }

            string safe = path.Replace("'", "''");
            string cmd = "Remove-MpPreference -ExclusionPath '" + safe +
                         "' -ErrorAction Stop";

            string err;
            if (RunPS(cmd, out err))
            {
                SetStatus("✅ Remove cheythu: " + path, Color.LightGreen);
                txtPath.Clear();
                LoadList();
            }
            else
            {
                SetStatus("❌ Fail: " + err, Color.OrangeRed);
            }
        }

        private void LoadList()
        {
            lstList.Items.Clear();
            string cmd = "(Get-MpPreference).ExclusionPath";

            string output;
            string err;
            if (RunPSOutput(cmd, out output, out err))
            {
                string[] lines = output.Split(
                    new char[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0)
                    lstList.Items.Add("(onnum illa)");
                else
                {
                    foreach (string l in lines)
                        lstList.Items.Add(l.Trim());
                }
            }
            else
            {
                lstList.Items.Add("Error: " + err);
            }
        }

        private bool RunPS(string command, out string error)
        {
            string output;
            return RunPSOutput(command, out output, out error);
        }

        private bool RunPSOutput(string command, out string output, out string error)
        {
            output = "";
            error = "";
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" +
                                command.Replace("\"", "\\\"") + "\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using (var p = Process.Start(psi))
                {
                    output = p.StandardOutput.ReadToEnd();
                    error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private void SetStatus(string msg, Color color)
        {
            lblStatus.Text = msg;
            lblStatus.ForeColor = color;
        }

        // ============================================
        //  ON CLOSE — bye.wav only (no VBS)
        // ============================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                string file = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "bye.wav");
                if (File.Exists(file))
                {
                    byePlayer = new MediaPlayer();
                    byePlayer.Open(new Uri(file));
                    byePlayer.Volume = 1.0;
                    byePlayer.Play();
                    System.Threading.Thread.Sleep(2500);
                }
            }
            catch { }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (creditTimer != null)
            {
                creditTimer.Stop();
                creditTimer.Dispose();
            }
            if (byePlayer != null)
            {
                try { byePlayer.Close(); } catch { }
                byePlayer = null;
            }

            if (hudVideo != null)
            {
                try
                {
                    hudVideo.Stop();
                    hudVideo.Close();
                }
                catch { }
                hudVideo = null;
            }

            base.OnFormClosed(e);
        }
    }

    // =====================================================
    //  🚀 PROGRAM ENTRY (Splash → Main works)
    // =====================================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Splash (modal — clean close)
                var splash = new SplashForm();
                splash.ShowDialog();
                splash.Dispose();

                // Main app
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try
                {
                    string logPath = Path.Combine(
                        Path.GetDirectoryName(
                            Assembly.GetExecutingAssembly().Location),
                        "error_log.txt");
                    File.AppendAllText(logPath,
                        DateTime.Now + " — " + ex.ToString() + "\n\n");
                }
                catch { }

                MessageBox.Show(
                    "Error:\n\n" + ex.Message +
                    "\n\nDetails saved to error_log.txt",
                    "Defender Exclusion Manager",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}