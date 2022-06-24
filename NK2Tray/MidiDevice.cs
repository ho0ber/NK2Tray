using NAudio.CoreAudioApi;
using NAudio.Midi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;

namespace NK2Tray
{
    public enum ControllerType
    {
        NanoKontrol2,
        OP1,
        XtouchMini,
        EasyControl
    }
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
        //public List<Button> buttons;
        //public Hashtable buttonsMappingTable;

        public AudioDevice audioDevices;

        public MidiDeviceTemplate deviceTemplate;

        //public virtual string SearchString => "wobbo";

        /*public virtual FaderDef DefaultFaderDef =>
            new FaderDef(
                false,
                1f,
                1,
                true,
                true,
                true,
                true,
                0,
                0,
                0,
                0,
                0,
                MidiCommandCode.ControlChange,
                MidiCommandCode.ControlChange,
                MidiCommandCode.ControlChange,
                MidiCommandCode.ControlChange,
                MidiCommandCode.ControlChange);*/

        private bool faderSetup = false;

        public float curve = 1f;

        public MidiDevice(AudioDevice audioDev, ControllerType templateType, string midiInPort, string midiOutPort)
        {
            Console.WriteLine($@"Initializing Midi Device {midiOutPort}");
            audioDevices = audioDev;

            FindMidiIn(midiInPort);
            FindMidiOut(midiOutPort);

            if (Found)
            {
                switch (templateType)
                {
                    case ControllerType.NanoKontrol2:
                        deviceTemplate = new NanoKontrol2(this);
                        break;
                    case ControllerType.OP1:
                        deviceTemplate = new OP1(this);
                        break;
                    case ControllerType.XtouchMini:
                        deviceTemplate = new XtouchMini(this);
                        break;
                    case ControllerType.EasyControl:
                        deviceTemplate = new EasyControl(this);
                        break;
                    default:
                        //Oops
                        return;
                }

                if (!faderSetup)
                {
                    InitFaders();
                    faderSetup = true;
                }

                if (deviceTemplate.hasLights) ResetAllLights();
                ResetFaders();
                LightShow();
                LoadAssignments();
                ListenForMidi();
            }
        }

        public void Close()
        {
            if (midiOut != null)
            {
                midiOut.Close();
            }
            if (midiIn != null)
            {
                midiIn.Close(); //Doesn't work for some reason
            }
        }

        public bool Found => (midiIn != null && midiOut != null);

