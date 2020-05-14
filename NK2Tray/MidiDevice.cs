using NAudio.CoreAudioApi;
using NAudio.Midi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
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
        public Hashtable buttonsMappingTable;

        public AudioDeviceWatcher audioDeviceWatcher;

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
            foreach (var fader in faders)
            {
                fader.HandleEvent(e);
                fader.SetHandling(false);
            }

            foreach (var button in buttons)
            {
                button.HandleEvent(e, this);
                button.SetHandling(false);
            }
        }

        public virtual void ResetAllLights() { }

        public virtual void LightShow() { }

        public virtual void SetVolumeIndicator(int fader, float level) { }

        public virtual void SetLight(int controller, bool state) {}

        public virtual void InitFaders()
        {
            faders = new List<Fader>();
        }

        public virtual void InitButtons()
        {
            buttons = new List<Button>();
        }

        public void SetCurve(float pow)
        {
            faders.ForEach(fader => fader.SetCurve(pow));
        }

        private Assignment GetAssignmentFromUid (string uid)
        {
            if (String.IsNullOrEmpty(uid)) return null;
            if (uid == "__FOCUS__") return new Assignment(audioDeviceWatcher);

            var isSession = audioDeviceWatcher.Sessions.ContainsKey(uid);
            if (isSession) return new Assignment(audioDeviceWatcher, uid);

            var device = audioDeviceWatcher.Devices.Find(d => audioDeviceWatcher.QuickDeviceIds[d] == uid);
            if (device != null) return new Assignment(audioDeviceWatcher, device);

            return null;
        }

        // AssignInactive is missing here. Not sure how this worked.
        public void LoadAssignments ()
        {
            foreach (var fader in faders)
            {
                var uid = ConfigSaver.GetAppSettings(fader.faderNumber.ToString());

                if (String.IsNullOrEmpty(uid))
                {
                    fader.Assign();
                    continue;
                }

                var assignment = GetAssignmentFromUid(uid);

                if (assignment != null)
                    fader.Assign(assignment);
                else
                    fader.Assign(uid);
            }

            // Load last fader as master volume control as default if no faders are set.
            // This isn't great to have this as a global setting.
            var hasAssignments = faders.Any(fader => fader.assigned);

            if (!hasAssignments && faders.Count > 0)
                faders.Last().Assign(new Assignment(audioDeviceWatcher, audioDeviceWatcher.DefaultDevice));
        }

        public void SaveAssignments ()
        {
            foreach (var fader in faders)
                ConfigSaver.AddOrUpdateAppSettings(
                    fader.faderNumber.ToString(),
                    fader.assigned ? fader.assignment.uid : ""
                );
        }
    }
}
