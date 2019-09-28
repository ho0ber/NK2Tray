using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public enum SessionType
    {
        Application,
        SystemSounds,
        Master,
        Focus
    }

    public class MixerSession
    {
        public string label;
        public string sessionIdentifier;
        public List<AudioSessionControl> audioSessions;
        public SessionType sessionType;
        public AudioEndpointVolume deviceVolume;
        public AudioDevice parent;

        public MixerSession(AudioDevice audioDev, string labl, string identifier, List<AudioSessionControl> sessions, SessionType sesType)
        {
            parent = audioDev;
            label = labl;
            sessionIdentifier = identifier;
            audioSessions = sessions;
            sessionType = sesType;
        }

        public MixerSession(AudioDevice audioDev, string labl, SessionType sesType, AudioEndpointVolume devVol)
        {
            parent = audioDev;
            label = labl;
            sessionIdentifier = "";
            audioSessions = new List<AudioSessionControl>();
            sessionType = sesType;
            deviceVolume = devVol;
        }

        public bool IsDead()
        {
            bool alive = false;
            if (sessionType == SessionType.Application)
            {
                foreach (var session in audioSessions)
                    if (session.State != AudioSessionState.AudioSessionStateExpired)
                        alive = true;
            }
            else
                alive = true;

            return !alive;
        }

        public void SetVolume(float volume)
        {
            if (sessionType == SessionType.Application)
            {
                foreach (var session in audioSessions)
                    session.SimpleAudioVolume.Volume = volume;
            }
            else if (sessionType == SessionType.Master)
            {
                try
                {
                    deviceVolume.MasterVolumeLevelScalar = volume;
                }
                catch (System.Runtime.InteropServices.InvalidComObjectException e)
                {
                    // This catch handles the "COM object that has been separated from its underlying RCW cannot be used" issue.
                    // I believe this happens when we refresh the device when opening the menu, but this is fine for now.
                    Console.WriteLine($@"Error when setting master volume: {e.Message}");
                    deviceVolume = parent.GetDeviceVolumeObject();
                    deviceVolume.MasterVolumeLevelScalar = volume;
                    Console.WriteLine($@"RETRY: Setting master volume to {volume}");
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    //TODO find out where this exception comes from and actually fix it
                    Console.WriteLine("COM Execption" + e);
                }
            } else if (sessionType == SessionType.Focus)
            {
                var pid = WindowTools.GetForegroundPID();
                var mixerSession = parent.FindMixerSessions(pid);
                // Check if null since mixer session might not exist for currently focused window
                if (mixerSession != null)
                {
                    foreach (var session in mixerSession.audioSessions)
                    {
                        session.SimpleAudioVolume.Volume = volume;
                    }
                }
            }
        }

        public float GetVolume()
        {
            if (sessionType == SessionType.Application)
            {
                foreach (var session in audioSessions)
                {
                    var curVol = session.SimpleAudioVolume.Volume;
                    if (curVol < 0)
                        curVol = 0;
                    if (curVol > 1)
                        curVol = 1;
                    return curVol;
                }
            }
            else if (sessionType == SessionType.Master)
            {
                try
                {
                    var curVol = deviceVolume.MasterVolumeLevelScalar;
                    if (curVol < 0)
                        curVol = 0;
                    if (curVol > 1)
                        curVol = 1;
                    return curVol;
                }
                catch (System.Runtime.InteropServices.InvalidComObjectException e)
                {
                    // This catch handles the "COM object that has been separated from its underlying RCW cannot be used" issue.
                    // I believe this happens when we refresh the device when opening the menu, but this is fine for now.
                    Console.WriteLine($@"Error when getting master volume: {e.Message}");
                    var curVol = deviceVolume.MasterVolumeLevelScalar;
                    if (curVol < 0)
                        curVol = 0;
                    if (curVol > 1)
                        curVol = 1;
                    return curVol;
                }
            }
            return -1;
        }

        public float ChangeVolume(float change)
        {
            float retVol = -1;
            if (sessionType == SessionType.Application)
            {
                foreach (var session in audioSessions)
                {
                    if (retVol < 0)
                    {
                        var curVol = session.SimpleAudioVolume.Volume;
                        curVol += change;
                        if (curVol < 0)
                            curVol = 0;
                        if (curVol > 1)
                            curVol = 1;
                        session.SimpleAudioVolume.Volume = curVol;
                        retVol = curVol;
                    }
                    {
                        session.SimpleAudioVolume.Volume = retVol;
                    }
                }
            }
            else if (sessionType == SessionType.Master)
            {
                try
                {
                    var curVol = deviceVolume.MasterVolumeLevelScalar;
                    curVol += change;
                    if (curVol < 0)
                        curVol = 0;
                    if (curVol > 1)
                        curVol = 1;
                    deviceVolume.MasterVolumeLevelScalar = curVol;
                    retVol = curVol;
                }
                catch (System.Runtime.InteropServices.InvalidComObjectException e)
                {
                    // This catch handles the "COM object that has been separated from its underlying RCW cannot be used" issue.
                    // I believe this happens when we refresh the device when opening the menu, but this is fine for now.
                    Console.WriteLine($@"Error when setting master volume: {e.Message}");
                    deviceVolume = parent.GetDeviceVolumeObject();
                    var curVol = deviceVolume.MasterVolumeLevelScalar;
                    curVol += change;
                    if (curVol < 0)
                        curVol = 0;
                    if (curVol > 1)
                        curVol = 1;
                    deviceVolume.MasterVolumeLevelScalar = curVol;
                    Console.WriteLine($@"RETRY: Setting master volume by {change}");
                }

            }
            else if (sessionType == SessionType.Focus)
            {
                var pid = WindowTools.GetForegroundPID();
                var mixerSession = parent.FindMixerSessions(pid);
                if( mixerSession != null)
                {
                    foreach (var session in mixerSession.audioSessions)
                    {
                        var curVol = session.SimpleAudioVolume.Volume;
                        curVol += change;
                        if (curVol < 0)
                            curVol = 0;
                        if (curVol > 1)
                            curVol = 1;
                        session.SimpleAudioVolume.Volume = curVol;
                        retVol = curVol;
                    }
                }
            }
            return retVol;
        }

        public bool ToggleMute()
        {
            if (sessionType == SessionType.Application)
            {
                var muted = !audioSessions.First().SimpleAudioVolume.Mute;
                foreach (var session in audioSessions)
                    session.SimpleAudioVolume.Mute = muted;
                return muted;
            }
            else if (sessionType == SessionType.Master)
            {
                try
                {
                    var muted = !deviceVolume.Mute;
                    deviceVolume.Mute = muted;
                    return muted;
                }
                catch (System.Runtime.InteropServices.InvalidComObjectException e)
                {
                    // This catch handles the "COM object that has been separated from its underlying RCW cannot be used" issue.
                    // I believe this happens when we refresh the device when opening the menu, but this is fine for now.
                    Console.WriteLine($@"Error when toggling mute on master volume: {e.Message}");
                    deviceVolume = parent.GetDeviceVolumeObject();
                    var muted = !deviceVolume.Mute;
                    deviceVolume.Mute = muted;

                    Console.WriteLine("RETRY: toggling mute on master volume");
                    return muted;
                }
            }
            return false;
        }
    }
}
