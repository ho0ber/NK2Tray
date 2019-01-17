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

            if (32 <= controller && controller < 40)
                if (me.ControllerValue == 127)
                    return new ControlSurfaceEvent(ControlSurfaceEventType.Assignment, controller - 32);
                else
                    return null;

            if (48 <= controller && controller < 56)
                if (me.ControllerValue == 127)
                    return new ControlSurfaceEvent(ControlSurfaceEventType.FaderVolumeMute, controller - 48);
                else
                    return null;

            if (64 <= controller && controller < 72)
                if (me.ControllerValue == 127)
                    return new ControlSurfaceEvent(ControlSurfaceEventType.Information, controller - 64);
                else
                    return null;

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
        }
    }
}
