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
            this.Label = audioDeviceWatcher.Sessions[sessionId].First().DisplayName;
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
            if (this.sessionId != null) this.audioDeviceWatcher.OnSessionVolumeChange += OnSessionVolumeChange;
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
            if (sessionId != e.sessionId) return;

            fader.SetVolumeIndicator(e.volume);
            fader.SetMuteLight(e.isMuted);
        }

        public virtual float GetVolume()
        {
            if (device != null)
                return device.AudioEndpointVolume.MasterVolumeLevelScalar;

            if (sessionId != null)
                return audioDeviceWatcher.Sessions[sessionId].First().SimpleAudioVolume.Volume;

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
                audioDeviceWatcher.Sessions[sessionId].ForEach(session =>
                    session.SimpleAudioVolume.Volume = targetVol
                );
            }
            else
            {
                // focus
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
                return audioDeviceWatcher.Sessions[sessionId].First().SimpleAudioVolume.Mute;

            return false;
        }
        public virtual bool ToggleMute()
        {
            var targetStatus = !GetMute();
            SetMute(targetStatus);

            return targetStatus;
        }
        public virtual void SetMute(bool muted)
        {
            if (device != null)
            {
                device.AudioEndpointVolume.Mute = muted;

                return;
            }

            if (sessionId != null)
            {
                audioDeviceWatcher.Sessions[sessionId].ForEach(session =>
                    session.SimpleAudioVolume.Mute = muted
                );

                return;
            }

            // Focus
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
                if (this.sessionId != null) this.audioDeviceWatcher.OnSessionVolumeChange -= OnSessionVolumeChange;
            }

            disposed = true;
        }
    }
}
