using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Night-Night: schedules a PC shutdown. The shutdown is triggered by this app's own
// timer (not by Windows' "shutdown /t"), so closing the app always cancels it.
public class NightNight : Form
{
    [DllImport("kernel32.dll")]
    static extern uint SetThreadExecutionState(uint esFlags);
    const uint ES_CONTINUOUS = 0x80000000;
    const uint ES_SYSTEM_REQUIRED = 0x00000001;

    const int WarningSeconds = 10;

    readonly Label heading = new Label();
    readonly Button modeButton = new Button();

    readonly Panel clockPanel = new Panel();
    readonly ComboBox monthBox = new ComboBox();
    readonly ComboBox dayBox = new ComboBox();
    readonly ComboBox hourBox = new ComboBox();
    readonly ComboBox minuteBox = new ComboBox();
    readonly Button ampmButton = new Button();

    readonly Panel timerPanel = new Panel();
    readonly ComboBox timerYearsBox = new ComboBox();
    readonly ComboBox timerMonthsBox = new ComboBox();
    readonly ComboBox timerDaysBox = new ComboBox();
    readonly ComboBox timerHoursBox = new ComboBox();
    readonly ComboBox timerMinutesBox = new ComboBox();

    readonly Button setButton = new Button();
    readonly Label countdownLabel = new Label();
    readonly Button warningToggle = new Button();
    readonly LinkLabel updateLink = new LinkLabel();
    readonly Timer ticker = new Timer();

    UpdateCheck.Offer update;

    Form warningPopup;
    Label warningPopupText;

    bool clockMode = true;
    bool isPm;
    bool armed;
    bool warningEnabled = true;
    DateTime target;

    public NightNight()
    {
        Text = "Night-Night";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(470, 214);
        Font = new Font("Segoe UI", 10f);

        heading.SetBounds(16, 16, 300, 24);
        heading.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        Controls.Add(heading);

        modeButton.SetBounds(344, 12, 110, 30);
        modeButton.Click += delegate { SetMode(!clockMode); };
        Controls.Add(modeButton);

        // --- Clock mode: month, day, hour : minute, AM/PM ---
        clockPanel.SetBounds(16, 52, 440, 56);
        string[] months = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
        for (int m = 0; m < 12; m++) monthBox.Items.Add(months[m]);
        AddLabeledCombo(clockPanel, "Month", monthBox, 0, 110);
        monthBox.SelectedIndexChanged += delegate { FillDays(); };
        AddLabeledCombo(clockPanel, "Day", dayBox, 116, 56);
        for (int h = 1; h <= 12; h++) hourBox.Items.Add(h.ToString());
        AddLabeledCombo(clockPanel, "Hour", hourBox, 182, 56);
        Label colon = new Label();
        colon.Text = ":";
        colon.SetBounds(240, 26, 12, 24);
        clockPanel.Controls.Add(colon);
        for (int mi = 0; mi < 60; mi++) minuteBox.Items.Add(mi.ToString("00"));
        AddLabeledCombo(clockPanel, "Minute", minuteBox, 254, 60);
        AddCaption(clockPanel, "AM/PM", 322, 60);
        ampmButton.SetBounds(322, 22, 60, 29);
        ampmButton.Click += delegate { isPm = !isPm; ampmButton.Text = isPm ? "PM" : "AM"; };
        clockPanel.Controls.Add(ampmButton);
        Controls.Add(clockPanel);

        // --- Timer mode: hours + minutes from now ---
        timerPanel.SetBounds(16, 52, 440, 56);
        for (int y = 0; y <= 10; y++) timerYearsBox.Items.Add(y.ToString());
        AddLabeledCombo(timerPanel, "Years", timerYearsBox, 0, 60);
        for (int mo = 0; mo <= 11; mo++) timerMonthsBox.Items.Add(mo.ToString());
        AddLabeledCombo(timerPanel, "Months", timerMonthsBox, 70, 60);
        for (int d = 0; d <= 30; d++) timerDaysBox.Items.Add(d.ToString());
        AddLabeledCombo(timerPanel, "Days", timerDaysBox, 140, 60);
        for (int h = 0; h <= 23; h++) timerHoursBox.Items.Add(h.ToString());
        AddLabeledCombo(timerPanel, "Hours", timerHoursBox, 210, 60);
        for (int mi = 0; mi < 60; mi++) timerMinutesBox.Items.Add(mi.ToString());
        AddLabeledCombo(timerPanel, "Minutes", timerMinutesBox, 280, 60);
        Label fromNow = new Label();
        fromNow.Text = "from now";
        fromNow.SetBounds(348, 26, 90, 24);
        timerPanel.Controls.Add(fromNow);
        timerYearsBox.SelectedIndex = 0;
        timerMonthsBox.SelectedIndex = 0;
        timerDaysBox.SelectedIndex = 0;
        timerHoursBox.SelectedIndex = 0;
        timerMinutesBox.SelectedIndex = 0;
        Controls.Add(timerPanel);

        setButton.SetBounds(16, 122, 140, 36);
        setButton.Click += delegate { if (armed) Disarm(); else Arm(); };
        Controls.Add(setButton);

        countdownLabel.SetBounds(170, 122, 290, 36);
        countdownLabel.TextAlign = ContentAlignment.MiddleLeft;
        countdownLabel.Font = new Font("Consolas", 16f, FontStyle.Bold);
        Controls.Add(countdownLabel);

        // --- Bottom right: shutdown warning on/off ---
        Label warningLabel = new Label();
        warningLabel.Text = "10 Second Shutdown Warning:";
        warningLabel.TextAlign = ContentAlignment.MiddleRight;
        warningLabel.SetBounds(150, 174, 240, 28);
        Controls.Add(warningLabel);
        warningToggle.SetBounds(394, 173, 60, 29);
        warningToggle.Click += delegate
        {
            warningEnabled = !warningEnabled;
            if (!warningEnabled) CloseWarningPopup();
            UpdateWarningToggle();
        };
        Controls.Add(warningToggle);

        // --- Bottom left: only shown when the website has a newer version ---
        updateLink.Text = "Update available";
        updateLink.Font = new Font("Segoe UI", 9f);
        updateLink.TextAlign = ContentAlignment.MiddleLeft;
        updateLink.SetBounds(16, 176, 130, 24);
        updateLink.Visible = false;
        updateLink.LinkClicked += delegate { OfferUpdate(); };
        Controls.Add(updateLink);
        Shown += delegate { StartUpdateCheck(); };

        ticker.Interval = 250;
        ticker.Tick += delegate { Tick(); };

        LoadCurrentTime();
        SetMode(true);
        UpdateSetButton();
        UpdateWarningToggle();
    }

