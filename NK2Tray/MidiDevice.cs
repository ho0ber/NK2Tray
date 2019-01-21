using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{
    public enum SendEvent
    {
        AssignedState,
        MuteState,
        ErrorState,
        MediaPlay,
        MediaStop,
        MediaPrevious,
        MediaNext,
        MediaRecord
    }

    public class MidiDevice
    {
        public MidiIn midiIn;
        public MidiOut midiOut;

        public List<Fader> faders;
        public List<Button> buttons;

        public AudioDevice audioDevice;

        public string searchString = "";

        public MidiDevice(string search, AudioDevice audioDev)
        {
            audioDevice = audioDev;
            searchString = search;
            FindMidiIn();
            FindMidiOut();
            ResetAllLights();
            InitFaders();
            InitButtons();
            LoadAssignments();
            ListenForMidi();
        }

        public void FindMidiIn()
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (MidiIn.DeviceInfo(i).ProductName.ToLower().Contains(searchString))
                {
                    midiIn = new MidiIn(i);
                    Console.WriteLine(MidiIn.DeviceInfo(i).ProductName);
                    break;
                }
            }
        }

        public void FindMidiOut()
        {
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (MidiOut.DeviceInfo(i).ProductName.ToLower().Contains(searchString))
                {
                    midiOut = new MidiOut(i);
                    Console.WriteLine(MidiOut.DeviceInfo(i).ProductName);
                    break;
                }
            }
        }

        public void ListenForMidi()
        {
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
        }

        public void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        public void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            foreach (var fader in faders)
                fader.HandleEvent(e);

            foreach (var button in buttons)
                button.HandleEvent(e);
        }

        public void ResetAllLights()
        {
            foreach (var i in Enumerable.Range(0, 128))
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());
        }

        public void InitFaders()
        {
            faders = new List<Fader>();
            foreach (var i in Enumerable.Range(0, 8))
            {
                Fader fader = new Fader(this, i, 0, 32, 48, 64);
                fader.ResetLights();
                faders.Add(fader);
            }
        }

        public void InitButtons()
        {
            buttons = new List<Button>();
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPrevious, 43, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaNext,     44, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaStop,     42, false));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPlay,     41, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaRecord,   45, false));
        }

        public void LoadAssignments()
        {
            bool foundAssignments = false;

            foreach (var fader in faders)
            {
                Console.WriteLine("Getting setting: " + fader.faderNumber.ToString());
                var ident = GetAppSettings(fader.faderNumber.ToString());

                Console.WriteLine("Got setting: " + ident);
                if (ident != null)
                {
                    if (ident == "__MASTER__")
                    {
                        foundAssignments = true;
                        fader.Assign(new MixerSession(audioDevice, "Master", SessionType.Master, audioDevice.GetDeviceVolumeObject()));
                    }
                    else if (ident.Length > 0)
                    {
                        foundAssignments = true;
                        var matchingSession = audioDevice.FindMixerSessions(ident);
                        if (matchingSession != null)
                            fader.Assign(matchingSession);
                        else
                            fader.AssignInactive(ident);
                    }
                    else
                    {
                        fader.Unassign();
                    }
                }
            }

            if (!foundAssignments)
            {
                faders.Last().Assign(new MixerSession(audioDevice, "Master", SessionType.Master, audioDevice.GetDeviceVolumeObject()));
                SaveAssignments();
            }
        }

        public void SaveAssignments()
        {
            foreach (var fader in faders)
            {
                if (fader.assigned)
                {
                    if (fader.assignment.sessionType == SessionType.Master)
                        AddOrUpdateAppSettings(fader.faderNumber.ToString(), "__MASTER__");
                    else
                        AddOrUpdateAppSettings(fader.faderNumber.ToString(), fader.assignment.sessionIdentifier);
                }
                else
                    AddOrUpdateAppSettings(fader.faderNumber.ToString(), "");
            }
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        public static string GetAppSettings(string key)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] != null)
                    return settings[key].Value;
                else
                    return null;
            }
            catch
            {
                Console.WriteLine("Error getting app settings");
            }
            return null;
        }
    }
}
