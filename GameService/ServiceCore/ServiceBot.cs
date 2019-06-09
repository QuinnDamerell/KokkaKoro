using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        string m_userPasscode;
        Guid m_gameId;
        string m_gamePassword;

        Thread m_processMonitor;
        ServiceBotState m_state;
        object m_stateLock = new object();

        // Output
        string m_fatalError = null;
        StringBuilder m_stdOutput = new StringBuilder();
        StringBuilder m_stdError = new StringBuilder();

        public ServiceBot(KokkaKoroBot info, string localPath, bool wasInCache)
        {
            m_info = info;
            m_localPath = localPath;
            m_wasLoadedFromCache = wasInCache;
            m_state = ServiceBotState.NotStarted;       
        }

        public bool StartBot(Guid gameId, string gamePassword, string userName, string passcode)
        {
            lock(m_stateLock)
            {
                if(m_state != ServiceBotState.NotStarted)
                {
                    return false;
                }
                m_state = ServiceBotState.Starting;
            }

            m_userName = userName;
            m_userPasscode = passcode;
            m_gameId = gameId;
            m_gamePassword = gamePassword;

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

            Console.WriteLine(m_stdOutput.ToString());

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
            const string c_localServiceAddress = "LocalServiceAddress";

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
            info.EnvironmentVariables[c_userNameKey] = m_userName;
            info.EnvironmentVariables[c_userPasscodeKey] = m_userPasscode;
            info.EnvironmentVariables[c_gameIdKey] = m_gameId.ToString();
            string hostName = Utils.GetServiceLocalAddress() == null ? "wss://kokkakoro.azurewebsites.net" : $"ws://{Utils.GetServiceLocalAddress()}";
            info.EnvironmentVariables[c_localServiceAddress] = hostName;
            if (!String.IsNullOrWhiteSpace(m_gamePassword))
            {
                info.EnvironmentVariables[c_gamePasswordKey] = m_gamePassword;
            }

            // Create and start the process.
            process.StartInfo = info;

            // Attach to get the std out and error.
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            // Start
            process.Start();

            // And start the reading.
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for the process to die.
            while (m_state == ServiceBotState.Starting || m_state == ServiceBotState.Joined)
            {
                // Wake up every 500ms to check state.
                process.WaitForExit(500);
                if(process.HasExited)
                {
                    break;
                }
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Add to the string builder.
            m_stdError.Append("\r\n");
            m_stdError.Append(e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // Add to the string builder.
            m_stdOutput.Append("\r\n");
            m_stdOutput.Append(e.Data);
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

        public bool IsReady()
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

        public string GetStdOut()
        {
            return m_stdOutput.ToString();
        }

        public string GetStdErr()
        {
            return m_stdError.ToString();
        }
    }
}
