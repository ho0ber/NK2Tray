using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{
    public class AudioDevice
    {
        private MMDevice device;
        private AudioEndpointVolume deviceVolume;

        public AudioDevice()
        {

        }

        private void UpdateDevice()
        {
            // Add audio devices to each fader menu items
            var deviceEnumerator = new MMDeviceEnumerator();

            device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            deviceVolume = device.AudioEndpointVolume;
        }

        public static AudioEndpointVolume GetDeviceVolumeObject()
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var transientDev = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return transientDev.AudioEndpointVolume;
        }

        private void OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            //LoadAssignments();
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

        public List<MixerSession> GetMixerSessions()
        {
            var mixerSessions = new List<MixerSession>();
            var sessionsByIdent = new Dictionary<String, List<AudioSessionControl>>();

            UpdateDevice();
            var sessions = device.AudioSessionManager.Sessions;

            Console.WriteLine("Getting sessions grouped by ident");
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (!sessionsByIdent.ContainsKey(session.GetSessionIdentifier))
                    sessionsByIdent[session.GetSessionIdentifier] = new List<AudioSessionControl>();
                sessionsByIdent[session.GetSessionIdentifier].Add(session);
            }
            Console.WriteLine("Done!");

            Console.WriteLine("Building MixerSession for each");
            foreach (var ident in sessionsByIdent.Keys.ToList())
            {
                Console.WriteLine("Working on " + ident);
                var ordered = sessionsByIdent[ident].OrderBy(i => (int)Process.GetProcessById((int)i.GetProcessID).MainWindowHandle).ToList();

                bool dup = ordered.Count > 1;
                string label;
                SessionType sessionType;

                if (ordered.First().IsSystemSoundsSession && WindowTools.ProcessExists(ordered.First().GetProcessID))
                {
                    label = "System Sounds";
                    sessionType = SessionType.SystemSounds;
                }
                else
                {
                    label = Process.GetProcessById((int)ordered.First().GetProcessID).ProcessName;
                    sessionType = SessionType.Application;
                }

                var mixerSession = new MixerSession(label, ident, ordered, sessionType);
                mixerSessions.Add(mixerSession);
            }

            return mixerSessions;
        }

        public AudioSessionControl FindSession(String sessionIdentifier, int instanceNumber)
        {
            var sessions = device.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                var sessionsAndMeta = SessionProcessor.GetSessionMeta(ref sessions);
                for (int i = 0; i < sessionsAndMeta.Count; i++)
                {
                    var sessionMeta = sessionsAndMeta[i];
                    if (sessionMeta.session.GetSessionIdentifier == sessionIdentifier && sessionMeta.instanceNumber == instanceNumber)
                        return sessionMeta.session;
                }
            }
            return null;
        }

        public static AudioSessionControl FindSession(int pid)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var transientDev = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessions = transientDev.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                for (int i = 0; i < sessions.Count; i++)
                {
                    if (sessions[i].GetProcessID == pid)
                        return sessions[i];
                }
            }
            return null;
        }
    }
}
