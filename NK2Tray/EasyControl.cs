using NAudio.Midi;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class EasyControl : MidiDevice
    {
        public override string SearchString => "easy";
        public override FaderDef DefaultFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            false,  // selectPresent
            true,  // mutePresent
            false,  // recordPresent
            0,     // faderOffset
            23,    // selectOffset
            48,    // muteOffset
            64,    // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange // recordCode
        );

        public FaderDef HorizntalFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            false,  // selectPresent
            true,  // mutePresent
            false,  // recordPresent
            0,     // faderOffset
            23,    // selectOffset
            -8,    // muteOffset
            64,    // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange // recordCode
        );

        public EasyControl(AudioDevice audioDev)
        {
            audioDevices = audioDev;
            FindMidiIn();
            FindMidiOut();
            if (Found)
            {
                ResetAllLights();
                InitFaders();
                InitButtons();
                LoadAssignments();
                ListenForMidi();
            }
        }

        public override void ResetAllLights()
        {
            foreach (var i in Enumerable.Range(0, 128))
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());
        }

        public override void SetLight(int controller, bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(controller), state ? 127 : 0).GetAsShortMessage());
        }

        public override void InitFaders()
        {
            faders = new List<Fader>();

            foreach (var i in Enumerable.Range(0, 10))
            {
                Fader fader = (i < 9) ? new Fader(this, i) : new Fader(this, i, this.HorizntalFaderDef);
                fader.ResetLights();
                faders.Add(fader);
            }
        }

        public override void InitButtons()
        {
            buttons = new List<Button>();
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPrevious, 47, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaNext,     48, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaStop,     46, false));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPlay,     45, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaRecord,   44, false));
        }
    }
}
