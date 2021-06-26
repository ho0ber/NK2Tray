using NAudio.Midi;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class OP1 : MidiDeviceTemplate
    {
        public override bool hasLights => false;
        //public override int lightCount => 128;
        //public override MidiCommandCode lightMessageType => MidiCommandCode.ControlChange;
        //public override bool hasVolumeIndicator => false;

        public override int fadersCount => 4;

        public override Button[] buttons => new Button[]
        {
            new Button(midiDevice, ButtonType.MediaPlay, 39, true),
            new Button(midiDevice, ButtonType.MediaStop, 40, false),
            new Button(midiDevice, ButtonType.MediaNext, 16, true),
            new Button(midiDevice, ButtonType.MediaPrevious, 15, true)
        };

        //public override string SearchString => "op-1";

        public override FaderDef[] faderDefs => new FaderDef[]{
            new FaderDef(
                false,   // delta
                127f,    // range
                1,      // channel
                true,   // selectPresent
                true,   // mutePresent
                true,  // recordPresent
                false, // subFaderPresent
                1,      // faderOffset
                64,     // selectOffset
                50,     // muteOffset
                51,     // recordOffset
                0,      // subFaderOffset
                MidiCommandCode.ControlChange,  // faderCode
                MidiCommandCode.ControlChange,  // selectCode
                MidiCommandCode.ControlChange,  // muteCode
                MidiCommandCode.ControlChange,  // recordCode
                MidiCommandCode.ControlChange   // subFaderCode
            ),
            new FaderDef(
                false,   // delta
                127f,    // range
                1,      // channel
                true,   // selectPresent
                true,   // mutePresent
                true,  // recordPresent
                false, // subFaderPresent
                1,      // faderOffset
                64,     // selectOffset
                51,     // muteOffset
                20,     // recordOffset
                0,      // subFaderOffset
                MidiCommandCode.ControlChange,  // faderCode
                MidiCommandCode.ControlChange,  // selectCode
                MidiCommandCode.ControlChange,  // muteCode
                MidiCommandCode.ControlChange,  // recordCode
                MidiCommandCode.ControlChange   // subFaderCode
            ),
            new FaderDef(
                false,   // delta
                127f,    // range
                1,      // channel
                true,   // selectPresent
                true,   // mutePresent
                true,  // recordPresent
                false, // subFaderPresent
                1,      // faderOffset
                64,     // selectOffset
                20,     // muteOffset
                21,     // recordOffset
                0,      // subFaderOffset
                MidiCommandCode.ControlChange,  // faderCode
                MidiCommandCode.ControlChange,  // selectCode
                MidiCommandCode.ControlChange,  // muteCode
                MidiCommandCode.ControlChange,  // recordCode
                MidiCommandCode.ControlChange   // subFaderCode
            ),
            new FaderDef(
                false,   // delta
                127f,    // range
                1,      // channel
                true,   // selectPresent
                true,   // mutePresent
                true,  // recordPresent
                false,  // subFaderPresent
                1,      // faderOffset
                64,     // selectOffset
                21,     // muteOffset
                22,     // recordOffset
                0,      // subFaderOffset
                MidiCommandCode.ControlChange,  // faderCode
                MidiCommandCode.ControlChange,  // selectCode
                MidiCommandCode.ControlChange,  // muteCode
                MidiCommandCode.ControlChange,  // recordCode
                MidiCommandCode.ControlChange   // subFaderCode
            )
        };


        public OP1(MidiDevice midiDev) : base(midiDev)
        {
            
        }
    }
}
