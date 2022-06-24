using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace NK2Tray
{
    public class XtouchMini : MidiDeviceTemplate
    {
        public override bool hasLights => true;
        public override int lightCount => 128;
        public override MidiCommandCode lightMessageType => MidiCommandCode.NoteOn;
        public override bool hasVolumeIndicator => true;

        public override int fadersCount => 9;

        public override Button[] buttons => new Button[]
        {
            new Button(midiDevice, ButtonType.MediaPrevious, 91, true, MidiCommandCode.NoteOn),
            new Button(midiDevice, ButtonType.MediaNext,     92, true, MidiCommandCode.NoteOn),
            new Button(midiDevice, ButtonType.MediaStop,     93, false, MidiCommandCode.NoteOn),
            new Button(midiDevice, ButtonType.MediaPlay,     94, true, MidiCommandCode.NoteOn),
            new Button(midiDevice, ButtonType.MediaRecord,   95, false, MidiCommandCode.NoteOn)
        };

        //public override string SearchString => "x-touch mini";

        public override FaderDef[] faderDefs => new FaderDef[]{
            new FaderDef( //Normal
                true,  // delta
                64f,   // range
                1,     // channel
                true,  // selectPresent
                true,  // mutePresent
                false, // recordPresent
                false, // subFaderPresent
                16,    // faderOffset
                32,    // selectOffset
                38,    // muteOffset
                0,     // recordOffset
                0,     // subFaderOffset
                MidiCommandCode.ControlChange, // faderCode
                MidiCommandCode.NoteOn,        // selectCode
                MidiCommandCode.NoteOn,        // muteCode
                MidiCommandCode.ControlChange, // recordCode
                MidiCommandCode.ControlChange  // subFaderCode
            ),
            new FaderDef( //First two
                true,  // delta
                64f,   // range
                1,     // channel
                true,  // selectPresent
                true,  // mutePresent
                false, // recordPresent
                false, // subFaderPresent
                16,    // faderOffset
                32,    // selectOffset
                89,    // muteOffset
                0,     // recordOffset
                0,     // subFaderOffset
                MidiCommandCode.ControlChange, // faderCode
                MidiCommandCode.NoteOn,        // selectCode
                MidiCommandCode.NoteOn,        // muteCode
                MidiCommandCode.ControlChange, // recordCode
                MidiCommandCode.ControlChange  // subFaderCode
            ),
            new FaderDef( //Master
                false,  // delta
                16256f, // range
                1,      // channel
                true,   // selectPresent
                true,   // mutePresent
                false,  // recordPresent
                false,  // subFaderPresent
                0,      // faderOffset
                76,     // selectOffset
                77,     // muteOffset
                0,      // recordOffset
                0,      // subFaderOffset
                MidiCommandCode.PitchWheelChange, // faderCode
                MidiCommandCode.NoteOn,           // selectCode
                MidiCommandCode.NoteOn,           // muteCode
                MidiCommandCode.ControlChange,    // recordCode
                MidiCommandCode.ControlChange,    // subFaderCode
                9      // faderChannelOverride
            )
        };

        public XtouchMini(MidiDevice midiDev) : base(midiDev)
        {

        }

        public override void ResetSuppLights(MidiOut midiOut)
        {
            foreach (var i in Enumerable.Range(48, 8))
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());

        }

        public override int SelectFaderDef(int faderNum)
        {
            if (faderNum < 2)
                return 1; // First two
            else if (faderNum == 8)
                return 2; //Master
            return 0;
        }
    }
}
