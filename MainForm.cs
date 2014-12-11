﻿using ApplicationLogger.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ApplicationLogger {

	public partial class MainForm : Form {

		// Constants
		private const string SETTINGS_FIELD_PATH_TEMPLATE = "PathTemplate";
		private const long IDLE_TIME = 10L * 60L * 1000L;							// Time to be considered idle, in ms; 10 minutes
		private const int TIME_CHECK_INTERVAL = 500;								// Time interval to check processes, in ms
		private const string LINE_DIVIDER = "\t";
		private const string LINE_END = "\r\n";
		private const string DATE_TIME_FORMAT = "o";								// 2008-06-15T21:15:07.0000000
		private const int MAX_LINES_TO_QUEUE = 20;									// Max lines to queue before writing to file

		// Properties
		private Timer timerCheck;
		private ContextMenu contextMenu;
		private MenuItem menuItemOpen;
		private MenuItem menuItemStartStop;
		private MenuItem menuItemExit;
		private bool isClosing;
		private bool isStarted;
		private bool isUserIdle;
		private string lastUserProcessId;
		private List<string> queuedLogMessages;

		private string newUserProcessId;											// Temp
		private DateTime now;														// Temp, used for getting the time
		private StringBuilder lineToLog;											// Temp, used to create the line

		// ================================================================================================================
		// CONSTRUCTOR ----------------------------------------------------------------------------------------------------

		public MainForm() {
			InitializeComponent();
		}


		// ================================================================================================================
		// EVENT INTERFACE ------------------------------------------------------------------------------------------------

		private void onFormLoad(object sender, EventArgs e) {
			// Just loaded everything

			// Initialize
			isClosing = false;
			isStarted = false;
			queuedLogMessages = new List<string>();
			lineToLog = new StringBuilder();

			// Create context menu for the tray icon
			createContextMenu();

			// Initialize notification icon
			notifyIcon.Icon = ApplicationLogger.Properties.Resources.trayIcon;
			notifyIcon.ContextMenu = contextMenu;

			// Initialize UI
			if (getSavedPathTemplate() == null || getSavedPathTemplate() == "") setSavedPathTemplate("logs/[[year]]_[[month]].log");
			textPathTemplate.Text = getSavedPathTemplate();

			// Finally, start
			start();
		}

		private void onFormClosing(object sender, FormClosingEventArgs e) {
			// Form is attempting to close
			if (!isClosing) {
				// User initiated, just minimize instead
				e.Cancel = true;
				Hide();
			} else {
				// Actually closing
				stop();
			}
		}

		private void onTimer(object sender, EventArgs e) {
			// Timer tick: check for the current application

			// Check the user is idle
			if (Win32.GetIdleTime() >= IDLE_TIME) {
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
		}

		private void onResize(object sender, EventArgs e) {
			// Resized window
			//notifyIcon.BalloonTipTitle = "Minimize to Tray App";
			//notifyIcon.BalloonTipText = "You have successfully minimized your form.";

			if (FormWindowState.Minimized == this.WindowState) {
				//notifyIcon.ShowBalloonTip(500);
				this.Hide();    
			}
		}

		private void onMenuItemOpenClicked(object Sender, EventArgs e) {
			Show();
			WindowState = FormWindowState.Normal;
		}

		private void onMenuItemStartStopClicked(object Sender, EventArgs e) {
			if (isStarted) {
				stop();
			} else {
				start();
			}
		}

		private void onMenuItemExitClicked(object Sender, EventArgs e) {
			exit();
		}

		private void onDoubleClickNotificationIcon(object sender, MouseEventArgs e) {
			Show();
			WindowState = FormWindowState.Normal;
		}

		private void onClickSave(object sender, EventArgs e) {
			// Save options
			setSavedPathTemplate(textPathTemplate.Text);
		}


		// ================================================================================================================
		// INTERNAL INTERFACE ---------------------------------------------------------------------------------------------

		private void createContextMenu() {
			// Initialize context menu
			contextMenu = new ContextMenu();

			// Initialize menu items
			menuItemOpen = new MenuItem();
			menuItemOpen.Index = 0;
			menuItemOpen.Text = "&Open";
			menuItemOpen.Click += new EventHandler(onMenuItemOpenClicked);

			menuItemStartStop = new MenuItem();
			menuItemStartStop.Index = 0;
			menuItemStartStop.Text = "";
			menuItemStartStop.Click += new EventHandler(onMenuItemStartStopClicked);

			menuItemExit = new MenuItem();
			menuItemExit.Index = 1;
			menuItemExit.Text = "E&xit";
			menuItemExit.Click += new EventHandler(onMenuItemExitClicked);

			contextMenu.MenuItems.AddRange(new MenuItem[] {menuItemOpen, menuItemStartStop, menuItemExit});

			updateContextMenu();
		}

		private void updateContextMenu() {
			if (menuItemStartStop != null) {
				if (isStarted) {
					menuItemStartStop.Text = "&Stop";
				} else {
					menuItemStartStop.Text = "&Start";
				}
			}
		}

		private void start() {
			if (!isStarted) {
				// Initialize timer
				timerCheck = new Timer();
				timerCheck.Tick += new EventHandler(onTimer);
				timerCheck.Interval = TIME_CHECK_INTERVAL;
				timerCheck.Start();

				lastUserProcessId = null;
				isStarted = true;

				updateContextMenu();
			}
		}

		private void stop() {
			if (isStarted) {
				logStop();

				timerCheck.Stop();
				timerCheck.Dispose();
				timerCheck = null;

				isStarted = false;

				updateContextMenu();
			}
		}

		private void logUserIdle() {
			// Log that the user is idle
			logLine("status::idle");
			updateText("User idle");

			commitLines();
		}

		private void logUserProcess(Process process) {
			// Log the current user process
			try {
				logLine("app::focus", process.ProcessName, process.MainModule.FileName, process.MainWindowTitle);
				updateText("Name: " + process.ProcessName + ", " + process.MainWindowTitle);
			} catch (Exception exception) {
				logLine("app::focus", process.ProcessName, "?", "?");
				updateText("Name: ?");
			}
		}

		private void logStop() {
			// Log stopping the application
			logLine("status::stop");
			updateText("Stopped");

			commitLines();
		}

		private void logLine(string type, string title = "", string location = "", string subject = "") {
			// Log a single line
			now = DateTime.Now;
			
			lineToLog.Clear();
			lineToLog.Append(now.ToString(DATE_TIME_FORMAT));
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(type);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(title);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(location);
			lineToLog.Append(LINE_DIVIDER);
			lineToLog.Append(subject);
			lineToLog.Append(LINE_END);

			queuedLogMessages.Add(lineToLog.ToString());

			//Console.Write("LOG ==> " + lineToLog.ToString());

			if (queuedLogMessages.Count > MAX_LINES_TO_QUEUE) commitLines();
		}

		private void commitLines() {
			// Commit all currently queued lines to the file

			lineToLog.Clear();
			foreach (var line in queuedLogMessages) {
				lineToLog.Append(line);
			}

			now = DateTime.Now;
			string fileName = getSavedPathTemplate().Replace("[[month]]", now.ToString("MM")).Replace("[[day]]", now.ToString("dd")).Replace("[[year]]", now.ToString("yyyy"));
			bool saved = false;

			try {
				System.IO.File.AppendAllText(fileName, lineToLog.ToString());
				saved = true;
			} catch (Exception exception) {
			}

			if (saved) {
				// Saved successfully, now clear the queue
				queuedLogMessages.Clear();
			}
		}

		private void updateText(string text) {
			labelApplication.Text = text;
		}

		private void exit() {
			isClosing = true;
			Close();
		}

		private Process getCurrentUserProcess() {
			// Find the process that's currently on top
			var procs = new List<Process>();

            var processListSnapshot = Process.GetProcesses();
			var foregroundWindowHandle = Win32.GetForegroundWindow();

            foreach (var process in processListSnapshot) {
                if (process.Id <= 4) { continue; } // system processes
				if (process.MainWindowHandle == foregroundWindowHandle) return process;
            }

			// Nothing found!
			return null;
		}

		private string getSavedPathTemplate() {
			return Settings.Default[SETTINGS_FIELD_PATH_TEMPLATE] as string;
		}

		private void setSavedPathTemplate(string pathTemplate) {
			Settings.Default[SETTINGS_FIELD_PATH_TEMPLATE] = pathTemplate;
			Settings.Default.Save();
		}


		// ================================================================================================================
		// INTERNAL CLASSES -----------------------------------------------------------------------------------------------

		internal struct LASTINPUTINFO {
			public uint cbSize;
			public uint dwTime;
		}

		public class Win32 {

			// System calls
			[DllImport("User32.dll")]
			private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

			[DllImport("Kernel32.dll")]
			private static extern uint GetLastError();

			[DllImport("user32.dll", CharSet=CharSet.Auto)]
			public static extern bool IsWindowVisible(IntPtr hWnd);

			[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
			public static extern IntPtr GetForegroundWindow();

			public static uint GetIdleTime() {
				LASTINPUTINFO lastInPut = new LASTINPUTINFO();
				lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
				GetLastInputInfo(ref lastInPut);

				return ((uint)Environment.TickCount - lastInPut.dwTime);
			}

			public static long GetTickCount() {
				return Environment.TickCount;
			}

			public static long GetLastInputTime() {
				LASTINPUTINFO lastInPut = new LASTINPUTINFO();
				lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
				if (!GetLastInputInfo(ref lastInPut)) {
					throw new Exception(GetLastError().ToString());
				}

				return lastInPut.dwTime;
			}
		}

	}
}