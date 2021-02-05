using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ApplicationLogger {
	static class Program {

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (SystemHelper.ScheduledTaskParse($"/Query /TN {SystemHelper.REGISTRY_KEY_ID}", SystemHelper.REGISTRY_KEY_ID) &&
			!SystemHelper.IsRunningAsAdmin()) {
				try {
					ProcessStartInfo startInfo = new ProcessStartInfo() {
						FileName = "schtasks.exe",
						UseShellExecute = false,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
						Arguments = $"/Run /tn {SystemHelper.REGISTRY_KEY_ID}",
						RedirectStandardOutput = true
					};

					using (Process process = Process.Start(startInfo)) {
						using (System.IO.StreamReader reader = process.StandardOutput) {
							string stdout = reader.ReadToEnd();
							if (stdout.Contains("SUCCESS")) {
								//MessageBox.Show("Restarting!");
								Application.Exit();

								return;
							}
						}
					}
				} catch (Exception ex) {
					//MessageBox.Show("Application error");
				}
			}

			// Check if it's already running
			if (Process.GetProcessesByName(SystemHelper.REGISTRY_KEY_ID).Length > 1 && !System.Diagnostics.Debugger.IsAttached) {
				// Already running!
				Console.WriteLine("Application already running, will exit");
				//MessageBox.Show("Application already running, will exit");
				Application.Exit();
			} else {
				//MessageBox.Show("Application will running");
				Application.Run(new MainForm());
			}
		}
	}
}
