using ApplicationLogger.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;
using System.Windows.Forms;

namespace ApplicationLogger {

	public partial class MainForm : Form {

		// Constants
		private const string SETTINGS_FIELD_RUN_AT_STARTUP = "RunAtStartup";
		private const string CONFIG_FILE = "ApplicationLogger.cfg";
		private const string TEMP_LOG_FILE = "tempLog";                             // Used when app restart
		private const string TEMP_WINDOWS_RUN_AT_STARTUP_AS_ADMIN_FILE = "tempAs";  // Used when app restart

		private const string LINE_DIVIDER = "\t";
		private const string LINE_END = "\r\n";
		private const string DATE_TIME_FORMAT = "o";                                // 2008-06-15T21:15:07.0000000

		// Properties
		private Timer timerCheck;
		private ContextMenu contextMenu;
		private MenuItem menuItemOpen;
		private MenuItem menuItemOpenLog;
		private MenuItem menuItemStartStop;
		private MenuItem menuItemRunAtStartup;
		private MenuItem menuItemExit;
		private bool allowClose;
		private bool allowShow;
		private bool isRunning;
		private bool isUserIdle;
		private bool hasInitialized;
		private string lastUserProcessId;
		private string lastFileNameSaved;
		private int lastDayLineLogged;
		private DateTime lastTimeQueueWritten;
		private List<string> queuedLogMessages;

		private string configPath;
		private float? configIdleTime;                                              // In seconds
		private float? configTimeCheckInterval;                                     // In seconds
		private float? configMaxQueueTime;
		private int? configMaxQueueEntries;

		private string newUserProcessId;                                            // Temp
		private StringBuilder lineToLog;                                            // Temp, used to create the line
		private ContextMenuStrip contextMenuStrip;
		private readonly ListViewColumnSorterExt fileSorter;

		// ================================================================================================================
		// CONSTRUCTOR ----------------------------------------------------------------------------------------------------

		public MainForm() {
			InitializeComponent();
			initializeForm();

			// Apply sorting to the listview
			fileSorter = new ListViewColumnSorterExt(lsvLog);
		}


		// ================================================================================================================
		// EVENT INTERFACE ------------------------------------------------------------------------------------------------

		private void onFormLoad(object sender, EventArgs e) {
			// First time the form is shown
		}

		protected override void SetVisibleCore(bool isVisible) {
			if (!allowShow) {
				// Initialization form show, when it's ran: doesn't allow showing form
				isVisible = false;
				if (!this.IsHandleCreated) CreateHandle();
			}
			base.SetVisibleCore(isVisible);
		}

		private void onFormClosing(object sender, FormClosingEventArgs e) {
			// Form is attempting to close
			if (!allowClose) {
				// User initiated, just minimize instead
				e.Cancel = true;
				Hide();
			}
		}

		private void onFormClosed(object sender, FormClosedEventArgs e) {
			// Stops everything
			stop();

			// If debugging, un-hook itself from startup
			if (System.Diagnostics.Debugger.IsAttached && windowsRunAtStartup) {
				settingsRunAtStartup = false;
				//windowsRunAtStartup = false;
				applySettingsRunAtStartup();
			}
		}

		private void onTimer(object sender, EventArgs e) {
			// Timer tick: check for the current application

			// Check the user is idle
			if (SystemHelper.GetIdleTime() >= configIdleTime * 1000f) {
				if (!isUserIdle) {
					// User is now idle
					isUserIdle = true;
					lastUserProcessId = null;
					logUserIdle();
				}
			} else {
				if (isUserIdle) {
					// User is not idle anymore
					isUserIdle = false;
				}
			}

			// Check the user process
			if (!isUserIdle) {
				var process = getCurrentUserProcess();
				if (process != null) {
					// Valid process, create a unique id
					newUserProcessId = process.ProcessName + "_" + process.MainWindowTitle;

					if (lastUserProcessId != newUserProcessId) {
						// New process
						logUserProcess(process);
						lastUserProcessId = newUserProcessId;
					}
				}
			}

			// Write to log if enough time passed
			if (queuedLogMessages.Count > 0 && (DateTime.Now - lastTimeQueueWritten).TotalSeconds > configMaxQueueTime) {
				commitLines();
			}
		}

