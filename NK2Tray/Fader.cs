using NAudio.Midi;
using System;

namespace NK2Tray
{
    public class FaderDef
    {
        public bool delta;
        public float range;
        public int channel;
        public bool selectPresent;
        public bool mutePresent;
        public bool recordPresent;
        public int faderOffset;
        public int selectOffset;
        public int muteOffset;
        public int recordOffset;
        public MidiCommandCode faderCode;
        public MidiCommandCode selectCode;
        public MidiCommandCode muteCode;
        public MidiCommandCode recordCode;

        public FaderDef(bool _delta, float _range, int _channel,
            bool _selectPresent, bool _mutePresent, bool _recordPresent,
            int _faderOffset, int _selectOffset, int _muteOffset, int _recordOffset,
            MidiCommandCode _faderCode, MidiCommandCode _selectCode, MidiCommandCode _muteCode, MidiCommandCode _recordCode)
        {
            delta = _delta;
            range = _range;
            channel = _channel;
            selectPresent = _selectPresent;
            mutePresent = _mutePresent;
            recordPresent = _recordPresent;
            faderOffset = _faderOffset;
            selectOffset = _selectOffset;
            muteOffset = _muteOffset;
            recordOffset = _recordOffset;
            faderCode = _faderCode;
            selectCode = _selectCode;
            muteCode = _muteCode;
            recordCode = _recordCode;
        }
    }

    public class Fader
    {
        public int faderNumber;
        public FaderDef faderDef;
        public MixerSession assignment;
        public bool assigned;
        public MidiOut midiOut;
        public MidiDevice parent;
        public string identifier;

        public Fader(MidiDevice midiDevice, int faderNum)
        {
            parent = midiDevice;
            midiOut = midiDevice.midiOut;
            faderNumber = faderNum;
            faderDef = parent.DefaultFaderDef;
        }

        public Fader(MidiDevice midiDevice, int faderNum, FaderDef _faderDef)
        {
            parent = midiDevice;
            midiOut = midiDevice.midiOut;
            faderNumber = faderNum;
            faderDef = _faderDef;
        }

        private int inputController => faderNumber + faderDef.faderOffset;
        private int selectController => faderNumber + faderDef.selectOffset;
        private int muteController => faderNumber + faderDef.muteOffset;
        private int recordController => faderNumber + faderDef.recordOffset;


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
            if (faderDef.delta)
                parent.SetVolumeIndicator(faderNumber, mixerSession.GetVolume());
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
            if (faderDef.delta)
                parent.SetVolumeIndicator(faderNumber, -1);
        }

        public void SetSelectLight(bool state)
        {
            parent.SetLight(selectController, state);
        }

        public void SetMuteLight(bool state)
        {
            parent.SetLight(muteController, state);
        }

        public void SetRecordLight( bool state)
        {
            parent.SetLight(recordController, state);
        }

        public bool Match(int faderNumber, MidiEvent midiEvent, MidiCommandCode code, int offset)
        {
            if (midiEvent.Channel != faderDef.channel)
                return false;
            if (midiEvent.CommandCode != code)
                return false;
            if (code == MidiCommandCode.ControlChange)
            {
                var me = (ControlChangeEvent)midiEvent;
                if ((int)me.Controller == faderNumber + offset)
                    return true;
            }
            else if (code == MidiCommandCode.NoteOn)
            {
                var me = (NoteEvent)midiEvent;
                if (me.NoteNumber == faderNumber + offset)
                    return true;
            }
            else if (code == MidiCommandCode.PitchWheelChange)
            {
                return true;
            }

            return false;
        }

        public int GetValue(MidiEvent midiEvent)
        {
            if (midiEvent.CommandCode == MidiCommandCode.ControlChange)
            {
                var me = (ControlChangeEvent)midiEvent;
                return me.ControllerValue;
            }
            else if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
            {
                var me = (NoteEvent)midiEvent;
                return me.Velocity;
            }
            else if (midiEvent.CommandCode == MidiCommandCode.PitchWheelChange)
            {
                var me = (PitchWheelChangeEvent)midiEvent;
                return me.Pitch;
            }

            return 0;
        }

        public bool HandleEvent(MidiInMessageEventArgs e)
        {
            // Fader match
            if (assigned && Match(faderNumber, e.MidiEvent, faderDef.faderCode, faderDef.faderOffset))
            {
                if (faderDef.delta)
                {
                    float curVol;
                    var val = GetValue(e.MidiEvent);
                    if (val > faderDef.range / 2)
                        curVol = assignment.ChangeVolume((faderDef.range - val) / faderDef.range);
                    else
                        curVol = assignment.ChangeVolume(val / faderDef.range);
                    parent.SetVolumeIndicator(faderNumber, curVol);
                }
                else
                {
                    assignment.SetVolume(GetValue(e.MidiEvent) / faderDef.range);
                }

                if (assignment.IsDead())
                    SetRecordLight(true);

                return true;
            }

            // Select match
            if (Match(faderNumber, e.MidiEvent, faderDef.selectCode, faderDef.selectOffset))
            {
                if (GetValue(e.MidiEvent) != 127) // Only on button-down
                    return true;

                Console.WriteLine($@"Attempting to assign current window to fader {faderNumber}");
                if (assigned)
                {
                    Unassign();
                    parent.SaveAssignments();
                }
                else
                {
                    var pid = WindowTools.GetForegroundPID();
                    var mixerSession = parent.audioDevice.FindMixerSessions(pid);
                    if (mixerSession != null)
                    {
                        Assign(mixerSession);
                        parent.SaveAssignments();
                    }
                    else
                        Console.WriteLine($@"MixerSession not found for pid {pid}");
                }
                return true;
            }

            // Mute match
            if (assigned && Match(faderNumber, e.MidiEvent, faderDef.muteCode, faderDef.muteOffset))
            {
                if (GetValue(e.MidiEvent) != 127) // Only on button-down
                    return true;

                SetMuteLight(assignment.ToggleMute());
                if (assignment.IsDead())
                    SetRecordLight(true);
                return true;
            }

            // Record match
            if (assigned && Match(faderNumber, e.MidiEvent, faderDef.recordCode, faderDef.recordOffset))
            {
                SetRecordLight(assignment.IsDead());
                return true;
            }
            return false;

        }
    }
}
