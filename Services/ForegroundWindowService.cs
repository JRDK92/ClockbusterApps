using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ClockbusterApps.Services
{
    public class ForegroundWindowService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // List of common system processes to exclude
        private static readonly HashSet<string> SystemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer", "dwm", "taskmgr", "taskhost", "taskhostw",
            "csrss", "winlogon", "services", "lsass", "svchost",
            "searchui", "searchapp", "startmenuexperiencehost",
            "shellexperiencehost", "runtimebroker", "applicationframehost",
            "systemsettings", "lockapp", "sihost", "fontdrvhost",
            "conhost", "backgroundtaskhost", "securityhealthsystray",
            "textinputhost", "EAAntiCheat.GameServiceLauncher", "SnippingTool"
        };

        public class ForegroundAppInfo
        {
            public string ProcessName { get; set; }
            public string WindowTitle { get; set; }
            public int ProcessId { get; set; }
            public bool IsSystemProcess { get; set; }
        }

        public static ForegroundAppInfo GetForegroundApplicationInfo()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    return null;

                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);
                Process process = Process.GetProcessById((int)processId);
                string processName = process.ProcessName;

                StringBuilder windowTitle = new StringBuilder(256);
                GetWindowText(hwnd, windowTitle, 256);
                string title = windowTitle.ToString();

                // Skip if it's a system process
                bool isSystemProcess = SystemProcesses.Contains(processName) ||
                                      string.IsNullOrWhiteSpace(title);

                return new ForegroundAppInfo
                {
                    ProcessName = processName,
                    WindowTitle = title,
                    ProcessId = (int)processId,
                    IsSystemProcess = isSystemProcess
                };
            }
            catch
            {
                return null;
            }
        }
    }
}