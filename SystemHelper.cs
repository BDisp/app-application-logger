using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;

public class SystemHelper {
	public const string REGISTRY_KEY_ID = "ApplicationLogger";                 // Registry app key for when it's running at startup

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

	internal struct LASTINPUTINFO {
		public uint cbSize;
		public uint dwTime;
	}

	public static bool ScheduledTaskParse(string arguments, string output) {
		ProcessStartInfo start = new ProcessStartInfo() {
			FileName = "schtasks.exe", // Specify exe name.
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			//Arguments = $"/Query /TN {taskname}",
			Arguments = arguments,
			RedirectStandardOutput = true
		};

		// Start the process.
		using (Process process = Process.Start(start)) {
			// Read in all the text from the process with the StreamReader.
			using (StreamReader reader = process.StandardOutput) {
				string stdout = reader.ReadToEnd();
				if (stdout.Contains(output)) {
					return true;
				} else {
					return false;
				}
			}
		}
	}

	public static bool IsRunningAsAdmin() {
		return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
					 .IsInRole(WindowsBuiltInRole.Administrator);
	}
}