    static void AddCaption(Panel panel, string text, int x, int width)
    {
        Label caption = new Label();
        caption.Text = text;
        caption.Font = new Font("Segoe UI", 8.5f);
        caption.ForeColor = SystemColors.GrayText;
        caption.SetBounds(x, 0, width, 20);
        panel.Controls.Add(caption);
    }

    static void AddLabeledCombo(Panel panel, string caption, ComboBox box, int x, int width)
    {
        AddCaption(panel, caption, x, width);
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.MaxDropDownItems = 12;
        box.SetBounds(x, 22, width, 29);
        panel.Controls.Add(box);
    }

    void LoadCurrentTime()
    {
        DateTime now = DateTime.Now;
        monthBox.SelectedIndex = now.Month - 1; // also fills the day list
        dayBox.SelectedIndex = now.Day - 1;
        int h12 = now.Hour % 12;
        hourBox.SelectedIndex = (h12 == 0 ? 12 : h12) - 1;
        minuteBox.SelectedIndex = now.Minute;
        isPm = now.Hour >= 12;
        ampmButton.Text = isPm ? "PM" : "AM";
    }

    int YearFor(int month)
    {
        // A month earlier than the current one means next year.
        DateTime now = DateTime.Now;
        return month < now.Month ? now.Year + 1 : now.Year;
    }

    void FillDays()
    {
        int month = monthBox.SelectedIndex + 1;
        int keep = dayBox.SelectedIndex;
        int days = DateTime.DaysInMonth(YearFor(month), month);
        dayBox.BeginUpdate();
        dayBox.Items.Clear();
        for (int d = 1; d <= days; d++) dayBox.Items.Add(d.ToString());
        dayBox.EndUpdate();
        dayBox.SelectedIndex = Math.Min(Math.Max(keep, 0), days - 1);
    }

    void SetMode(bool clock)
    {
        clockMode = clock;
        clockPanel.Visible = clock;
        timerPanel.Visible = !clock;
        heading.Text = clock ? "Shut down at:" : "Shut down in:";
        modeButton.Text = clock ? "Mode: Clock" : "Mode: Timer";
    }

