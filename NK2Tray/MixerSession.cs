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

    /// <summary>
    /// A <c>MixerSession</c> represents a running application and all audio sessions belonging to it.
    /// </summary>
    public class MixerSession
    {
        public string label;
        public string sessionIdentifier;
        public List<AudioSessionControl> audioSessions;
        public SessionType sessionType;
        public AudioDevice devices;
        public String parentDeviceIdentifier;

        public MixerSession(AudioDevice devices, string labl, string identifier, List<AudioSessionControl> sessions, SessionType sesType)
        {
            this.devices = devices;
   
            sessionIdentifier = identifier;
            audioSessions = sessions;
            sessionType = sesType;
            label = labl;
        }

        public MixerSession(String deviceIdentifier, AudioDevice devices, string labl, SessionType sesType)
        {
            this.devices = devices;
            
            sessionIdentifier = "";
            audioSessions = new List<AudioSessionControl>();
            sessionType = sesType;
            parentDeviceIdentifier = deviceIdentifier;

            if (sesType == SessionType.Focus)
                label = "Focus";
            else
                label = devices.GetDeviceByIdentifier(deviceIdentifier).FriendlyName + ": " + labl;
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
                    devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.MasterVolumeLevelScalar = volume;                                   
                }
                catch (System.Runtime.InteropServices.InvalidComObjectException e)
                {
                    // This catch handles the "COM object that has been separated from its underlying RCW cannot be used" issue.
                    // I believe this happens when we refresh the device when opening the menu, but this is fine for now.
                    Console.WriteLine($@"Error when setting master volume: {e.Message}");
                    AudioEndpointVolume deviceVolume = devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume;
                    deviceVolume.MasterVolumeLevelScalar = volume;
                    Console.WriteLine($@"RETRY: Setting master volume to {volume}");
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    //TODO find out where this exception comes from and actually fix it
                    Console.WriteLine("COM Execption" + e);
                }
                catch (System.InvalidCastException e)
                {                    
                    Console.WriteLine("InvalidCastException Exeception" + e);
                }
            } 
            else if (sessionType == SessionType.Focus)
            {
                var pid = WindowTools.GetForegroundPID();
                var mixerSession = devices.FindMixerSession(pid);
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
                    var curVol = devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.MasterVolumeLevelScalar;
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
                    var curVol = devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.MasterVolumeLevelScalar;
                    if (curVol < 0)
                        curVol = 0;
                    if (curVol > 1)
                        curVol = 1;
                    return curVol;
                }
            }
            else if (sessionType == SessionType.Focus)
            {
                var pid = WindowTools.GetForegroundPID();
                var mixerSession = devices.FindMixerSession(pid);
                if (mixerSession != null)
                {
                    foreach (var session in mixerSession.audioSessions)
                    {
                        var curVol = session.SimpleAudioVolume.Volume;
                        if (curVol < 0)
                            curVol = 0;
                        if (curVol > 1)
                            curVol = 1;
                        return curVol;
                    }
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
                    var curVol = devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.MasterVolumeLevelScalar;
                    curVol += change;
                    if (curVol < 0)
                        curVol = 0;
                    if (curVol > 1)
                        curVol = 1;
                    devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.MasterVolumeLevelScalar = curVol;
                    retVol = curVol;
                }
                catch (System.Runtime.InteropServices.InvalidComObjectException e)
                {
                    // This catch handles the "COM object that has been separated from its underlying RCW cannot be used" issue.
                    // I believe this happens when we refresh the device when opening the menu, but this is fine for now.
                    Console.WriteLine($@"Error when setting master volume: {e.Message}");
                    AudioEndpointVolume deviceVolume = devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume;
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
                var mixerSession = devices.FindMixerSession(pid);
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
                    var muted = !devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.Mute;
                    devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.Mute = muted;
                    return muted;
                }
                catch (System.Runtime.InteropServices.InvalidComObjectException e)
                {
                    // This catch handles the "COM object that has been separated from its underlying RCW cannot be used" issue.
                    // I believe this happens when we refresh the device when opening the menu, but this is fine for now.
                    Console.WriteLine($@"Error when toggling mute on master volume: {e.Message}");
                    AudioEndpointVolume deviceVolume = devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume;
                    var muted = !deviceVolume.Mute;
                    deviceVolume.Mute = muted;

                    Console.WriteLine("RETRY: toggling mute on master volume");
                    return muted;
                }
            }
            else if (sessionType == SessionType.Focus)
            {
                var pid = WindowTools.GetForegroundPID();
                var mixerSession = devices.FindMixerSession(pid);
                if (mixerSession != null)
                {
                    var muted = !mixerSession.audioSessions.First().SimpleAudioVolume.Mute;
                    foreach (var session in mixerSession.audioSessions)
                        session.SimpleAudioVolume.Mute = muted;
                    return muted;
                }
            }
            return false;
        }

        public bool GetMute()
        {
            if (sessionType == SessionType.Master)
                return devices.GetDeviceByIdentifier(parentDeviceIdentifier).AudioEndpointVolume.Mute;

            var targetAudioSessions = audioSessions;

            if (sessionType == SessionType.Focus)
            {
                var pid = WindowTools.GetForegroundPID();
                var mixerSession = devices.FindMixerSession(pid);
                if (mixerSession == null) return false;
                targetAudioSessions = mixerSession.audioSessions;
            }

            return targetAudioSessions.First().SimpleAudioVolume.Mute;
        }

        public bool HasCrossoverProcesses(MixerSession other)
        {
            if (other == null) return false;

            return audioSessions.Any(session =>
                other.audioSessions.Any(otherSession =>
                    session.GetProcessID == otherSession.GetProcessID
                )
            );
        }
    }
}
