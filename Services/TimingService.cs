using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

namespace ClockbusterApps.Services
{
    // 1. Session Model (Requirement 1)
    public class Session
    {
        public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 8) + "-" + Guid.NewGuid().ToString().Substring(0, 4);
        public string ApplicationName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int ProcessId { get; set; }
        public double DurationMinutes
        {
            get
            {
                if (!EndTime.HasValue)
                    return (DateTime.Now - StartTime).TotalMinutes;
                return (EndTime.Value - StartTime).TotalMinutes;
            }
        }
    }

    public class TimingService
    {
        private System.Timers.Timer _timer;
        private readonly string _logFilePath;

        // Track all running applications by ProcessId
        private Dictionary<int, Session> _runningSessions = new Dictionary<int, Session>();

        // Track which PIDs we've seen to detect new launches
        private HashSet<int> _knownProcessIds = new HashSet<int>();

        public TimingService()
        {
            _logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockbusterApps",
                "timeclock.log"
            );

            var directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        // Helper for UI to get the currently focused app's session
        public Session GetCurrentSession()
        {
            try
            {
                var appInfo = ForegroundWindowService.GetForegroundApplicationInfo();
                if (appInfo != null && !appInfo.IsSystemProcess && _runningSessions.ContainsKey(appInfo.ProcessId))
                {
                    return _runningSessions[appInfo.ProcessId];
                }
            }
            catch { }

            return null;
        }

        public IReadOnlyCollection<Session> GetActiveSessions()
        {
            return _runningSessions.Values.ToList().AsReadOnly();
        }

        public void Start(bool trackExistingApplications = false)
        {
            if (_timer != null) return;

            // Initialize with currently running processes
            InitializeRunningProcesses(trackExistingApplications);

            _timer = new System.Timers.Timer(2000); // Check every 2 seconds
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer == null) return;

            // Close all active sessions
            var sessionsToClose = _runningSessions.Values.ToList();
            foreach (var session in sessionsToClose)
            {
                session.EndTime = DateTime.Now;
                LogSession(session);
            }

            _timer.Stop();
            _timer.Dispose();
            _timer = null;
            _runningSessions.Clear();
            _knownProcessIds.Clear();
        }

        private void InitializeRunningProcesses(bool createSessions)
        {
            try
            {
                var processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        int pid = process.Id;

                        if (IsSystemProcess(process.ProcessName))
                            continue;

                        // Always add to known PIDs
                        _knownProcessIds.Add(pid);

                        // Only create sessions if option is enabled and process has a main window
                        if (createSessions && process.MainWindowHandle != IntPtr.Zero)
                        {
                            var newSession = new Session
                            {
                                Id = Guid.NewGuid().ToString("N").Substring(0, 8) + "-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                                ApplicationName = process.ProcessName,
                                StartTime = DateTime.Now,
                                EndTime = null,
                                ProcessId = pid
                            };

                            _runningSessions[pid] = newSession;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var currentProcesses = Process.GetProcesses();
                var currentPids = new HashSet<int>();

                // Detect NEW processes (just launched)
                foreach (var process in currentProcesses)
                {
                    try
                    {
                        int pid = process.Id;
                        currentPids.Add(pid);

                        // Skip system processes
                        if (IsSystemProcess(process.ProcessName))
                            continue;
                        // Only track processes with a main window
                        if (process.MainWindowHandle == IntPtr.Zero)
                            continue;

                        // NEW process detected!
                        if (!_knownProcessIds.Contains(pid))
                        {
                            _knownProcessIds.Add(pid);

                            var newSession = new Session
                            {
                                Id = Guid.NewGuid().ToString("N").Substring(0, 8) + "-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                                ApplicationName = process.ProcessName,
                                StartTime = DateTime.Now,
                                EndTime = null,
                                ProcessId = pid
                            };

                            _runningSessions[pid] = newSession;
                        }
                    }
                    catch { }
                }

                // Detect CLOSED processes
                var closedPids = _runningSessions.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
                foreach (var pid in closedPids)
                {
                    var session = _runningSessions[pid];
                    session.EndTime = DateTime.Now;

                    // Only log if session was at least 10 seconds
                    if ((session.EndTime.Value - session.StartTime).TotalSeconds >= 10)
                    {
                        LogSession(session);
                    }

                    _runningSessions.Remove(pid);
                    _knownProcessIds.Remove(pid);
                }
            }
            catch
            {
                // Silently handle errors
            }
        }

        private bool IsSystemProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return true;

            var systemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "explorer", "dwm", "taskmgr", "taskhost", "taskhostw",
                "csrss", "winlogon", "services", "lsass", "svchost",
                "searchui", "searchapp", "startmenuexperiencehost",
                "shellexperiencehost", "runtimebroker", "applicationframehost",
                "systemsettings", "lockapp", "sihost", "fontdrvhost",
                "conhost", "backgroundtaskhost", "securityhealthsystray",
                "textinputhost", "idle", "system", "livepreviewsurface", "nahimic", "githubdesktop",
                "discord", "steam", "epicgameslauncher", "devenv", "setup", "steamwebhelper", "clockbusterApps",
                "RazerAppEngine", "vksts"
            };

            return systemProcesses.Contains(processName);
        }

        private void LogSession(Session session)
        {
            // Requirement 5: Pipe-delimited format
            // SessionID|ApplicationName|StartDateTime|EndDateTime|DurationInMinutes

            var line = string.Format("{0}|{1}|{2}|{3}|{4:F2}",
                session.Id,
                session.ApplicationName,
                session.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                session.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                session.DurationMinutes);

            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
            catch
            {
                // File access retry logic could go here
            }
        }
    }
}