        public void FindMidiIn(string name)
        {
            if(midiIn != null)
            {
                midiIn.Close(); //Doesn't work for some reason
            }
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                Console.WriteLine("MIDI IN: " + MidiIn.DeviceInfo(i).ProductName);
                //if (MidiIn.DeviceInfo(i).ProductName.ToLower().Contains(name))
                if (MidiIn.DeviceInfo(i).ProductName.Equals(name))
                {
                    midiIn = new MidiIn(i);
                    Console.WriteLine($@"Assigning MidiIn: {MidiIn.DeviceInfo(i).ProductName}");
                    break;
                }
            }
        }

        public void FindMidiOut(string name)
        {
            if (midiOut != null)
            {
                midiOut.Close();
            }
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                Console.WriteLine("MIDI OUT: " + MidiOut.DeviceInfo(i).ProductName);
                //if (MidiOut.DeviceInfo(i).ProductName.ToLower().Contains(name))
                if (MidiOut.DeviceInfo(i).ProductName.Equals(name))
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
            //WindowTools.Dump(e.MidiEvent);

            foreach (var fader in faders)
            {
                fader.HandleEvent(e);
                fader.SetHandling(false);
            }

            //ControlChangeEvent midiController = null;
            //
            //try
            //{
            //    midiController = (ControlChangeEvent)e.MidiEvent;
            //}
            //catch (System.InvalidCastException exc)
            //{
            //    return;
            //}
            //
            //if (midiController == null)
            //    return;
            ////key UP...!
            //if (midiController.ControllerValue == 0)
            //    return;
            //
            //var obj = buttonsMappingTable[(int)midiController.Controller];
            //if (obj != null)
            //{
            //    Button button = (Button)obj;
            //    button.HandleEvent(e, this);
            //    button.SetHandling(false);
            //}
            //else
            {
                foreach (var button in deviceTemplate.buttons)
                {
                    button.HandleEvent(e, this);
                    button.SetHandling(false);
                }
            }
        }

        public void ResetAllLights() {
            for (int i = 0; i < deviceTemplate.lightCount; i++)
            {
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());
            }
            deviceTemplate.ResetSuppLights(midiOut);
        }

        public void LightShow() {
            if (deviceTemplate.hasLights) deviceTemplate.LightShow(ref faders);
        }

        public void SetVolumeIndicator(int faderNum, float level) {
            if (deviceTemplate.hasVolumeIndicator)
            {
                if (level >= 0)
                {
                    var usedLevel = level;
                    var fader = faders[faderNum];

                    if (fader.faderDef.delta)
                    {
                        var nearestStep = fader.steps.Select((x, i) => new { Index = i, Distance = Math.Abs(level - x) }).OrderBy(x => x.Distance).First().Index;
                        usedLevel = (float)nearestStep / (fader.steps.Length - 1);
                    }

                    var val = (int)Math.Round(usedLevel * 12) + 1 + 16 * 2;

                    Console.WriteLine($@"Setting fader {fader} display to {usedLevel} ({val})");
                    midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(faderNum + 48), val).GetAsShortMessage());
                }
                else
                    midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(faderNum + 48), 0).GetAsShortMessage());
            }
        }

        public virtual void SetLight(int controller, bool state) {
            if (deviceTemplate.hasLights)
            {
                if (deviceTemplate.lightMessageType == MidiCommandCode.NoteOn)
                {
                    midiOut.Send(new NoteOnEvent(0, 1, controller, state ? 127 : 0, 0).GetAsShortMessage());
                }
                else
                {
                    midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(controller), state ? 127 : 0).GetAsShortMessage());
                }
            }
        }

        public void InitFaders()
        {
            faders = new List<Fader>();

            for (int i = 0; i < deviceTemplate.fadersCount; i++)
            {
                Fader fader = new Fader(this, i, deviceTemplate.faderDefs[deviceTemplate.SelectFaderDef(i)]);
                faders.Add(fader);
            }
        }

        public void ResetFaders()
        {
            if (deviceTemplate.hasLights)
            {
                foreach(Fader fader in faders)
                {
                    fader.ResetLights();
                    fader.SetCurve(curve);
                }
            }
        }

        public void SetCurve(float pow)
        {
            curve = pow;
            if(faderSetup) faders.ForEach(fader => fader.SetCurve(pow));
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
                    if (ident.Equals("__FOCUS__"))
                    {
                        foundAssignments = true;
                        fader.Assign(new MixerSession("", audioDevices, "Focus", SessionType.Focus));
                    }
                    else if (ident.Equals("__MASTER__") || (ident.Substring(0, Math.Min(10, ident.Length)).Equals("__MASTER__")))
                    {
                        foundAssignments = true;
                        MMDevice mmDevice = audioDevices.GetDeviceByIdentifier(ident.IndexOf("|") >= 0 ? ident.Substring(ident.IndexOf("|")+1) : "");
                        fader.Assign(new MixerSession(mmDevice.ID, audioDevices, "Master", SessionType.Master));
                    }                    
                    else if (ident.Length > 0)
                    {
                        foundAssignments = true;
                        var matchingSession = audioDevices.FindMixerSession(ident);
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

                var savedSubFaderPosition = ConfigSaver.GetAppSettings(fader.faderNumber.ToString() + "m");
                fader.faderPositionMultiplier = savedSubFaderPosition != null ? float.Parse(savedSubFaderPosition) : 1;
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
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "__FOCUS__");
                    else
                        ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), fader.assignment.sessionIdentifier);
                }
                else
                {
                    ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString(), "");
                }

                ConfigSaver.AddOrUpdateAppSettings(fader.faderNumber.ToString() + "m", fader.faderPositionMultiplier.ToString());
            }
        }

    }
}
