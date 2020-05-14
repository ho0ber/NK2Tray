using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static NAudio.CoreAudioApi.AudioSessionManager;

namespace NK2Tray
{
    public class Assignment : IDisposable
    {
        private bool disposed = false;
        private readonly MMDevice device;
        private readonly string sessionId;
        private readonly AudioDeviceWatcher audioDeviceWatcher;

        public Fader fader;
        public string uid; // A unique ID that can be used for this device/application
        public string Label;

        public Assignment (AudioDeviceWatcher audioDeviceWatcher, MMDevice device)
        {
            this.audioDeviceWatcher = audioDeviceWatcher;
            this.device = device;
            this.Label = audioDeviceWatcher.QuickDeviceNames[device];
            this.uid = this.audioDeviceWatcher.QuickDeviceIds[this.device];
            this.SetupListeners();
        }

        public Assignment (AudioDeviceWatcher audioDeviceWatcher, string sessionId)
        {
            this.audioDeviceWatcher = audioDeviceWatcher;
            this.sessionId = sessionId;
            this.Label = audioDeviceWatcher.Sessions.ContainsKey(sessionId)
                ? audioDeviceWatcher.Sessions[sessionId].First().DisplayName
                : Assignment.GetInactiveSessionLabel(this.sessionId);
            this.uid = sessionId;
            this.SetupListeners();
        }

        public Assignment (AudioDeviceWatcher audioDeviceWatcher)
        {
            this.audioDeviceWatcher = audioDeviceWatcher;
            this.Label = "Focus";
            this.uid = "__FOCUS__";
            this.SetupListeners();
        }

        private void SetupListeners ()
        {
            if (this.device != null) this.audioDeviceWatcher.OnDeviceVolumeChange += OnDeviceVolumeChange;
            if (this.sessionId != null || this.uid == "__FOCUS__") this.audioDeviceWatcher.OnSessionVolumeChange += OnSessionVolumeChange;
        }

        public void LaunchApplication ()
        {
            if (String.IsNullOrEmpty(this.sessionId)) return;
            if (this.sessionId.StartsWith("#")) return;

            int deviceIndex = this.sessionId.IndexOf("\\Device");
            int endIndex = this.sessionId.IndexOf("%b{");
            var devicePath = this.sessionId.Substring(deviceIndex, endIndex - deviceIndex);
            var appPath = DevicePathMapper.FromDevicePath(devicePath);

            if (!String.IsNullOrEmpty(appPath))
            {
                if (WindowTools.IsProcessByNameRunning(this.Label)) return;
                WindowTools.StartApplication(appPath);
            }
        }

        private static string GetInactiveSessionLabel (string sessionId)
        {
            if (String.IsNullOrEmpty(sessionId) || !sessionId.Contains(".exe")) return "";

            int lastBackSlash = sessionId.LastIndexOf('\\') + 1;
            int programNameLength = sessionId.IndexOf(".exe") - lastBackSlash;

            return sessionId.Substring(lastBackSlash, programNameLength);
        }

        private void OnDeviceVolumeChange (object sender, DeviceVolumeChangedEventArgs e)
        {
            if (fader == null) return;
            if (device != e.device) return;

            fader.SetVolumeIndicator(e.volume);
            fader.SetMuteLight(e.isMuted);
        }

        private void OnSessionVolumeChange (object sender, SessionVolumeChangedEventArgs e)
        {
            if (fader == null) return;

            if (!String.IsNullOrEmpty(this.sessionId))
            {
                if (this.sessionId != e.sessionId) return;
            }
            else
            {
                var sessionId = audioDeviceWatcher.GetForegroundSessionId();
                if (sessionId != e.sessionId) return;
            }

            fader.SetVolumeIndicator(e.volume);
            fader.SetMuteLight(e.isMuted);
        }

