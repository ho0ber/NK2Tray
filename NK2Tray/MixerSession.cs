using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{
    public enum SessionType
    {
        Application,
        SystemSounds,
        Master
    }

    public class MixerSession
    {
        public string label;
        public string sessionIdentifier;
        public List<AudioSessionControl> audioSessions;
        public SessionType sessionType;
        public AudioEndpointVolume deviceVolume;

        public MixerSession(string labl, string identifier, List<AudioSessionControl> sessions, SessionType sesType)
        {
            label = labl;
            sessionIdentifier = identifier;
            audioSessions = sessions;
            sessionType = sesType;
        }

        public MixerSession(string labl, SessionType sesType, AudioEndpointVolume devVol)
        {
            label = labl;
            sessionIdentifier = "";
            audioSessions = new List<AudioSessionControl>();
            sessionType = sesType;
            deviceVolume = devVol;
        }

        public void SetVolume(float volume)
        {
            if (sessionType == SessionType.Application)
            {
                foreach (var session in audioSessions)
                {
                    session.SimpleAudioVolume.Volume = volume;
                }
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
                    deviceVolume = AudioDevice.GetDeviceVolumeObject();
                    deviceVolume.MasterVolumeLevelScalar = volume;
                    Console.WriteLine($@"RETRY: Setting master volume to {volume}");
                }
            }
        }
    }


}