		private void onResize(object sender, EventArgs e) {
			// Resized window
			//notifyIcon.BalloonTipTitle = "Minimize to Tray App";
			//notifyIcon.BalloonTipText = "You have successfully minimized your form.";

			if (WindowState == FormWindowState.Minimized) {
				//notifyIcon.ShowBalloonTip(500);
				this.Hide();
			}
		}

		private void onMenuItemOpenClicked(object Sender, EventArgs e) {
			showForm();
		}

		private void onMenuItemStartStopClicked(object Sender, EventArgs e) {
			if (isRunning) {
				stop();
			} else {
				start();
			}
		}

		private void onMenuItemOpenLogClicked(object Sender, EventArgs e) {
			commitLines();
			Process.Start(getLogFileName());
		}

		private void onMenuItemRunAtStartupClicked(object Sender, EventArgs e) {
			menuItemRunAtStartup.Checked = !menuItemRunAtStartup.Checked;
			settingsRunAtStartup = menuItemRunAtStartup.Checked;
			applySettingsRunAtStartup();
		}

		private void onMenuItemExitClicked(object Sender, EventArgs e) {
			exit();
		}

		private void onDoubleClickNotificationIcon(object sender, MouseEventArgs e) {
			showForm();
		}


		// ================================================================================================================
		// INTERNAL INTERFACE ---------------------------------------------------------------------------------------------

		private void initializeForm() {
			// Initialize

			if (!hasInitialized) {
				allowClose = false;
				isRunning = false;
				queuedLogMessages = new List<string>();
				lineToLog = new StringBuilder();
				lastFileNameSaved = "";
				allowShow = false;
				ckbAutoScroll.Checked = true;

				// Force working folder
				System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

				// Read configuration
				readConfiguration();

				// Create context menu for the tray icon and update it
				createContextMenu();

				// Update tray
				updateTrayIcon();

				// Check if it needs to run at startup
				applySettingsRunAtStartup();

				// Create ListView
				createListView();

				// Create context menu for the list view and update it
				createContextMenuStrip();

				// Check if is running as administrator
				btnRunAs.Visible = !SystemHelper.IsRunningAsAdmin();

				// Check if has temp log file to log into list view
				ImportListViewItemsFromFile(TEMP_LOG_FILE, true);

				// Check if has temp file to set startup windows as admin
				CheckSettingStartupWindowsAsAdminIsNeeded();

				// Finally, start
				start();

				hasInitialized = true;
			}
		}

		private void CheckSettingStartupWindowsAsAdminIsNeeded() {
			if (File.Exists(TEMP_WINDOWS_RUN_AT_STARTUP_AS_ADMIN_FILE)) {
				try {
					windowsRunAtStartup = true;
					File.Delete(TEMP_WINDOWS_RUN_AT_STARTUP_AS_ADMIN_FILE);
				} catch (Exception ex) {
				}
			}
		}

		private void createContextMenuStrip() {
			contextMenuStrip = new ContextMenuStrip();

			contextMenuStrip.Items.Add("&Open folder in File Explorer");
			contextMenuStrip.Items.Add("&Copy Command Line to Clipboard");
			contextMenuStrip.Opening += ContextMenuStrip_Opening;
			contextMenuStrip.ItemClicked += ContextMenuStrip_ItemClicked;

			lsvLog.ContextMenuStrip = contextMenuStrip;
		}

		private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
			updateContextMenuStrip();
		}

		private void updateContextMenuStrip() {
			// Only enable if a item is selected
			if (lsvLog.SelectedItems.Count > 0) {
				contextMenuStrip.Enabled = true;
			} else {
				contextMenuStrip.Enabled = false;
			}
		}

