using NAudio.CoreAudioApi;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public AudioDevice audioDevices;

        public virtual string SearchString => "wobbo";

        public virtual FaderDef DefaultFaderDef => new FaderDef(false, 1f, 1, true, true, true, 0, 0, 0, 0, MidiCommandCode.ControlChange, MidiCommandCode.ControlChange, MidiCommandCode.ControlChange, MidiCommandCode.ControlChange);

        public MidiDevice()
        {
            Console.WriteLine($@"Initializing Midi Device {SearchString}");
        }

        public bool Found => (midiIn != null && midiOut != null);

        public void FindMidiIn()
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                Console.WriteLine("MIDI IN: " + MidiIn.DeviceInfo(i).ProductName);
                if (MidiIn.DeviceInfo(i).ProductName.ToLower().Contains(SearchString))
                {
                    midiIn = new MidiIn(i);
                    Console.WriteLine($@"Assigning MidiIn: {MidiIn.DeviceInfo(i).ProductName}");
                    break;
                }
            }
        }

        public void FindMidiOut()
        {
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                Console.WriteLine("MIDI OUT: " + MidiOut.DeviceInfo(i).ProductName);
                if (MidiOut.DeviceInfo(i).ProductName.ToLower().Contains(SearchString))
                {
                    midiOut = new MidiOut(i);
                    Console.WriteLine($@"Assigning MidiOut: {MidiOut.DeviceInfo(i).ProductName}");
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

        public virtual void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            WindowTools.Dump(e.MidiEvent);

            foreach (var fader in faders)
                fader.HandleEvent(e);

            foreach (var button in buttons)
                button.HandleEvent(e);
        }

        public virtual void ResetAllLights() { }

        public virtual void SetVolumeIndicator(int fader, float level) { }

        public virtual void SetLight(int controller, bool state) { }

        public virtual void InitFaders()
        {
            faders = new List<Fader>();
        }

        public virtual void InitButtons()
        {
            buttons = new List<Button>();
        }

        public void LoadAssignments()
        {
            bool foundAssignments = false;

            foreach (var fader in faders)
            {
                Console.WriteLine("Getting setting: " + fader.faderNumber.ToString());
                var ident = ConfigSaver.GetAppSettings(fader.faderNumber.ToString());

                Console.WriteLine("Got setting: " + ident);
                if (ident != null)
                {
                    if (ident.Equals("__FOCUS__") || (ident.Length>=9 && ident.Substring(0, 9).Equals("__FOCUS__")))
                    {
                        foundAssignments = true;
                        MMDevice mmDevice = audioDevices.GetDeviceByIdentifier(ident.IndexOf("|") >= 0 ? ident.Substring(ident.IndexOf("|")+1) : "");
                        fader.Assign(new MixerSession(mmDevice.ID, audioDevices, "Focus", SessionType.Focus));
                    }
                    if (ident.Equals("__MASTER__") || (ident.Length>=10 && ident.Substring(0, 10).Equals("__MASTER__")))
                    {
                        foundAssignments = true;
                        MMDevice mmDevice = audioDevices.GetDeviceByIdentifier(ident.IndexOf("|") >= 0 ? ident.Substring(ident.IndexOf("|")+1) : "");
                        fader.Assign(new MixerSession(mmDevice.ID, audioDevices, "Master", SessionType.Master));
                    }                    
                    else if (ident.Length > 0)
                    {
                        foundAssignments = true;
                        var matchingSession = audioDevices.FindMixerSessions(ident);
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

            
            
            // Load fader 8 as master volume control as default if no faders are set
            if (!foundAssignments)
            {
                if (faders.Count > 0)
                {
                    faders.Last().Assign(new MixerSession(audioDevices.GetDeviceByIdentifier("").ID, audioDevices, "Master", SessionType.Master));
                    SaveAssignments();
                }
            }
            
        }

        public void SaveAssignments()
        {
            Console.WriteLine("Saving Assignments");
            foreach (var fader in faders)
            {
                if (fader.assigned)
                {
                    if (fader.assignment.sessionType == SessionType.Master)
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "__MASTER__|" + fader.assignment.parentDeviceIdentifier );
                    else if (fader.assignment.sessionType == SessionType.Focus)
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "__FOCUS__|" + fader.assignment.parentDeviceIdentifier);
                    else
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), fader.assignment.sessionIdentifier);
                }
                else
                    ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "");
            }
        }

    }
}
