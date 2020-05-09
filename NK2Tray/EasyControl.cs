using NAudio.Midi;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class EasyControl : MidiDevice
    {
        public override string SearchString => "easy";
        public FaderDef FirstFourFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            false,  // selectPresent
            true,  // mutePresent
            false,  // recordPresent
            14,     // faderOffset
            0,    // selectOffset
            23,    // muteOffset
            0,    // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange // recordCode
        );

        public FaderDef SecondFiveFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            false,  // selectPresent
            true,  // mutePresent
            false,  // recordPresent
            14,     // faderOffset
            0,    // selectOffset
            24,    // muteOffset
            0,    // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange // recordCode
        );

        public FaderDef KnobFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            true,  // selectPresent
            true,  // mutePresent
            false,  // recordPresent
            1,     // faderOffset
            55,    // selectOffset
            58,    // muteOffset
            0,    // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange // recordCode
        );

        public FaderDef HorizontalFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            true,  // selectPresent
            true,  // mutePresent
            true,  // recordPresent
            -1,     // faderOffset
            -8,    // selectOffset
            -9,    // muteOffset
            17,    // recordOffset
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

            foreach (var i in Enumerable.Range(0, 11))
            {
                Fader fader = new Fader(this, i, SelectFaderDef(i));
                fader.ResetLights();
                faders.Add(fader);
            }
        }

        public FaderDef SelectFaderDef(int faderNum)
        {
            if (faderNum < 4) return FirstFourFaderDef;
            if (faderNum < 9) return SecondFiveFaderDef;
            if (faderNum == 9) return KnobFaderDef;

            return HorizontalFaderDef;
        }

        public override void InitButtons()
        {
            buttons = new List<Button>();
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPrevious, 34, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaNext,     35, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaStop,     36, false));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPlay,     37, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaRecord,   38, false));
        }
    }
}
