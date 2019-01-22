using NAudio.Midi;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class NanoKontrol2 : MidiDevice
    {
        public override string SearchString => "nano";
        public override FaderDef DefaultFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            true,  // selectPresent
            true,  // mutePresent
            true,  // recordPresent
            0,     // faderOffset
            32,    // selectOffset
            48,    // muteOffset
            64,    // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange // recordCode
        );

        public NanoKontrol2(AudioDevice audioDev)
        {
            audioDevice = audioDev;
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
            foreach (var i in Enumerable.Range(0, 8))
            {
                Fader fader = new Fader(this, i);
                fader.ResetLights();
                faders.Add(fader);
            }
        }

        public override void InitButtons()
        {
            buttons = new List<Button>();
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPrevious, 43, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaNext,     44, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaStop,     42, false));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPlay,     41, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaRecord,   45, false));
        }
    }
}
