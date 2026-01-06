using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace ClockbusterApps.Services
{
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
        private readonly DatabaseService _databaseService;
        private Dictionary<int, Session> _runningSessions = new Dictionary<int, Session>();
        private HashSet<int> _knownProcessIds = new HashSet<int>();
        private HashSet<string> _userIgnoredProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TimingService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public void UpdateIgnoredProcesses(List<string> ignoredList)
        {
            _userIgnoredProcesses = new HashSet<string>(ignoredList, StringComparer.OrdinalIgnoreCase);

            var sessionsToKill = _runningSessions.Values
                .Where(s => _userIgnoredProcesses.Contains(s.ApplicationName))
                .ToList();

            foreach (var session in sessionsToKill)
            {
                session.EndTime = DateTime.Now;
                LogSession(session);
                _runningSessions.Remove(session.ProcessId);
                _knownProcessIds.Remove(session.ProcessId);
            }
        }

        public Session GetCurrentSession()
        {
            try
            {
                var appInfo = ForegroundWindowService.GetForegroundApplicationInfo();
                if (appInfo != null &&
                    !IsSystemProcess(appInfo.ProcessName) &&
                    _runningSessions.ContainsKey(appInfo.ProcessId))
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
            InitializeRunningProcesses(trackExistingApplications);
            _timer = new System.Timers.Timer(2000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer == null) return;
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
                        if (IsSystemProcess(process.ProcessName)) continue;

                        _knownProcessIds.Add(pid);

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

                foreach (var process in currentProcesses)
                {
                    try
                    {
                        int pid = process.Id;
                        currentPids.Add(pid);

                        if (IsSystemProcess(process.ProcessName)) continue;
                        if (process.MainWindowHandle == IntPtr.Zero) continue;

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

                var closedPids = _runningSessions.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
                foreach (var pid in closedPids)
                {
                    var session = _runningSessions[pid];
                    session.EndTime = DateTime.Now;
                    if ((session.EndTime.Value - session.StartTime).TotalSeconds >= 10)
                    {
                        LogSession(session);
                    }
                    _runningSessions.Remove(pid);
                    _knownProcessIds.Remove(pid);
                }
            }
            catch { }
        }

        private bool IsSystemProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return true;

            if (_userIgnoredProcesses.Contains(processName)) return true;

            var systemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "explorer", "dwm", "taskmgr", "taskhost", "taskhostw",
                "csrss", "winlogon", "services", "lsass", "svchost",
                "searchui", "searchapp", "startmenuexperiencehost",
                "shellexperiencehost", "runtimebroker", "applicationframehost",
                "systemsettings", "lockapp", "sihost", "fontdrvhost",
                "conhost", "backgroundtaskhost", "securityhealthsystray",
                "textinputhost", "idle", "system", "livepreviewsurface", "nahimic",
                "epicgameslauncher", "devenv", "setup", "steamwebhelper", "clockbusterApps",
                "RazerAppEngine", "vksts"
            };

            return systemProcesses.Contains(processName);
        }

        private void LogSession(Session session)
        {
            try
            {
                _databaseService.InsertSession(
                    session.Id,
                    session.ApplicationName,
                    session.StartTime,
                    session.EndTime,
                    session.DurationMinutes,
                    false
                );
            }
            catch { }
        }
    }
}