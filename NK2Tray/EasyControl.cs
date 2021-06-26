using NAudio.Midi;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class EasyControl : MidiDeviceTemplate
    {
        public override bool hasLights => true;
        public override int lightCount => 128;
        public override MidiCommandCode lightMessageType => MidiCommandCode.ControlChange;
        public override bool hasVolumeIndicator => false;
        public override int fadersCount => 11;

        public override Button[] buttons => new Button[]
        {
            new Button(midiDevice, ButtonType.MediaPrevious, 34, true),
            new Button(midiDevice, ButtonType.MediaNext,     35, true),
            new Button(midiDevice, ButtonType.MediaStop,     36, false),
            new Button(midiDevice, ButtonType.MediaPlay,     37, true),
            new Button(midiDevice, ButtonType.MediaRecord,   38, false)
        };

        //public override string SearchString => "easy";

        public override FaderDef[] faderDefs => new FaderDef[]{
            new FaderDef( //FirstFourFaderDef
                false, // delta
                127f,   // range
                1,     // channel
                false,  // selectPresent
                true,  // mutePresent
                false,  // recordPresent
                false, // subfaderPresent
                14,     // faderOffset
                0,    // selectOffset
                23,    // muteOffset
                0,    // recordOffset
                0,    // subFaderOffset
                MidiCommandCode.ControlChange, // faderCode
                MidiCommandCode.ControlChange, // selectCode
                MidiCommandCode.ControlChange, // muteCode
                MidiCommandCode.ControlChange, // recordCode
                MidiCommandCode.ControlChange  // subFaderCode
            ),
            new FaderDef( //SecondFiveFaderDef
                false, // delta
                127f,   // range
                1,     // channel
                false,  // selectPresent
                true,  // mutePresent
                false,  // recordPresent
                false,  // subfaderPresent
                14,     // faderOffset
                0,    // selectOffset
                24,    // muteOffset
                0,    // recordOffset
                0,    // subfaderOffset
                MidiCommandCode.ControlChange, // faderCode
                MidiCommandCode.ControlChange, // selectCode
                MidiCommandCode.ControlChange, // muteCode
                MidiCommandCode.ControlChange, // recordCode
                MidiCommandCode.ControlChange  // subFaderCode
            ),
            new FaderDef( //KnobFaderDef
                false, // delta
                127f,   // range
                1,     // channel
                true,  // selectPresent
                true,  // mutePresent
                false,  // recordPresent
                false,  // subfaderPresent
                1,     // faderOffset
                55,    // selectOffset
                58,    // muteOffset
                0,    // recordOffset
                0,    // subfaderOffset
                MidiCommandCode.ControlChange, // faderCode
                MidiCommandCode.ControlChange, // selectCode
                MidiCommandCode.ControlChange, // muteCode
                MidiCommandCode.ControlChange, // recordCode
                MidiCommandCode.ControlChange  // subFaderCode
            ),
            new FaderDef( //HorizontalFaderDef
                false, // delta
                127f,   // range
                1,     // channel
                true,  // selectPresent
                true,  // mutePresent
                true,  // recordPresent
                false, // subfaderPresent
                -1,     // faderOffset
                -8,    // selectOffset
                -9,    // muteOffset
                17,    // recordOffset
                0,     // subfaderOffset
                MidiCommandCode.ControlChange, // faderCode
                MidiCommandCode.ControlChange, // selectCode
                MidiCommandCode.ControlChange, // muteCode
                MidiCommandCode.ControlChange, // recordCode
                MidiCommandCode.ControlChange  // subFaderCode
            )
        };

        public EasyControl(MidiDevice midiDev) : base(midiDev)
        {

        }

        public override int SelectFaderDef(int faderNum)
        {
            if (faderNum < 4) return 0; //FirstFourFaderDef
            if (faderNum < 9) return 1; //SecondFiveFaderDef
            if (faderNum == 9) return 2; //KnobFaderDef

            return 3; //HorizontalFaderDef
        }
    }
}
