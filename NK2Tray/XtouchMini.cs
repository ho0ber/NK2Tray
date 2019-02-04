using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class XtouchMini : MidiDevice
    {
        public override string SearchString => "x-touch mini";
        public override FaderDef DefaultFaderDef => new FaderDef(
            true,  // delta
            64f,   // range
            1,     // channel
            true,  // selectPresent
            true,  // mutePresent
            false, // recordPresent
            16,    // faderOffset
            32,    // selectOffset
            38,    // muteOffset
            0,     // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.NoteOn,        // selectCode
            MidiCommandCode.NoteOn,        // muteCode
            MidiCommandCode.ControlChange  // recordCode
        );

        public FaderDef FirstTwoFaderDef => new FaderDef(
            true,  // delta
            64f,   // range
            1,     // channel
            true,  // selectPresent
            true,  // mutePresent
            false, // recordPresent
            16,    // faderOffset
            32,    // selectOffset
            89,    // muteOffset
            0,     // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.NoteOn,        // selectCode
            MidiCommandCode.NoteOn,        // muteCode
            MidiCommandCode.ControlChange  // recordCode
        );

        public FaderDef MasterFaderDef => new FaderDef(
            false,  // delta
            16383f, // range
            1,      // channel
            true,   // selectPresent
            true,   // mutePresent
            false,  // recordPresent
            0,      // faderOffset
            76,     // selectOffset
            77,     // muteOffset
            0,      // recordOffset
            MidiCommandCode.PitchWheelChange, // faderCode
            MidiCommandCode.NoteOn,           // selectCode
            MidiCommandCode.NoteOn,           // muteCode
            MidiCommandCode.ControlChange,    // recordCode
            9      // faderChannelOverride
        );

        public XtouchMini(AudioDevice audioDev)
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

        public override void SetVolumeIndicator(int fader, float level)
        {
            if (level >= 0)
            {
                var val = (int)Math.Round(level * 12) + 1 + 16 * 2;
                Console.WriteLine($@"Setting fader {fader} display to {level} ({val})");
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(fader + 48), val).GetAsShortMessage());
            }
            else
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(fader + 48), 0).GetAsShortMessage());
        }

        public override void ResetAllLights()
        {

            foreach (var i in Enumerable.Range(48, 8))
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());


            foreach (var i in Enumerable.Range(0, 128))
                midiOut.Send(new NoteOnEvent(0, 1, i, 0, 0).GetAsShortMessage());
        }

        public override void SetLight(int controller, bool state)
        {
            midiOut.Send(new NoteOnEvent(0, 1, controller, state ? 127 : 0, 0).GetAsShortMessage());
        }

        public override void InitFaders()
        {
            faders = new List<Fader>();
            
            foreach (var i in Enumerable.Range(0, 9))
            {
                Fader fader = new Fader(this, i, SelectFaderDef(i));
                fader.ResetLights();
                faders.Add(fader);
            }
        }

        public FaderDef SelectFaderDef(int faderNum)
        {
            if (faderNum < 2)
                return FirstTwoFaderDef;
            else if (faderNum == 8)
                return MasterFaderDef;
            else
                return DefaultFaderDef;
        }

        public override void InitButtons()
        {
            buttons = new List<Button>();

            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPrevious, 91, true,  MidiCommandCode.NoteOn));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaNext,     92, true,  MidiCommandCode.NoteOn));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaStop,     93, false, MidiCommandCode.NoteOn));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPlay,     94, true,  MidiCommandCode.NoteOn));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaRecord,   95, false, MidiCommandCode.NoteOn));
        }
    }
}