		private void ContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
			switch (e.ClickedItem.ToString()) {
				case "&Open folder in File Explorer":
					OpenFolderInFileExplorer();
					break;
				case "&Copy Command Line to Clipboard":
					Clipboard.SetText(lsvLog.SelectedItems[0].SubItems[6].Text);
					break;
				default:
					break;
			}
		}

		private void OpenFolderInFileExplorer() {
			string getSelectedFileName = lsvLog.SelectedItems[0].SubItems[4].Text;

			//string filePath = System.IO.Path.GetDirectoryName(getSelectedFileName);
			if (getSelectedFileName.Length > 0 && System.IO.File.Exists(getSelectedFileName)) {
				Process.Start("explorer.exe", $"/select, {getSelectedFileName}");
			}
		}

		private void createListView() {
			lsvLog.Columns.Add("Date/Time", 210, HorizontalAlignment.Left);
			lsvLog.Columns.Add("Type", 100, HorizontalAlignment.Left);
			lsvLog.Columns.Add("Machine", 100, HorizontalAlignment.Left);
			lsvLog.Columns.Add("Title", 100, HorizontalAlignment.Left);
			lsvLog.Columns.Add("Location", 300, HorizontalAlignment.Left);
			lsvLog.Columns.Add("Subject", 150, HorizontalAlignment.Left);
			lsvLog.Columns.Add("Command Line", 400, HorizontalAlignment.Left);

			lsvLog.View = View.Details;
			lsvLog.FullRowSelect = true;
			lsvLog.MultiSelect = false;
			lsvLog.ShowItemToolTips = true;
		}

		private void createContextMenu() {
			// Initialize context menu
			contextMenu = new ContextMenu();

			// Initialize menu items
			menuItemOpen = new MenuItem();
			menuItemOpen.Index = 0;
			menuItemOpen.Text = "&Open";
			menuItemOpen.Click += new EventHandler(onMenuItemOpenClicked);
			contextMenu.MenuItems.Add(menuItemOpen);

			menuItemStartStop = new MenuItem();
			menuItemStartStop.Index = 0;
			menuItemStartStop.Text = ""; // Set later
			menuItemStartStop.Click += new EventHandler(onMenuItemStartStopClicked);
			contextMenu.MenuItems.Add(menuItemStartStop);

			contextMenu.MenuItems.Add("-");

			menuItemOpenLog = new MenuItem();
			menuItemOpenLog.Index = 0;
			menuItemOpenLog.Text = ""; // Set later
			menuItemOpenLog.Click += new EventHandler(onMenuItemOpenLogClicked);
			contextMenu.MenuItems.Add(menuItemOpenLog);

			contextMenu.MenuItems.Add("-");

			menuItemRunAtStartup = new MenuItem();
			menuItemRunAtStartup.Index = 0;
			menuItemRunAtStartup.Text = "Run at Windows startup";
			menuItemRunAtStartup.Click += new EventHandler(onMenuItemRunAtStartupClicked);
			menuItemRunAtStartup.Checked = settingsRunAtStartup;
			contextMenu.MenuItems.Add(menuItemRunAtStartup);

			contextMenu.MenuItems.Add("-");

			menuItemExit = new MenuItem();
			menuItemExit.Index = 1;
			menuItemExit.Text = "E&xit";
			menuItemExit.Click += new EventHandler(onMenuItemExitClicked);
			contextMenu.MenuItems.Add(menuItemExit);

			notifyIcon.ContextMenu = contextMenu;

			updateContextMenu();
		}

		private void updateContextMenu() {
			// Update start/stop command
			if (menuItemStartStop != null) {
				if (isRunning) {
					menuItemStartStop.Text = "&Stop";
				} else {
					menuItemStartStop.Text = "&Start";
				}
			}

			// Update filename
			if (menuItemOpenLog != null) {
				var filename = getLogFileName();
				if (!System.IO.File.Exists(filename)) {
					// Doesn't exist
					menuItemOpenLog.Text = "Open &log file";
					menuItemOpenLog.Enabled = false;
				} else {
					// Exists
					menuItemOpenLog.Text = "Open &log file (" + filename + ")";
					menuItemOpenLog.Enabled = true;
				}
			}
		}

		private void updateTrayIcon() {
			if (isRunning) {
				notifyIcon.Icon = ApplicationLogger.Properties.Resources.iconNormal;
				notifyIcon.Text = "Application Logger (started)";
				Icon = ApplicationLogger.Properties.Resources.iconNormal;
				Text = "Application Logger (started)";
			} else {
				notifyIcon.Icon = ApplicationLogger.Properties.Resources.iconStopped;
				notifyIcon.Text = "Application Logger (stopped)";
				this.Icon = ApplicationLogger.Properties.Resources.iconStopped;
				Text = "Application Logger (stopped)";
			}
		}

		private void readConfiguration() {
			// Read the current configuration file

			// Read default file
			ConfigParser configDefault = new ConfigParser(ApplicationLogger.Properties.Resources.default_config);
			ConfigParser configUser;

			if (!System.IO.File.Exists(CONFIG_FILE)) {
				// Config file not found, create it first
				Console.Write("Config file does not exist, creating");

				// Write file so it can be edited by the user
				System.IO.File.WriteAllText(CONFIG_FILE, ApplicationLogger.Properties.Resources.default_config);

				// User config is the same as the default
				configUser = configDefault;
			} else {
				// Read the existing user config
				configUser = new ConfigParser(System.IO.File.ReadAllText(CONFIG_FILE));
			}

			// Interprets config data
			configPath = configUser.getString("path") ?? configDefault.getString("path");
			configIdleTime = configUser.getFloat("idleTime") ?? configDefault.getFloat("idleTime");
			configTimeCheckInterval = configUser.getFloat("checkInterval") ?? configDefault.getFloat("checkInterval");
			configMaxQueueEntries = configUser.getInt("maxQueueEntries") ?? configDefault.getInt("maxQueueEntries");
			configMaxQueueTime = configUser.getFloat("maxQueueTime") ?? configDefault.getFloat("maxQueueTime");
		}

		private void start() {
			if (!isRunning) {
				// Initialize timer
				timerCheck = new Timer();
				timerCheck.Tick += new EventHandler(onTimer);
				timerCheck.Interval = (int)(configTimeCheckInterval * 1000f);
				timerCheck.Start();

				lastUserProcessId = null;
				lastTimeQueueWritten = DateTime.Now;
				isRunning = true;

				updateContextMenu();
				updateTrayIcon();
			}
		}

		private void stop() {
			if (isRunning) {
				logStop();

				timerCheck.Stop();
				timerCheck.Dispose();
				timerCheck = null;

				isRunning = false;

				updateContextMenu();
				updateTrayIcon();
			}
		}

		private void logUserIdle() {
			// Log that the user is idle
			logLine("status::idle", true, false, configIdleTime ?? 0);
			updateText("User idle");
			newUserProcessId = null;
		}

		private void logStop() {
			// Log stopping the application
			logLine("status::stop", true);
			updateText("Stopped");
			newUserProcessId = null;
		}

		private void logEndOfDay() {
			// Log an app focus change after the end of the day, and at the end of the specific log file
			logLine("status::end-of-day", true, true);
			newUserProcessId = null;
		}

		private void logUserProcess(Process process) {
			// Log the current user process

			int dayOfLog = DateTime.Now.Day;

			if (dayOfLog != lastDayLineLogged) {
				// The last line was logged on a different day, so check if it should be a new file
				string newFileName = getLogFileName();

				if (newFileName != lastFileNameSaved && lastFileNameSaved != "") {
					// It's a new file: commit current with an end-of-day event
					logEndOfDay();
				}
			}

			string commandLine;
			try {
				commandLine = GetCommandLine(process);
			} catch (Exception) {
				commandLine = "?";
			}

			try {
				logLine("app::focus", process.ProcessName, process.MainModule.FileName, process.MainWindowTitle, commandLine);
				updateText("Name: " + process.ProcessName + ", " + process.MainWindowTitle);
			} catch (Exception exception) {
				logLine("app::focus", process.ProcessName, "?", "?", commandLine);
				updateText("Name: " + process.ProcessName + ", ?");
			}
		}

		// Define an extension method for type System.Process that returns the command 
		// line via WMI.
		private string GetCommandLine(Process process) {
			string cmdLine = null;
			using (var searcher = new ManagementObjectSearcher(
			  $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}")) {
				// By definition, the query returns at most 1 match, because the process 
				// is looked up by ID (which is unique by definition).
				using (var matchEnum = searcher.Get().GetEnumerator()) {
					if (matchEnum.MoveNext()) // Move to the 1st item.
					{
						cmdLine = matchEnum.Current["CommandLine"]?.ToString();
					}
				}
			}
			if (cmdLine == null) {
				// Not having found a command line implies 1 of 2 exceptions, which the
				// WMI query masked:
				// An "Access denied" exception due to lack of privileges.
				// A "Cannot process request because the process (<pid>) has exited."
				// exception due to the process having terminated.
				// We provoke the same exception again simply by accessing process.MainModule.
				var dummy = process.MainModule; // Provoke exception.
			}
			return cmdLine;
		}

		private void logLine(string type, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0) {
			logLine(type, "", "", "", "", forceCommit, usePreviousDayFileName, idleTimeOffsetSeconds);
		}

		private void logLine(string type, string title, string location, string subject, string commandLine, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0) {
			// Log a single line
			DateTime now = DateTime.Now;

			now.AddSeconds(idleTimeOffsetSeconds);

			lineToLog.Clear();
			lineToLog.Append(now.ToString(DATE_TIME_FORMAT));
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(type);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(Environment.MachineName);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(title);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(location);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(subject);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(commandLine);

			addLineToListView(lineToLog.ToString());

			lineToLog.Append(LINE_END);

			//Console.Write("LOG ==> " + lineToLog.ToString());

			queuedLogMessages.Add(lineToLog.ToString());
			lastDayLineLogged = DateTime.Now.Day;

			if (queuedLogMessages.Count > configMaxQueueEntries || forceCommit) {
				if (usePreviousDayFileName) {
					commitLines(lastFileNameSaved);
				} else {
					commitLines();
				}
			}
		}

		private void addLineToListView(string line) {
			var splitedLine = line.Split(LINE_DIVIDER.ToCharArray());
			ListViewItem row = new ListViewItem();
			foreach (var item in splitedLine) {
				if (string.IsNullOrEmpty(row.Text)) {
					row.Text = item.ToString();
				} else {
					row.SubItems.Add(item.ToString());
				}
			}
			lsvLog.Items.Add(row);
			if (lsvLog.SelectedItems.Count == 0 && ckbAutoScroll.Checked) {
				ScrollToEndListView();
			} else {
				if (ckbAutoScroll.Checked) {
					ckbAutoScroll.Checked = false;
				}
			}
		}

		private void ScrollToEndListView() {
			if (lsvLog.Items.Count > 0) {
				lsvLog.Items[lsvLog.Items.Count - 1].EnsureVisible();
			}
		}

		private void commitLines(string fileName = null) {
			// Commit all currently queued lines to the file

			// If no commit needed, just return
			if (queuedLogMessages.Count == 0) return;

			lineToLog.Clear();
			foreach (var line in queuedLogMessages) {
				lineToLog.Append(line);
			}

			string commitFileName = fileName ?? getLogFileName();
			bool saved = false;

			// Check if the path exists, creating it otherwise
			string filePath = System.IO.Path.GetDirectoryName(commitFileName);
			if (filePath.Length > 0 && !System.IO.Directory.Exists(filePath)) {
				System.IO.Directory.CreateDirectory(filePath);
			}

			try {
				System.IO.File.AppendAllText(commitFileName, lineToLog.ToString());
				saved = true;
			} catch (Exception exception) {
			}

			if (saved) {
				// Saved successfully, now clear the queue
				queuedLogMessages.Clear();

				lastTimeQueueWritten = DateTime.Now;

				updateContextMenu();
			}
		}

		private void updateText(string text) {
			labelApplication.Text = text;
		}

		private void applySettingsRunAtStartup() {
			// Check whether it's properly set to run at startup or not
			if (settingsRunAtStartup) {
				// Should run at startup
				if (!windowsRunAtStartup) windowsRunAtStartup = true;
			} else {
				// Should not run at startup
				if (windowsRunAtStartup) windowsRunAtStartup = false;
			}
		}

		private void showForm() {
			allowShow = true;
			Show();
			WindowState = FormWindowState.Normal;
			Activate();
		}

		private void exit() {
			allowClose = true;
			Close();
		}


		// ================================================================================================================
		// ACCESSOR INTERFACE ---------------------------------------------------------------------------------------------

		private bool settingsRunAtStartup {
			// Whether the settings say the app should run at startup or not
			get {
				return (bool)Settings.Default[SETTINGS_FIELD_RUN_AT_STARTUP];
			}
			set {
				Settings.Default[SETTINGS_FIELD_RUN_AT_STARTUP] = value;
				Settings.Default.Save();
			}
		}

		private bool windowsRunAtStartup {
			// Whether it's actually set to run at startup or not
			get {
				return getStartupRegistryKey().GetValue(SystemHelper.REGISTRY_KEY_ID) != null ||
					SystemHelper.ScheduledTaskParse($"/Query /TN {SystemHelper.REGISTRY_KEY_ID}", SystemHelper.REGISTRY_KEY_ID);
			}
			set {
				if (value) {
					// Add
					if (hasInitialized) {
						if (!SystemHelper.IsRunningAsAdmin()) {
							if (MessageBox.Show("Do you want to run at startup as administrator?", "Startup", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes) {
								if (RunAsAdmin()) {
									using (File.Create(TEMP_WINDOWS_RUN_AT_STARTUP_AS_ADMIN_FILE)) {
										return;
									}
								}
							}
						}

					}

					if (SystemHelper.IsRunningAsAdmin()) {
						if (getStartupRegistryKey().GetValue(SystemHelper.REGISTRY_KEY_ID) != null) {
							getStartupRegistryKey(true).DeleteValue(SystemHelper.REGISTRY_KEY_ID, false);
						}

						//string args = $"/Create /f /tn {SystemHelper.REGISTRY_KEY_ID} /tr {Application.ExecutablePath.ToString()} /sc onlogon /rl highest";
						//Process.Start("schtasks", args);

						SystemHelper.ScheduledTaskParse($"/Create /f /tn {SystemHelper.REGISTRY_KEY_ID} /tr {Application.ExecutablePath.ToString()} /sc onlogon /rl highest",
							"SUCCESS");
					} else {
						getStartupRegistryKey(true).SetValue(SystemHelper.REGISTRY_KEY_ID, Application.ExecutablePath.ToString());
					}
					//Console.WriteLine("RUN AT STARTUP SET AS => TRUE");
				} else {
					// Remove
					getStartupRegistryKey(true).DeleteValue(SystemHelper.REGISTRY_KEY_ID, false);
					//Console.WriteLine("RUN AT STARTUP SET AS => FALSE");

					SystemHelper.ScheduledTaskParse($"/Delete /F /TN {SystemHelper.REGISTRY_KEY_ID}", "SUCCESS");
					//string args = $"/Delete /f /tn {SystemHelper.REGISTRY_KEY_ID}";
					//Process.Start("schtasks", args);
					//string error = p.StandardError.ReadToEnd();
					//p.WaitForExit();

					//if (!string.IsNullOrEmpty(error))
					//{

					//}
				}
			}
		}

		private RegistryKey getStartupRegistryKey(bool writable = false) {
			return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable);
		}

		private Process getCurrentUserProcess() {
			// Find the process that's currently on top
			var processes = Process.GetProcesses();
			var foregroundWindowHandle = SystemHelper.GetForegroundWindow();

			foreach (var process in processes) {
				if (process.Id <= 4) { continue; } // system processes
				if (process.MainWindowHandle == foregroundWindowHandle) return process;
			}

			// Nothing found!
			return null;
		}

		private string getLogFileName() {
			// Get the log filename for something to be logged now
			var now = DateTime.Now;
			var filename = configPath;

			// Replaces variables
			filename = filename.Replace("[[month]]", now.ToString("MM"));
			filename = filename.Replace("[[day]]", now.ToString("dd"));
			filename = filename.Replace("[[year]]", now.ToString("yyyy"));
			filename = filename.Replace("[[machine]]", Environment.MachineName);

			var pathOnly = System.IO.Path.GetDirectoryName(filename);
			var fileOnly = System.IO.Path.GetFileName(filename);

			// Make it safe
			foreach (char c in System.IO.Path.GetInvalidFileNameChars()) {
				fileOnly = fileOnly.Replace(c, '_');
			}

			return (pathOnly.Length > 0 ? pathOnly + "\\" : "") + fileOnly;
		}

		private void BtnClearLog_Click(object sender, EventArgs e) {
			lsvLog.Items.Clear();
		}

		private void CkbAutoScroll_CheckedChanged(object sender, EventArgs e) {
			if (ckbAutoScroll.Checked) {
				if (lsvLog.SelectedItems.Count > 0) {
					lsvLog.SelectedItems[0].Selected = false;
				}
				ScrollToEndListView();
			}
		}

		private void btnRunAs_Click(object sender, EventArgs e) {
			RunAsAdmin();
		}

		private bool RunAsAdmin() {
			try {
				ProcessStartInfo elevated = new ProcessStartInfo(System.Reflection.Assembly.GetEntryAssembly().Location) {
					UseShellExecute = true,
					Verb = "runas"
				};

				// Restart the program
				using (Process process = Process.Start(elevated)) {
					commitLines();

					ExportListViewItemsToFile(TEMP_LOG_FILE);

					exit();

					//close this one
					//Process.GetCurrentProcess().Kill();

					return true;

				}
			} catch (Exception ex) {
				File.Delete(TEMP_LOG_FILE);
				return false;
			}

		}

		private void ExportListViewItemsToFile(string fileName) {
			StringBuilder builder = new StringBuilder();

			for (int i = 0; i < lsvLog.Items.Count; i++) {
				for (int j = 0; j < lsvLog.Items[i].SubItems.Count; j++) {
					builder.Append(lsvLog.Items[i].SubItems[j].Text);
					if (j < lsvLog.Items[i].SubItems.Count) {
						builder.Append(LINE_DIVIDER);
					}
				}
				if (i < lsvLog.Items.Count) {
					builder.Append(LINE_END);
				}
			}

			try {
				File.WriteAllText(fileName, builder.ToString());
			} catch (Exception) {
			}
		}

		private void ImportListViewItemsFromFile(string fileName, bool deleteFile) {
			try {
				var fileLines = File.ReadAllLines(fileName);

				for (int i = 0; i < fileLines.Length; i++) {
					addLineToListView(fileLines[i]);
				}
				if (deleteFile) {
					File.Delete(fileName);
				}
				showForm();
			} catch (Exception ex) {
			}
		}

		private void LsvLog_DoubleClick(object sender, EventArgs e) {
			OpenFolderInFileExplorer();
		}
	}
}
