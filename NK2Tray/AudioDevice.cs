﻿using NAudio.CoreAudioApi;
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
        public MidiDevice midiDevice;
        private MMDevice device;
        private AudioEndpointVolume deviceVolume;

        private void UpdateDevice()
        {
            // Add audio devices to each fader menu items
            var deviceEnumerator = new MMDeviceEnumerator();

            device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            deviceVolume = device.AudioEndpointVolume;
        }

        public AudioEndpointVolume GetDeviceVolumeObject()
        {
            // Used to handle "COM object that has been separated from its underlying RCW cannot be used" issue.
            UpdateDevice();
            return deviceVolume;
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

        public List<MixerSession> GetMixerSessions()
        {
            var mixerSessions = new List<MixerSession>();
            var sessionsByIdent = new Dictionary<String, List<AudioSessionControl>>();

            UpdateDevice();
            var sessions = device.AudioSessionManager.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (session.State != AudioSessionState.AudioSessionStateExpired)
                {
                    if (!sessionsByIdent.ContainsKey(session.GetSessionIdentifier))
                        sessionsByIdent[session.GetSessionIdentifier] = new List<AudioSessionControl>();

                    sessionsByIdent[session.GetSessionIdentifier].Add(session);
                }
            }

            foreach (var ident in sessionsByIdent.Keys.ToList())
            {
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

                var mixerSession = new MixerSession(this, label, ident, ordered, sessionType);
                mixerSessions.Add(mixerSession);
            }

            return mixerSessions;
        }

        public MixerSession FindMixerSessions(string sessionIdentifier)
        {
            var mixerSessions = GetMixerSessions();
            foreach (var mixerSession in mixerSessions)
            {
                foreach (var session in mixerSession.audioSessions)
                {
                    if (session.GetSessionIdentifier == sessionIdentifier)
                        return mixerSession;
                }
            }
            return null;
        }
        
        public MixerSession FindMixerSessions(int pid)
        {
            var mixerSessions = GetMixerSessions();
            foreach (var mixerSession in mixerSessions)
                foreach (var session in mixerSession.audioSessions)
                    if (session.GetProcessID == pid)
                        return mixerSession;

            return null;
        }
    }
}