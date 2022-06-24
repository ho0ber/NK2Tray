using NAudio.Midi;
using System;

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
        private bool activeHandling = false;

        private bool light = false;

        public ButtonType buttonType;
        public int controller;
        public MidiCommandCode commandCode;
        public int channel;
        public MidiDevice midiDevice;

        public Button(MidiDevice midiDeviceRef, ButtonType butType, int cont, bool initialState, MidiCommandCode code=MidiCommandCode.ControlChange)
        {
            midiDevice = midiDeviceRef;
            commandCode = code;
            channel = 1;
            buttonType = butType;
            controller = cont;
            SetLight(initialState);
        }

        public void SetLight(bool state)
        {
            light = state;
            refreshLight();
        }

        public void refreshLight()
        {
            midiDevice.SetLight(controller, light);
        }

        public void SetLightTemp(bool state)
        {
            midiDevice.SetLight(controller, state);
        }

        public bool HandleEvent(MidiInMessageEventArgs e, MidiDevice device)
        {
            if (!IsHandling())
            {
                SetHandling(true);

                if (e.MidiEvent.CommandCode != commandCode)
                    return false;

                int c;

                if (commandCode == MidiCommandCode.ControlChange)
                {
                    var me = (ControlChangeEvent)e.MidiEvent;

                    if (me.Channel != channel || me.ControllerValue != 127) // Only on correct channel and button-down (127)
                        return false;

                    c = (int)me.Controller;
                }
                else if (commandCode == MidiCommandCode.NoteOn)
                {
                    var me = (NoteEvent)e.MidiEvent;

                    if (me.Channel != channel || me.Velocity != 127) // Only on correct channel and button-down (127)
                        return false;

                    c = me.NoteNumber;
                }
                else
                    return false;

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
                            device.LightShow();
                            break;
                        default:
                            break;
                    }
                    return true;
                }
            }

            return false;
        }

        public bool IsHandling()
        {
            return activeHandling;
        }

        public void SetHandling(bool handling)
        {
            activeHandling = handling;
        }

        public bool GetLight() { return light; }

    }
}
