using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static NAudio.CoreAudioApi.AudioSessionManager;

namespace NK2Tray
{
    public class Assignment
    {
        private MMDevice device;
        private string sessionId;
        private AudioDeviceWatcher audioDeviceWatcher;

        public event EventHandler VolumeChanged;
        public event EventHandler MuteChanged;

        public Assignment (AudioDeviceWatcher audioDeviceWatcher, MMDevice device)
        {
            this.audioDeviceWatcher = audioDeviceWatcher;
            this.device = device;
        }

        public Assignment (AudioDeviceWatcher audioDeviceWatcher, string sessionId)
        {
            this.audioDeviceWatcher = audioDeviceWatcher;
            this.sessionId = sessionId;
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
    }
}