        public virtual float GetVolume()
        {
            if (device != null)
                return device.AudioEndpointVolume.MasterVolumeLevelScalar;

            if (sessionId != null)
                return audioDeviceWatcher.Sessions.ContainsKey(sessionId)
                    ? audioDeviceWatcher.Sessions[sessionId].First().SimpleAudioVolume.Volume
                    : 0;

            if (this.uid == "__FOCUS__")
            {
                var sessionId = this.audioDeviceWatcher.GetForegroundSessionId();

                if (!String.IsNullOrEmpty(sessionId))
                    return audioDeviceWatcher.Sessions.ContainsKey(sessionId)
                        ? audioDeviceWatcher.Sessions[sessionId].First().SimpleAudioVolume.Volume
                        : 0;
            }

            return 0;
        }

        public virtual float SetVolume(float volume)
        {
            var targetVol = Math.Min(1, Math.Max(0, volume));

            if (device != null)
            {
                device.AudioEndpointVolume.MasterVolumeLevelScalar = targetVol;
            }
            else if (sessionId != null)
            {
                if (audioDeviceWatcher.Sessions.ContainsKey(sessionId))
                    audioDeviceWatcher.Sessions[sessionId].ForEach(session =>
                        session.SimpleAudioVolume.Volume = targetVol
                    );
            }
            else if (this.uid == "__FOCUS__")
            {
                var sessionId = this.audioDeviceWatcher.GetForegroundSessionId();

                if (!String.IsNullOrEmpty(sessionId))
                    if (audioDeviceWatcher.Sessions.ContainsKey(sessionId))
                        audioDeviceWatcher.Sessions[sessionId].ForEach(session =>
                            session.SimpleAudioVolume.Volume = targetVol
                        );
            }

            return targetVol;
        }
        public virtual float ChangeVolume(float change)
        {
            var curVol = GetVolume();
            var targetVol = curVol + change;

            return SetVolume(targetVol);
        }
        public virtual bool GetMute()
        {
            if (device != null)
                return device.AudioEndpointVolume.Mute;

            if (sessionId != null)
                return audioDeviceWatcher.Sessions.ContainsKey(sessionId)
                    ? audioDeviceWatcher.Sessions[sessionId].First().SimpleAudioVolume.Mute
                    : false;

            if (this.uid == "__FOCUS__")
            {
                var sessionId = this.audioDeviceWatcher.GetForegroundSessionId();

                if (!String.IsNullOrEmpty(sessionId))
                    return audioDeviceWatcher.Sessions.ContainsKey(sessionId)
                        ? audioDeviceWatcher.Sessions[sessionId].First().SimpleAudioVolume.Mute
                        : false;
            }

            return false;
        }
        public virtual bool ToggleMute()
        {
            var targetStatus = !GetMute();

            return SetMute(targetStatus);
        }

        // Returns whether or not the mute status was successfully set.
        // If it failed, it's probably because the session didn't exist.
        public virtual bool SetMute(bool muted)
        {
            if (device != null)
            {
                device.AudioEndpointVolume.Mute = muted;

                return true;
            }

            if (sessionId != null)
            {
                if (!audioDeviceWatcher.Sessions.ContainsKey(sessionId)) return false;

                audioDeviceWatcher.Sessions[sessionId].ForEach(session =>
                    session.SimpleAudioVolume.Mute = muted
                );

                return true;
            }

            if (this.uid == "__FOCUS__")
            {
                var sessionId = audioDeviceWatcher.GetForegroundSessionId();

                if (!String.IsNullOrEmpty(sessionId))
                {
                    if (!audioDeviceWatcher.Sessions.ContainsKey(sessionId)) return false;

                    audioDeviceWatcher.Sessions[sessionId]?.ForEach(session =>
                        session.SimpleAudioVolume.Mute = muted
                    );

                    return true;
                }
            }

            return false;
        }

        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                if (this.device != null) this.audioDeviceWatcher.OnDeviceVolumeChange -= OnDeviceVolumeChange;
                if (this.sessionId != null || this.uid == "__FOCUS__") this.audioDeviceWatcher.OnSessionVolumeChange -= OnSessionVolumeChange;
            }

            disposed = true;
        }
    }
}
