using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NK2Tray
{
    public class AudioDevice
    {
        public MidiDevice midiDevice;
        public MMDeviceCollection outputDevices;
        public MMDeviceCollection inputDevices;


        private List<MixerSession> mixerSessionListCache;
        private long currentCacheDate;
        private int cacheExpireTime = 1500;

        private void UpdateDevices()
        {
            // Add audio devices to each fader menu items
            var deviceEnumerator = new MMDeviceEnumerator();

            //device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            //device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            //deviceVolume = device.AudioEndpointVolume;

            outputDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            inputDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            
            foreach (MMDevice mmDevice in outputDevices)
            {
                mmDevice.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            }

            foreach (MMDevice mmDevice in inputDevices)
            {
                mmDevice.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            }
        }

        public AudioEndpointVolume GetDeviceVolumeObject(String deviceIdentifier)
        {
            // Used to handle "COM object that has been separated from its underlying RCW cannot be used" issue.
            UpdateDevices();
            return GetDeviceByIdentifier(deviceIdentifier).AudioEndpointVolume;
        }

        private void OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            Console.WriteLine("OnSessionCreated");        
            midiDevice.LoadAssignments();

            // These correspond with the below events handlers
            //NAudioEventCallbacks callbacks = new NAudioEventCallbacks();
            //AudioSessionEventsCallback notifications = new AudioSessionEventsCallback(callbacks);
            //audioSession.RegisterEventClient(callbacks);
        }



        /*
        // Saving these for later because I'll definitely need them.
        public class NAudioEventCallbacks : IAudioSessionEventsHandler
        {
            public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { Console.WriteLine("OnChannelVolumeChanged"); }

            public void OnDisplayNameChanged(string displayName) { Console.WriteLine("OnDisplayNameChanged"); }

            public void OnGroupingParamChanged(ref Guid groupingId) { Console.WriteLine("OnGroupingParamChanged"); }

            public void OnIconPathChanged(string iconPath) { Console.WriteLine("OnIconPathChanged"); }

            public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) { Console.WriteLine("OnSessionDisconnected"); }

            public void OnStateChanged(AudioSessionState state) { Console.WriteLine("OnStateChanged"); }

            public void OnVolumeChanged(float volume, bool isMuted) { Console.WriteLine("OnVolumeChanged"); }
        }
        */

        public List<MixerSession> GetCachedMixerSessions()
        {
            if (mixerSessionListCache != null && (currentCacheDate + cacheExpireTime > DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond))
            {
                return mixerSessionListCache;
            }

            mixerSessionListCache = GetMixerSessions();
            currentCacheDate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            return mixerSessionListCache;
        }

        private List<MixerSession> GetMixerSessions()
        {
            var mixerSessions = new List<MixerSession>();
            var sessionsByIdent = new Dictionary<String, List<AudioSessionControl>>();

            UpdateDevices();
            for (int j = 0; j < outputDevices.Count; j++)
            {
                var sessions = outputDevices[j].AudioSessionManager.Sessions;

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (session.State != AudioSessionState.AudioSessionStateExpired)
                    {
                        String searchIdentifier = session.GetSessionIdentifier.Substring(session.GetSessionIdentifier.IndexOf("|") + 1, session.GetSessionIdentifier.Length - session.GetSessionIdentifier.IndexOf("|") - 1);
                        if (!sessionsByIdent.ContainsKey(searchIdentifier))
                            sessionsByIdent[searchIdentifier] = new List<AudioSessionControl>();

                        sessionsByIdent[searchIdentifier].Add(session);
                    }
                }
            }

            /*
            // Commented out Iput device code because it breaks the session lists when input and ouput is controlled at the same time
            for (int j = 0; j < inputDevices.Count; j++)
            {
                var sessions = inputDevices[j].AudioSessionManager.Sessions;
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (session.State != AudioSessionState.AudioSessionStateExpired)
                    {
                        String searchIdentifier = session.GetSessionIdentifier.Substring(session.GetSessionIdentifier.IndexOf("|") + 1, session.GetSessionIdentifier.Length - session.GetSessionIdentifier.IndexOf("|") - 1);
                        if (!sessionsByIdent.ContainsKey(searchIdentifier))
                            sessionsByIdent[searchIdentifier] = new List<AudioSessionControl>();

                        sessionsByIdent[searchIdentifier].Add(session);
                    }
                }
            }
            */

            foreach (var ident in sessionsByIdent.Keys.ToList())
            {
                var identSessions = sessionsByIdent[ident]; //.OrderBy(i => (int)Process.GetProcessById((int)i.GetProcessID).MainWindowHandle).ToList();

                bool dup = identSessions.Count > 1;

                var process = FindLivingProcess(identSessions);
                string label = (process != null) ? process.ProcessName : ident;

                if (HasSystemSoundsSession(identSessions))
                    label = "System Sounds";
                                
                var mixerSession = new MixerSession(this, label, ident, identSessions, SessionType.Application);
                mixerSessions.Add(mixerSession);
            }

            return mixerSessions;
        }
                
        public MMDevice GetDeviceByIdentifier(String identifier)
        {
            UpdateDevices();

            String deviceId = (identifier==null?"":identifier);
            if (deviceId.IndexOf("|")>-1) { //work with session identifier also
                deviceId = deviceId.Substring(0, deviceId.IndexOf("|"));
            };

            for (int i = 0; i < outputDevices.Count; i++)
            {
                if (outputDevices[i].ID.Equals(deviceId))
                    return outputDevices[i];
            }
            for (int i = 0; i < inputDevices.Count; i++)
            {
                if (inputDevices[i].ID.Equals(deviceId))
                    return inputDevices[i];
            }
            //return default if none found (config/save retro-compatibility)
            var deviceEnumerator = new MMDeviceEnumerator();
            return deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        public Process FindLivingProcess(List<AudioSessionControl> sessions)
        {
            Process process = null;
            foreach (var session in sessions)
            {
                try
                {
                    process = Process.GetProcessById((int)session.GetProcessID);
                    return process;
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine($@"Failed to find process {session.GetProcessID}");
                }
            }
            return process;
        }

        public bool HasSystemSoundsSession(List<AudioSessionControl> sessions)
        {
            foreach (var session in sessions)
                if (session.IsSystemSoundsSession)
                    return true;

            return false;
        }

        public MixerSession FindMixerSession(string sessionIdentifier)
        {
            var mixerSessions = GetCachedMixerSessions();

            foreach (var mixerSession in mixerSessions)
            {
                foreach (var session in mixerSession.audioSessions)
                {
                    if (session.GetSessionIdentifier.Contains(sessionIdentifier)) 
                        return mixerSession;                  
                }
            }

            return null;
        }

        private struct ProcessCache
        {
            public Process process;
            public int id;

            public ProcessCache(Process process, int id)
            {
                this.process = process;
                this.id = id;
            }
        }
        private ProcessCache processCache = new ProcessCache(null, -1);
              
        public MixerSession FindMixerSession(int pid)
        {
            var mixerSessions = GetCachedMixerSessions();

            foreach (var mixerSession in mixerSessions)
                foreach (var session in mixerSession.audioSessions)
                    if (session.GetProcessID == pid)
                        return mixerSession;

            // if mixer session was not found becuase the pid does not match the pid of any audio session try finding it by process name
            // this is necessary since chrome and some other applications have some weired process structure
            if (processCache.id != pid)
            {
                processCache = new ProcessCache(Process.GetProcessById(pid), pid);
            }
            Process process = processCache.process;

            foreach (var mixerSession in mixerSessions)
            {
                if (mixerSession.label.Equals(process.ProcessName))
                {
                    return mixerSession;
                }
            }

            return null;
        }
    }
}