    void Arm()
    {
        DateTime now = DateTime.Now;
        if (clockMode)
        {
            int month = monthBox.SelectedIndex + 1;
            int day = dayBox.SelectedIndex + 1;
            int hour = (hourBox.SelectedIndex + 1) % 12 + (isPm ? 12 : 0);
            int minute = minuteBox.SelectedIndex;
            target = new DateTime(YearFor(month), month, day, hour, minute, 0);
            // Anything in the current minute or earlier is rejected.
            DateTime thisMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            if (target <= thisMinute)
            {
                MessageBox.Show(this, "Cannot set a shutdown for the current time or a time in the past.",
                    "Night-Night", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        else
        {
            target = now.AddYears(timerYearsBox.SelectedIndex)
                        .AddMonths(timerMonthsBox.SelectedIndex)
                        .AddDays(timerDaysBox.SelectedIndex)
                        .AddHours(timerHoursBox.SelectedIndex)
                        .AddMinutes(timerMinutesBox.SelectedIndex);
            if (target - now < TimeSpan.FromMinutes(1))
            {
                MessageBox.Show(this, "Choose at least 1 minute.", "Night-Night",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        armed = true;
        // Keep the PC from going to sleep while waiting, or the timer would never fire.
        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
        ticker.Start();
        UpdateSetButton();
        Tick();
    }

    void Disarm()
    {
        armed = false;
        ticker.Stop();
        SetThreadExecutionState(ES_CONTINUOUS);
        CloseWarningPopup();
        countdownLabel.Text = "";
        UpdateSetButton();
    }

    void UpdateSetButton()
    {
        setButton.Text = armed ? "Cancel" : "Set Shutdown";
        clockPanel.Enabled = !armed;
        timerPanel.Enabled = !armed;
        modeButton.Enabled = !armed;
    }

    void UpdateWarningToggle()
    {
        warningToggle.Text = warningEnabled ? "On" : "Off";
    }

    void Tick()
    {
        TimeSpan left = target - DateTime.Now;
        if (left <= TimeSpan.Zero)
        {
            Disarm();
            countdownLabel.Text = "Shutting down...";
            Process.Start(new ProcessStartInfo("shutdown", "/s /f /t 0") { CreateNoWindow = true, UseShellExecute = false });
            return;
        }
        int secs = (int)Math.Ceiling(left.TotalSeconds);
        TimeSpan s = TimeSpan.FromSeconds(secs);
        countdownLabel.Text = s.Days > 0
            ? string.Format("{0}d {1:00}:{2:00}:{3:00}", s.Days, s.Hours, s.Minutes, s.Seconds)
            : string.Format("{0:00}:{1:00}:{2:00}", s.Hours, s.Minutes, s.Seconds);

        if (warningEnabled && secs <= WarningSeconds)
        {
            if (warningPopup == null) ShowWarningPopup();
            warningPopupText.Text = string.Format("The PC will shut down in: {0} second{1}", secs, secs == 1 ? "" : "s");
        }
    }

    void ShowWarningPopup()
    {
        Form popup = new Form();
        popup.Text = "Night-Night - Shutdown warning";
        popup.FormBorderStyle = FormBorderStyle.FixedDialog;
        popup.MaximizeBox = false;
        popup.MinimizeBox = false;
        popup.ShowInTaskbar = false;
        popup.TopMost = true;
        popup.StartPosition = FormStartPosition.CenterScreen;
        popup.ClientSize = new Size(380, 130);
        popup.Font = new Font("Segoe UI", 10f);

        warningPopupText = new Label();
        warningPopupText.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        warningPopupText.TextAlign = ContentAlignment.MiddleCenter;
        warningPopupText.SetBounds(10, 16, 360, 40);
        popup.Controls.Add(warningPopupText);

        Button cancel = new Button();
        cancel.Text = "Cancel Shutdown";
        cancel.SetBounds(115, 72, 150, 36);
        cancel.Click += delegate { Disarm(); };
        popup.Controls.Add(cancel);

        // Closing the popup with X also cancels, so the user is never surprised.
        popup.FormClosed += delegate
        {
            bool stillArmed = armed && warningPopup != null;
            warningPopup = null;
            if (stillArmed) Disarm();
        };

        warningPopup = popup;
        popup.Show(this);
        popup.Activate();
    }

    void CloseWarningPopup()
    {
        if (warningPopup == null) return;
        Form popup = warningPopup;
        warningPopup = null; // clear first so FormClosed doesn't treat this as a cancel
        popup.Close();
    }

    // Asks the website once, in the background, when the window opens. Quiet unless there is
    // something newer - no internet just means no link.
    void StartUpdateCheck()
    {
        System.Threading.ThreadPool.QueueUserWorkItem(delegate
        {
            UpdateCheck.Offer offer = UpdateCheck.Check();
            if (offer == null || !UpdateCheck.IsNewer(offer.Version)) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    update = offer;
                    updateLink.Visible = true;
                });
            }
            catch (InvalidOperationException)
            {
                // the window was already closed
            }
        });
    }

    void OfferUpdate()
    {
        if (update == null) return;
        DialogResult answer = MessageBox.Show(this,
            "Night-Night V" + update.Version + " is available. You have V" + UpdateCheck.CurrentVersion + ".\n\n" +
            "Download it now? It opens in your browser; run the new Night-Night when it finishes.",
            "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (answer != DialogResult.Yes) return;
        try
        {
            Process.Start(new ProcessStartInfo(update.Download) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open the download:\n" + ex.Message, "Night-Night",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Closing the app cancels the shutdown: the timer dies with the app.
        if (armed) Disarm();
        base.OnFormClosing(e);
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new NightNight());
    }
}
