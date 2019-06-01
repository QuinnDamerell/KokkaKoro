using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public enum ServiceBotState
    {
        // The bot is waiting to be started
        NotStarted,
        // The process has been started, but the bot hasn't connected
        Starting,
        // The bot is joined to the game.
        Joined,
        // The process is being killed
        Terminated,
        // The process is dead and done cleaning up.
        CleanedUp
    }

    public class ServiceBot
    {
        KokkaKoroBot m_info;
        string m_localPath;
        bool m_wasLoadedFromCache;

        // Passed to the bot to help it connect
        string m_userName;
        Guid m_gameId;
        string m_gamePassword;

        Thread m_processMonitor;
        ServiceBotState m_state;
        object m_stateLock = new object();

        // Output
        string m_fatalError = null;
        string m_stdOut = null;
        string m_stdErr = null;

        public ServiceBot(KokkaKoroBot info, string localPath, bool wasInCache, string userName, Guid gameId, string gamePassword)
        {
            m_info = info;
            m_localPath = localPath;
            m_wasLoadedFromCache = wasInCache;
            m_state = ServiceBotState.NotStarted;

            m_userName = userName;
            m_gameId = gameId;
            m_gamePassword = gamePassword;
        }

        public void SetGameO

        public bool StartBot()
        {
            lock(m_stateLock)
            {
                if(m_state != ServiceBotState.NotStarted)
                {
                    return false;
                }
                m_state = ServiceBotState.Starting;
            }
            m_processMonitor = new Thread(ProcessMontior);
            m_processMonitor.Start();
            return true;
        }

        private void ProcessMontior()
        {
            lock (m_stateLock)
            {
                if (m_state != ServiceBotState.Starting)
                {
                    SetFatalError("Thread not started in correct state.");
                    return;
                }
            }

            Process process = new Process();
            try
            {
                InnerProcessMonitor(process);
            }
            catch(Exception e)
            {
                string msg = $"Exception thrown in bot loop. {e.Message}";
                Logger.Error(msg, e);
                SetFatalError(msg);
            }

            // Update the state
            lock (m_stateLock)
            {
                m_state = ServiceBotState.Terminated;
            }

            // Ensure the process is dead
            try
            {
                process.Kill();
            }
            catch { }

            // Try to grab the outputs
            try
            {
                m_stdOut = process.StandardOutput.ReadToEnd();
            }
            catch { }
            try
            {
                m_stdErr = process.StandardError.ReadToEnd();
            }
            catch { }

            // Update the state
            lock (m_stateLock)
            {
                m_state = ServiceBotState.CleanedUp;
            }
        }

        private void InnerProcessMonitor(Process process)
        {
            const string c_userNameKey = "UserName";
            const string c_userPasscodeKey = "Passcode";
            const string c_gameIdKey = "GameId";
            const string c_gamePasswordKey = "GamePassword";
            const string s_botPassword = "IamABot";

            // Create the starting args.
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = GetExePath(),
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = m_localPath
            };

            // Pass the game vars to the bot.
            info.EnvironmentVariables.Add(c_userNameKey, m_userName);
            info.EnvironmentVariables.Add(c_userPasscodeKey, s_botPassword);
            info.EnvironmentVariables.Add(c_gameIdKey, m_gameId.ToString());
            if (!String.IsNullOrWhiteSpace(m_gamePassword))
            {
                info.EnvironmentVariables.Add(c_gamePasswordKey, m_gamePassword);
            }

            // Create and start the process.
            process.StartInfo = info;
            process.Start();

            // Wait for the process to die.
            while(m_state == ServiceBotState.Starting || m_state == ServiceBotState.Joined)
            {
                // Wake up every 500ms to check state.
                process.WaitForExit(500);
                if(process.HasExited)
                {
                    break;
                }
            }
        }

        private void SetFatalError(string msg)
        {
            if(String.IsNullOrWhiteSpace(msg))
            {
                m_fatalError = msg;
            }
        }

        public void Kill()
        {
            // Set the state to Terminated, this will kill the process thread.
            m_state = ServiceBotState.Terminated;
        }

        public void WaitForCleanup()
        {
            while(true)
            {
                lock(m_stateLock)
                {
                    if(m_state == ServiceBotState.CleanedUp)
                    {
                        return;
                    }
                }
                Thread.Sleep(100);
            }
        }

        public void SetBotJoined()
        {
            lock(m_stateLock)
            {
                // Only move to joined if we are currently starting.
                if(m_state == ServiceBotState.Starting)
                {
                    m_state = ServiceBotState.Joined;
                }
            }
        }

        public bool GetIsReady()
        {
            return m_state == ServiceBotState.Joined;
        }

        private string GetExePath()
        {
            return $"{m_localPath}/{m_info.EntryDll}";
        }

        public string GetBotName()
        {
            return m_info.Name;
        }

        public KokkaKoroBot GetBotInfo()
        {
            return m_info;
        }

        public bool WasInCache()
        {
            return m_wasLoadedFromCache;
        }
    }
}
