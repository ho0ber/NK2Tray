using NAudio.Midi;
using System;
using System.Collections.Generic;
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

        public string searchString = "";

        public MidiDevice(string search)
        {
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
            {
                fader.HandleEvent(e);
            }

            foreach (var button in buttons)
            {
                button.HandleEvent(e);
            }
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
                Fader fader = new Fader(ref midiOut, i, 0, 32, 48, 64);
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
            faders.Last().Assign(new MixerSession("Master", SessionType.Master, AudioDevice.GetDeviceVolumeObject()));
        }
        /*
        public void SaveAssignments()
        {
            Console.WriteLine("Saving assignments");

            foreach (var fader in faders)
            {
                if (fader.assignment.assigned)
                {
                    if (fader.assignment.aType == AssignmentType.Master)
                        AddOrUpdateAppSettings(fader.faderNumber.ToString(), "__MASTER__;0");
                    else
                        AddOrUpdateAppSettings(fader.faderNumber.ToString(), String.Format("{0};{1}", fader.assignment.sessionIdentifier, fader.assignment.instanceNumber));
                }
                else
                    AddOrUpdateAppSettings(fader.faderNumber.ToString(), "");
            }
        }

        public void LoadAssignments()
        {
            Console.WriteLine("Loading assignments: " + GetAppSettings("assignments"));
            UpdateDevice();

            foreach (var fader in faders)
            {
                //assignments.Add(new Assignment());
                Console.WriteLine("Getting setting: " + i.ToString());
                var identAndInstance = GetAppSettings(i.ToString()).Split(';');
                int instance = 0;
                var ident = identAndInstance[0];
                if (identAndInstance.Count() > 1)
                    instance = int.Parse(identAndInstance[1]);

                Console.WriteLine("Got setting: " + ident);
                if (ident != null)
                {
                    if (ident == "__MASTER__")
                    {
                        Console.WriteLine(String.Format("Fader {0} is {1} (master)", i, ident));
                        assignments[i] = new Assignment("Master Volume", "", -1, AssignmentType.Master, "", "", null);
                        NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, i, true));
                        NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, i, false));
                        Console.WriteLine("Assigned!");
                    }
                    else if (ident.Length > 0)
                    {
                        Console.WriteLine(String.Format("Fader {0} is {1} (process)", i, ident));
                        var matchingSession = FindSession(ident, instance);
                        if (matchingSession != null)
                        {
                            assignments[i] = new Assignment(matchingSession, i);
                            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, i, true));
                            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, i, false));
                            Console.WriteLine("Assigned!");
                        }
                        else
                        {
                            assignments[i] = new Assignment(ident, i);
                            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, i, true));
                            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, i, true));
                            Console.WriteLine("Assigned!");
                        }
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Fader {0} is {1} (nothing)", i, ident));
                        assignments[i] = new Assignment();
                        Console.WriteLine("Assigned!");
                    }
                }
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

        /*
        private void InitAssignments()
        {
            assignments[7] = new Assignment("Master Volume", "", -1, AssignmentType.Master, "", "", null);
            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, 7, true));
            SaveAssignments();
        }*/
    }
}
