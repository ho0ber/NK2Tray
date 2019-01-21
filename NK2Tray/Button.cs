using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{
    public enum ButtonType
    {
        MediaPlay,
        MediaStop,
        MediaPrevious,
        MediaNext,
        MediaRecord
    }

    public class Button
    {
        public ButtonType buttonType;
        public int controller;
        public MidiCommandCode commandCode;
        public int channel;
        public MidiOut midiOut;

        public Button(ref MidiOut midiOutRef, ButtonType butType, int cont, bool initialState)
        {
            commandCode = MidiCommandCode.ControlChange;
            channel = 1;
            buttonType = butType;
            controller = cont;
            midiOut = midiOutRef;
            SetLight(initialState);
        }

        public void SetLight(bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, channel, (MidiController)(controller), state ? 127 : 0).GetAsShortMessage());
        }

        public bool HandleEvent(MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode != commandCode)
                return false;

            ControlChangeEvent me = (ControlChangeEvent)e.MidiEvent;

            if (me.Channel != channel || me.ControllerValue != 127) // Only on correct channel and button-down (127)
                return false;

            int c = (int)me.Controller;

            if (c == controller)
            {
                switch (buttonType)
                {
                    case ButtonType.MediaNext:
                        MediaTools.Next();
                        break;
                    case ButtonType.MediaPrevious:
                        MediaTools.Previous();
                        break;
                    case ButtonType.MediaStop:
                        MediaTools.Stop();
                        break;
                    case ButtonType.MediaPlay:
                        MediaTools.Play();
                        break;
                    case ButtonType.MediaRecord:
                        throw new Exception("Kaboom");
                    default:
                        break;
                }
                return true;
            }

            return false;
         }

    }
}
