using NAudio.Midi;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class OP1 : MidiDevice
    {
        public override string SearchString => "op-1";

        public FaderDef FirstFader => new FaderDef(
            false,   // delta
            127f,    // range
            1,      // channel
            true,   // selectPresent
            true,   // mutePresent
            true,  // recordPresent
            1,      // faderOffset
            64,     // selectOffset
            50,     // muteOffset
            51,     // recordOffset
            MidiCommandCode.ControlChange,  // faderCode
            MidiCommandCode.ControlChange,  // selectCode
            MidiCommandCode.ControlChange,  // muteCode
            MidiCommandCode.ControlChange   // recordCode
        );

        public FaderDef SecondFader => new FaderDef(
            false,   // delta
            127f,    // range
            1,      // channel
            true,   // selectPresent
            true,   // mutePresent
            true,  // recordPresent
            1,      // faderOffset
            64,     // selectOffset
            51,     // muteOffset
            20,     // recordOffset
            MidiCommandCode.ControlChange,  // faderCode
            MidiCommandCode.ControlChange,  // selectCode
            MidiCommandCode.ControlChange,  // muteCode
            MidiCommandCode.ControlChange   // recordCode
        );

        public FaderDef ThirdFader => new FaderDef(
            false,   // delta
            127f,    // range
            1,      // channel
            true,   // selectPresent
            true,   // mutePresent
            true,  // recordPresent
            1,      // faderOffset
            64,     // selectOffset
            20,     // muteOffset
            21,     // recordOffset
            MidiCommandCode.ControlChange,  // faderCode
            MidiCommandCode.ControlChange,  // selectCode
            MidiCommandCode.ControlChange,  // muteCode
            MidiCommandCode.ControlChange   // recordCode
        );

        public FaderDef FourthFader => new FaderDef(
            false,   // delta
            127f,    // range
            1,      // channel
            true,   // selectPresent
            true,   // mutePresent
            true,  // recordPresent
            1,      // faderOffset
            64,     // selectOffset
            21,     // muteOffset
            22,     // recordOffset
            MidiCommandCode.ControlChange,  // faderCode
            MidiCommandCode.ControlChange,  // selectCode
            MidiCommandCode.ControlChange,  // muteCode
            MidiCommandCode.ControlChange   // recordCode
        );

        public OP1(AudioDevice audioDev)
        {
            audioDevices = audioDev;
            FindMidiIn();
            FindMidiOut();

            if (Found)
            {
                InitFaders();
                InitButtons();
                LoadAssignments();
                ListenForMidi();
            }
        }

        public override void InitFaders()
        {
            faders = new List<Fader>
            {
                new Fader(this, 0, FirstFader),
                new Fader(this, 1, SecondFader),
                new Fader(this, 2, ThirdFader),
                new Fader(this, 3, FourthFader)
            };
        }

        public override void InitButtons()
        {
            buttons = new List<Button>
            {
                new Button(ref midiOut, ButtonType.MediaPlay, 39, true),
                new Button(ref midiOut, ButtonType.MediaStop, 40, false),
                new Button(ref midiOut, ButtonType.MediaNext, 16, true),
                new Button(ref midiOut, ButtonType.MediaPrevious, 15, true)
            };

            // is this still needed?
            buttonsMappingTable = new Hashtable();

            foreach (var button in buttons)
            {
                buttonsMappingTable.Add(button.controller, button);
            }
        }
    }
}
