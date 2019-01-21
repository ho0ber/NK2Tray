using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{
    public class Fader
    {
        public MidiCommandCode commandCode;
        public int channel;
        public int faderNumber;
        public int inputController;
        public int selectController;
        public int muteController;
        public int recordController;
        public MixerSession assignment;
        public bool assigned;
        public MidiOut midiOut;
        public MidiDevice parent;
        public string identifier;

        public Fader(MidiDevice midiDevice, int faderNum, int inputOffst, int selectOffset, int muteOffset, int recordOffset)
        {
            parent = midiDevice;
            midiOut = midiDevice.midiOut;
            commandCode = MidiCommandCode.ControlChange;
            channel = 1;
            faderNumber = faderNum;
            inputController = faderNum + inputOffst;
            selectController = faderNum + selectOffset;
            muteController = faderNum + muteOffset;
            recordController = faderNum + recordOffset;
        }

        public void ResetLights()
        {
            SetSelectLight(false);
            SetMuteLight(false);
            SetRecordLight(false);
        }

        public void Assign(MixerSession mixerSession)
        {
            assigned = true;
            assignment = mixerSession;
            identifier = mixerSession.sessionIdentifier;
            SetSelectLight(true);
            SetRecordLight(false);
        }

        public void AssignInactive(string ident)
        {
            identifier = ident;
            assigned = false;
            SetSelectLight(true);
            SetRecordLight(true);
        }

        public void Unassign()
        {
            assigned = false;
            assignment = null;
            SetSelectLight(false);
            identifier = "";
        }

        public void SetSelectLight(bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(selectController), state ? 127 : 0).GetAsShortMessage());
        }

        public void SetMuteLight(bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(muteController), state ? 127 : 0).GetAsShortMessage());
        }

        public void SetRecordLight( bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(recordController), state ? 127 : 0).GetAsShortMessage());
        }

        public bool HandleEvent(MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode != commandCode)
                return false;

            ControlChangeEvent me = (ControlChangeEvent)e.MidiEvent;

            if (me.Channel != channel)
                return false;

            int controller = (int)me.Controller;

            if (controller == inputController)
            {
                if (assigned)
                {
                    assignment.SetVolume(me.ControllerValue / 127f);
                    if (assignment.IsDead())
                        SetRecordLight(true);
                }
                return true;
            }
            else if (controller == selectController)
            {
                if (me.ControllerValue != 127) // Only on button-down
                    return true;

                Console.WriteLine($@"Attempting to assign current window to fader {faderNumber}");
                if (assigned)
                    Unassign();
                else
                {
                    var pid = WindowTools.GetForegroundPID();
                    var mixerSession = parent.audioDevice.FindMixerSessions(pid);
                    if (mixerSession != null)
                        Assign(mixerSession);
                    else
                        Console.WriteLine($@"MixerSession not found for pid {pid}");
                }
                return true;
            }
            else if (controller == muteController)
            {
                if (me.ControllerValue != 127) // Only on button-down
                    return true;

                SetMuteLight(assignment.ToggleMute());
                if (assignment.IsDead())
                    SetRecordLight(true);
                return true;
            }
            else if (controller == recordController)
            {
                SetRecordLight(assignment.IsDead());
                return true;
            }
            return false;
        }
    }
}