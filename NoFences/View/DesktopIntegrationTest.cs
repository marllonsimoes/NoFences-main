using log4net;
using NoFences.Util;
using NoFences.Win32.Desktop;
using NoFences.Win32.Window;
using NoFences.Win32.Shell;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NoFences.View
{
    /// <summary>
    /// Test form to demonstrate and verify the new WorkerW desktop integration.
    /// This can be used for debugging and testing the integration.
    /// </summary>
    public class DesktopIntegrationTest : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DesktopIntegrationTest));

        private Button btnBehindIcons;
        private Button btnAboveIcons;
        private Button btnShowInfo;
        private Button btnRefresh;
        private Label lblStatus;
        private TextBox txtInfo;
        private bool currentlyBehind = false;

        public DesktopIntegrationTest()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Desktop Integration Test";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Create controls
            btnBehindIcons = new Button
            {
                Text = "Test: Behind Desktop Icons",
                Location = new Point(20, 20),
                Size = new Size(200, 40)
            };
            btnBehindIcons.Click += BtnBehindIcons_Click;

            btnAboveIcons = new Button
            {
                Text = "Test: Above Desktop Icons",
                Location = new Point(240, 20),
                Size = new Size(200, 40)
            };
            btnAboveIcons.Click += BtnAboveIcons_Click;

            btnShowInfo = new Button
            {
                Text = "Show Integration Info",
                Location = new Point(20, 70),
                Size = new Size(200, 40)
            };
            btnShowInfo.Click += BtnShowInfo_Click;

            btnRefresh = new Button
            {
                Text = "Refresh Integration",
                Location = new Point(240, 70),
                Size = new Size(200, 40)
            };
            btnRefresh.Click += BtnRefresh_Click;

            lblStatus = new Label
            {
                Text = "Status: Ready",
                Location = new Point(20, 120),
                Size = new Size(440, 20),
                BackColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtInfo = new TextBox
            {
                Location = new Point(20, 150),
                Size = new Size(440, 180),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Courier New", 9)
            };

            // Add controls to form
            this.Controls.AddRange(new Control[]
            {
                btnBehindIcons,
                btnAboveIcons,
                btnShowInfo,
                btnRefresh,
                lblStatus,
                txtInfo
            });

            UpdateInfo("Desktop Integration Test initialized.\n" +
                      "Click buttons to test different modes.\n" +
                      "A semi-transparent test window will appear on the desktop.");
        }

        private void BtnBehindIcons_Click(object sender, EventArgs e)
        {
            CreateTestWindow(behindIcons: true);
        }

        private void BtnAboveIcons_Click(object sender, EventArgs e)
        {
            CreateTestWindow(behindIcons: false);
        }

        private void BtnShowInfo_Click(object sender, EventArgs e)
        {
            try
            {
                var info = WorkerWIntegration.GetIntegrationInfo();

                var infoText = $"Desktop Integration Information:\n" +
                              $"================================\n" +
                              $"Progman Handle: {info.ProgmanHandle} (0x{info.ProgmanHandle:X})\n" +
                              $"WorkerW Handle: {info.WorkerWHandle} (0x{info.WorkerWHandle:X})\n" +
                              $"WorkerW Cached: {info.WorkerWCached}\n" +
                              $"Cache Age: {info.CacheAge.TotalSeconds:F1} seconds\n" +
                              $"\n" +
                              $"System Information:\n" +
                              $"==================\n" +
                              $"Primary Screen: {Screen.PrimaryScreen.Bounds}\n" +
                              $"Screen Count: {Screen.AllScreens.Length}\n" +
                              $"OS Version: {Environment.OSVersion}\n";

                UpdateInfo(infoText);
                lblStatus.Text = "Status: Information retrieved";
            }
            catch (Exception ex)
            {
                UpdateInfo($"Error getting integration info:\n{ex.Message}\n\n{ex.StackTrace}");
                lblStatus.Text = "Status: Error";
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                // Force refresh WorkerW discovery
                var workerw = WorkerWIntegration.GetWorkerW(forceRefresh: true);

                UpdateInfo($"Desktop integration refreshed.\n" +
                          $"New WorkerW Handle: {workerw} (0x{workerw:X})\n" +
                          $"\n" +
                          $"Call this after display changes or if windows disappear.");

                lblStatus.Text = "Status: Refreshed";
            }
            catch (Exception ex)
            {
                UpdateInfo($"Error refreshing:\n{ex.Message}");
                lblStatus.Text = "Status: Error";
            }
        }

        private void CreateTestWindow(bool behindIcons)
        {
            try
            {
                // Create a semi-transparent colored test window
                var testWindow = new Form
                {
                    Text = behindIcons ? "Behind Icons Test" : "Above Icons Test",
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = behindIcons ? Color.Blue : Color.Green,
                    Opacity = 0.3,
                    TopMost = false
                };

                // Make it cover a portion of the primary screen
                var bounds = Screen.PrimaryScreen.Bounds;
                testWindow.Bounds = new Rectangle(
                    bounds.Width / 4,
                    bounds.Height / 4,
                    bounds.Width / 2,
                    bounds.Height / 2
                );

                // Add a label to show what mode it's in
                var label = new Label
                {
                    Text = behindIcons ? "BEHIND DESKTOP ICONS\n(Like Lively Wallpaper)" : "ABOVE DESKTOP ICONS\n(Traditional NoFences)",
                    Font = new Font("Arial", 24, FontStyle.Bold),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };
                testWindow.Controls.Add(label);

                // Add a close button
                var closeButton = new Button
                {
                    Text = "Close This Test Window",
                    Location = new Point(testWindow.Width / 2 - 75, testWindow.Height - 60),
                    Size = new Size(150, 40),
                    BackColor = Color.White
                };
                closeButton.Click += (s, ev) => testWindow.Close();
                testWindow.Controls.Add(closeButton);

                // Show the window first
                testWindow.Show();

                // Then apply desktop integration
                if (behindIcons)
                {
                    WorkerWIntegration.ParentToBehindDesktopIcons(testWindow.Handle);
                    WindowUtil.HideFromAltTab(testWindow.Handle);
                }
                else
                {
                    WorkerWIntegration.ParentToAboveDesktopIcons(testWindow.Handle);
                    WindowUtil.HideFromAltTab(testWindow.Handle);
                }

                currentlyBehind = behindIcons;

                UpdateInfo($"Test window created: {(behindIcons ? "BEHIND" : "ABOVE")} icons\n" +
                          $"Color: {(behindIcons ? "Blue" : "Green")}\n" +
                          $"Position: Center of screen\n" +
                          $"Opacity: 30%\n" +
                          $"\n" +
                          $"If behind icons: You should see desktop icons OVER the blue window\n" +
                          $"If above icons: You should see desktop icons UNDER the green window\n" +
                          $"\n" +
                          $"Click the button in the test window to close it.");

                lblStatus.Text = $"Status: Test window active ({(behindIcons ? "Behind" : "Above")} icons)";
            }
            catch (Exception ex)
            {
                UpdateInfo($"Error creating test window:\n{ex.Message}\n\n{ex.StackTrace}");
                lblStatus.Text = "Status: Error";
            }
        }

        private void UpdateInfo(string text)
        {
            txtInfo.Text = text;
            log.Debug($"DesktopIntegrationTest: {text.Replace("\n", " ").Substring(0, Math.Min(100, text.Length))}...");
        }

        /// <summary>
        /// Show the test dialog
        /// </summary>
        public static void ShowTestDialog()
        {
            var testForm = new DesktopIntegrationTest();
            testForm.ShowDialog();
        }
    }
}
