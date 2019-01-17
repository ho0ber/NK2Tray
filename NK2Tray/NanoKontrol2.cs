using NAudio.Midi;
using System;

namespace NK2Tray
{
    class NanoKontrol2
    {
        public const int FADERS = 8;

        public static ControlSurfaceEvent Evaluate(MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode != MidiCommandCode.ControlChange)
                return null;

            ControlChangeEvent me = (ControlChangeEvent)e.MidiEvent;

            if (me.Channel != 1)
                return null;

            int controller = (int)me.Controller;
            if (0 <= controller && controller < 8)
                return new ControlSurfaceEvent(ControlSurfaceEventType.FaderVolumeChange, controller, me.ControllerValue / 127f);
            else if (32 <= controller && controller < 40 && me.ControllerValue == 127)
                return new ControlSurfaceEvent(ControlSurfaceEventType.Assignment, controller - 32);
            else if (48 <= controller && controller < 56 && me.ControllerValue == 127)
                return new ControlSurfaceEvent(ControlSurfaceEventType.FaderVolumeMute, controller - 48);
            else if (64 <= controller && controller < 72 && me.ControllerValue == 127)
                return new ControlSurfaceEvent(ControlSurfaceEventType.Information, controller - 64);

            switch (controller)
            {
                case 43 when me.ControllerValue == 127:
                    return new ControlSurfaceEvent(ControlSurfaceEventType.MediaPrevious);
                case 44 when me.ControllerValue == 127:
                    return new ControlSurfaceEvent(ControlSurfaceEventType.MediaNext);
                case 42 when me.ControllerValue == 127:
                    return new ControlSurfaceEvent(ControlSurfaceEventType.MediaStop);
                case 41 when me.ControllerValue == 127:
                    return new ControlSurfaceEvent(ControlSurfaceEventType.MediaPlay);
                case 45 when me.ControllerValue == 127:
                    return new ControlSurfaceEvent(ControlSurfaceEventType.MediaRecord);
            }

            Console.WriteLine(String.Format("controller: {0}  value: {1}", controller, me.ControllerValue));
            return null;
        }

        public static void Respond(ref MidiOut midiOut, ControlSurfaceDisplay csd)
        {
            if (csd.displayType == ControlSurfaceDisplayType.MuteState)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(csd.fader + 48), csd.state ? 127 : 0).GetAsShortMessage());
            else if (csd.displayType == ControlSurfaceDisplayType.AssignedState)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(csd.fader + 32), csd.state ? 127 : 0).GetAsShortMessage());
            else if (csd.displayType == ControlSurfaceDisplayType.ErrorState)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(csd.fader + 64), csd.state ? 127 : 0).GetAsShortMessage());
            else if (csd.displayType == ControlSurfaceDisplayType.MediaPrevious)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)43, csd.state ? 127 : 0).GetAsShortMessage());
            else if (csd.displayType == ControlSurfaceDisplayType.MediaNext)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)44, csd.state ? 127 : 0).GetAsShortMessage());
            else if (csd.displayType == ControlSurfaceDisplayType.MediaStop)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)42, csd.state ? 127 : 0).GetAsShortMessage());
            else if (csd.displayType == ControlSurfaceDisplayType.MediaPlay)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)41, csd.state ? 127 : 0).GetAsShortMessage());
            else if (csd.displayType == ControlSurfaceDisplayType.MediaRecord)
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)45, csd.state ? 127 : 0).GetAsShortMessage());
        }
    }
}
