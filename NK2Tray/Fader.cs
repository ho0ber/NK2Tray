using NAudio.CoreAudioApi;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public int faderChannelOverride;
        public int selectChannelOverride;
        public int muteChannelOverride;
        public int recordChannelOverride;

        public FaderDef(bool _delta, float _range, int _channel,
            bool _selectPresent, bool _mutePresent, bool _recordPresent,
            int _faderOffset, int _selectOffset, int _muteOffset, int _recordOffset,
            MidiCommandCode _faderCode, MidiCommandCode _selectCode, MidiCommandCode _muteCode, MidiCommandCode _recordCode,
            int _faderChannelOverride = -1, int _selectChannelOverride = -1, int _muteChannelOverride = -1, int _recordChannelOverride = -1)
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
            faderChannelOverride = _faderChannelOverride;
            selectChannelOverride = _selectChannelOverride;
            muteChannelOverride = _muteChannelOverride;
            recordChannelOverride = _recordChannelOverride;
        }
    }

    public class Fader
    {
        private bool activeHandling = false;
        private bool selectLight = false;
        private bool muteLight = false;
        private bool recordLight = false;
        // private ThrottledEventHandler<VolumeChangedEventArgs> volumeChangedHandler;
        // private EventHandler<VolumeChangedEventArgs> volumeChangedHandler;

        public int faderNumber;
        public FaderDef faderDef;
        public Assignment assignment;
        public bool assigned => (assignment != null);
        public MidiOut midiOut;
        public MidiDevice parent;
        public string identifier;
        public string applicationPath;
        public string applicationName;

        public float[] steps;
        public float pow;

        public Fader(MidiDevice midiDevice, int faderNum)
        {
            parent = midiDevice;
            midiOut = midiDevice.midiOut;
            faderNumber = faderNum;
            faderDef = parent.DefaultFaderDef;
            SetCurve(1f);
            // volumeChangedHandler = new ThrottledEventHandler<VolumeChangedEventArgs>(MixerSession_VolumeChanged, TimeSpan.FromMilliseconds(100));
            // volumeChangedHandler = new EventHandler<VolumeChangedEventArgs>(MixerSession_VolumeChanged);
        }

        public Fader(MidiDevice midiDevice, int faderNum, FaderDef _faderDef)
        {
            parent = midiDevice;
            midiOut = midiDevice.midiOut;
            faderNumber = faderNum;
            faderDef = _faderDef;
            SetCurve(1f);
            // volumeChangedHandler = new ThrottledEventHandler<VolumeChangedEventArgs>(MixerSession_VolumeChanged, TimeSpan.FromMilliseconds(100));
        }

        public void SetCurve(float _pow)
        {
            pow = _pow;
            steps = calculateSteps();
            parent.SetVolumeIndicator(faderNumber, assignment != null ? assignment.GetVolume() : -1);
        }

        private float[] calculateSteps()
        {
            if (!faderDef.delta) return new float[0];

            return Enumerable.Range(0, (int)faderDef.range + 1).Select(stage => getVolFromStage(stage)).ToArray();
        }

        public float getVolFromStage(int stage)
        {
            return (float)Math.Pow((double)stage / faderDef.range, pow);
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

        /*
        public void Assign(MixerSession mixerSession)
        {
            // if (assignment != null) assignment.VolumeChanged -= MixerSession_VolumeChanged;
            assigned = true;
            assignment = mixerSession;
            identifier = mixerSession.sessionIdentifier;
            convertToApplicationPath(identifier);
            applicationName = mixerSession.label;
            SetSelectLight(true);
            SetRecordLight(false);
            SetMuteLight(mixerSession.GetMute());

            // Subscribe to mixer session volume changes
            mixerSession.VolumeChanged += MixerSession_VolumeChanged;

            if (faderDef.delta)
                parent.SetVolumeIndicator(faderNumber, mixerSession.GetVolume());
        }
        */

        // Assign a fader to an audio device
        public void Assign (MMDevice device)
        {
            var assignment = new Assignment(parent.audioDeviceWatcher, device);
            Assign(assignment);
        }

        // Assign a fader to a mixer entry
        public void Assign (string sessionId)
        {
            var assignment = new Assignment(parent.audioDeviceWatcher, sessionId);
            Assign(assignment);
        }

        // Assign a fader to the focused application
        public void Assign (bool _)
        {
            var assignment = new Assignment(parent.audioDeviceWatcher);
            Assign(assignment);
        }

        // Assign a fader to an existing assignment
        public void Assign (Assignment assignment)
        {
            if (this.assignment != null) this.assignment.Dispose();
            assignment.fader = this;
            this.assignment = assignment;
            RefreshStatus();
        }

        public void Assign()
        {
            if (this.assignment != null) this.assignment.Dispose();
            this.assignment = null;
            ResetLights();
            SetVolumeIndicator(-1);
        }

        public void RefreshStatus()
        {
            SetSelectLight(assigned);
            SetRecordLight(false);
            SetMuteLight(assigned ? assignment.GetMute() : false);
            SetVolumeIndicator(assigned ? assignment.GetVolume() : 0);
        }

        /*
        private void MixerSession_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            var faders = GetMatchingFaders();

            foreach (var fader in faders)
            {
                fader.parent.SetVolumeIndicator(fader.faderNumber, e.volume);
                fader.SetMuteLight(e.isMuted);
            }
        }
        */

        /*
        public void AssignInactive(string ident)
        {
            if (assignment != null) assignment.VolumeChanged -= MixerSession_VolumeChanged;
            identifier = ident;
            convertToApplicationPath(identifier);
            assigned = false;
            SetSelectLight(true);
            SetRecordLight(true);
            SetMuteLight(false);
        }
        */

        /*
        public void Unassign()
        {
            if (assignment != null) assignment.VolumeChanged -= MixerSession_VolumeChanged;
            assigned = false;
            assignment = null;
            SetSelectLight(false);
            SetRecordLight(false);
            SetMuteLight(false);
            identifier = "";

            if (faderDef.delta)
                parent.SetVolumeIndicator(faderNumber, -1);
        }
        */

        private void convertToApplicationPath(string ident)
        {
            if (
                ident != null
                && ident != ""
                && !ident.StartsWith("#")
                && !ident.Substring(0, Math.Min(10, ident.Length)).Equals("__MASTER__")
                && ident != "__FOCUS__"
            ) // TODO cleaner handling of special fader types
            {
                //"{0.0.0.00000000}.{...}|\\Device\\HarddiskVolume8\\Users\\Dr. Locke\\AppData\\Roaming\\Spotify\\Spotify.exe%b{00000000-0000-0000-0000-000000000000}"
                int deviceIndex = ident.IndexOf("\\Device");
                int endIndex = ident.IndexOf("%b{");
                ident = ident.Substring(deviceIndex, endIndex - deviceIndex);
                applicationPath = DevicePathMapper.FromDevicePath(ident);
            }
        }

        public void SetSelectLight(bool state)
        {
            selectLight = state;
            parent.SetLight(selectController, state);
        }

        public void SetMuteLight(bool state)
        {
            muteLight = state;
            parent.SetLight(muteController, state);
        }

        public void SetRecordLight(bool state)
        {
            recordLight = state;
            parent.SetLight(recordController, state);
        }

        public float GetVolumeDisplay (float volume)
        {
            if (faderDef.delta)
            {
                var nearestStep = steps.Select((x, i) => new { Index = i, Distance = Math.Abs(volume - x) }).OrderBy(x => x.Distance).First().Index;

                return steps[nearestStep];
            }

            return (float)Math.Pow(volume, pow);
        }

        public float GetVolumeDisplay ()
        {
            var volume = this.assigned ? this.assignment.GetVolume() : 0;

            return GetVolumeDisplay(volume);
        }

        public void SetVolumeIndicator (float volume)
        {
            var displayVol = volume >= 0 ? GetVolumeDisplay(volume) : -1;
            parent.SetVolumeIndicator(faderNumber, displayVol);
        }

        public bool GetSelectLight() { return selectLight; }

        public bool GetMuteLight() { return muteLight; }

        public bool GetRecordLight() { return recordLight; }

        public bool Match(int faderNumber, MidiEvent midiEvent, MidiCommandCode code, int offset, int channel = -1)
        {
            if (channel < 0)
                channel = faderDef.channel;
            if (midiEvent.Channel != channel)
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

        public void HandleEvent (MidiInMessageEventArgs e)
        {
            if (IsHandling()) return;
            SetHandling(true);

            // Try fader match
            if (
                assigned
                && Match(faderNumber, e.MidiEvent, faderDef.faderCode, faderDef.faderOffset, faderDef.faderChannelOverride)
            )
            {
                float newVol;

                if (faderDef.delta)
                {
                    var val = GetValue(e.MidiEvent);
                    var volNow = assignment.GetVolume();
                    var nearestStep = steps.Select((x, i) => new { Index = i, Distance = Math.Abs(volNow - x) }).OrderBy(x => x.Distance).First().Index;
                    int nextStepIndex;

                    var volumeGoingDown = val > faderDef.range / 2;

                    if (volumeGoingDown)
                        nextStepIndex = Math.Max(nearestStep - 1, 0);
                    else
                        nextStepIndex = Math.Min(nearestStep + 1, steps.Length - 1);

                    newVol = steps[nextStepIndex];
                    assignment.SetVolume(newVol);
                }
                else
                {
                    newVol = getVolFromStage(GetValue(e.MidiEvent));
                    assignment.SetVolume(newVol);
                }

                return;
            }

            // Try select match
            if (Match(faderNumber, e.MidiEvent, faderDef.selectCode, faderDef.selectOffset, faderDef.selectChannelOverride))
            {
                // Only on button down
                if (GetValue(e.MidiEvent) != 127) return;

                if (assigned)
                {
                    Assign();
                    parent.SaveAssignments();

                    return;
                }

                var sessionId = parent.audioDeviceWatcher.GetForegroundSessionId();

                if (!String.IsNullOrEmpty(sessionId))
                {
                    Assign(sessionId);
                    parent.SaveAssignments();
                }

                return;
            }

            // Try mute match
            if (
                assigned
                && Match(faderNumber, e.MidiEvent, faderDef.muteCode, faderDef.muteOffset, faderDef.muteChannelOverride)
            )
            {
                // Only on button down
                if (GetValue(e.MidiEvent) != 127) return;

                var muteStatus = assignment.ToggleMute();
                SetMuteLight(muteStatus);

                return;
            }

            // Try record match
            if (
                assigned
                && applicationPath != null
                && Match(faderNumber, e.MidiEvent, faderDef.recordCode, faderDef.recordOffset, faderDef.recordChannelOverride)
            )
            {
                throw new NotImplementedException();
            }
        }

        public bool IsHandling()
        {
            return activeHandling;
        }

        public void SetHandling(bool handling)
        {
            activeHandling = handling;
        }
    }
}
