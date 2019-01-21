using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{
    public class Fader
    {
        public MidiCommandCode commandCode;
        public int channel;
        public int faderNumber;
        public int inputController;
        public int selectController;
        public int muteController;
        public int recordController;
        public MixerSession assignment;
        public bool assigned;
        public MidiOut midiOut;

        public Fader(ref MidiOut midiOutRef, int faderNum, int inputOffst, int selectOffset, int muteOffset, int recordOffset)
        {
            midiOut = midiOutRef;
            commandCode = MidiCommandCode.ControlChange;
            channel = 1;
            faderNumber = faderNum;
            inputController = faderNum + inputOffst;
            selectController = faderNum + selectOffset;
            muteController = faderNum + muteOffset;
            recordController = faderNum + recordOffset;
        }

        public void ResetLights()
        {
            SetSelectLight(false);
            SetMuteLight(false);
            SetRecordLight(false);
        }

        public void Assign(MixerSession mixerSession)
        {
            assigned = true;
            assignment = mixerSession;
            SetSelectLight(true);
        }

        public void Unassign()
        {
            assigned = false;
            assignment = null;
            SetSelectLight(false);
        }

        public void SetSelectLight(bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(selectController), state ? 127 : 0).GetAsShortMessage());
        }

        public void SetMuteLight(bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(muteController), state ? 127 : 0).GetAsShortMessage());
        }

        public void SetRecordLight( bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(recordController), state ? 127 : 0).GetAsShortMessage());
        }

        public bool HandleEvent(MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode != commandCode)
                return false;

            ControlChangeEvent me = (ControlChangeEvent)e.MidiEvent;

            if (me.Channel != channel)
                return false;

            int controller = (int)me.Controller;

            if (controller == inputController)
            {
                if (assigned)
                    assignment.SetVolume(me.ControllerValue / 127f);
                return true;
            }
            else if (controller == selectController)
            {
                //select foreground app
                return true;
            }
            else if (controller == muteController)
            {
                //mute app
                return true;
            }
            else if (controller == recordController)
            {
                //get info
                return true;
            }
            return false;
        }
    }
}

/*
            ControlSurfaceEvent cse = NanoKontrol2.Evaluate(e);
            if (cse == null)
                return;

            if (cse.fader > 0 && assignments[cse.fader].assigned && !assignments[cse.fader].IsAlive())
                NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, cse.fader, true));

            switch (cse.eventType)
            {
                case ControlSurfaceEventType.FaderVolumeChange:
                    ChangeApplicationVolume(cse);
                    break;
                case ControlSurfaceEventType.FaderVolumeMute:
                    MuteApplication(cse);
                    break;
                case ControlSurfaceEventType.Information:
                    GetAssignmentInformation(cse);
                    break;
                case ControlSurfaceEventType.Assignment:
                    AssignForegroundSession(cse);
                    break;
                case ControlSurfaceEventType.MediaNext:
                    MediaTools.Next();
                    break;
                case ControlSurfaceEventType.MediaPlay:
                    MediaTools.Play();
                    break;
                case ControlSurfaceEventType.MediaPrevious:
                    MediaTools.Previous();
                    break;
                case ControlSurfaceEventType.MediaRecord:
                    throw new Exception("Kaboom");
                    // Maybe mute/unmute microphone?
                    break;
                case ControlSurfaceEventType.MediaStop:
                    MediaTools.Stop();
                    break;
                default:
                    break;
            }
            */


    /*
public void AssignForegroundSession(ControlSurfaceEvent cse)
{
    if (assignments[cse.fader].assigned)
    {
        UnassignFader(cse.fader);
    }
    else
    {
        var pid = WindowTools.GetForegroundPID();
        UpdateDevice();
        var matchingSession = FindSession(pid);
        if (matchingSession != null)
        {
            assignments[cse.fader] = new Assignment(matchingSession, cse.fader);
            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, cse.fader, true));
            SaveAssignments();
        }
    }
}

public void GetAssignmentInformation(ControlSurfaceEvent cse)
{
    if (assignments[cse.fader].session_alive)
    {
        Assignment assignment = assignments[cse.fader];
        if (assignment.aType == AssignmentType.Process)
            DumpProps(assignment.audioSession);

        if (assignment.CheckHealth())
            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, cse.fader, false));

    }
}

public void ChangeApplicationVolume(ControlSurfaceEvent cse)
{
    if (assignments[cse.fader].session_alive)
    {
        Assignment assignment = assignments[cse.fader];
        if (assignment.aType == AssignmentType.Process)
            assignment.audioSession.SimpleAudioVolume.Volume = cse.value;
        else if (assignment.aType == AssignmentType.Master)
            deviceVolume.MasterVolumeLevelScalar = cse.value;
    }
}

public void MuteApplication(ControlSurfaceEvent cse)
{
    bool muted = false;
    if (assignments[cse.fader].processName != null)
    {
        Assignment assignment = assignments[cse.fader];
        if (assignment.aType == AssignmentType.Process)
        {
            muted = !assignment.audioSession.SimpleAudioVolume.Mute;
            assignment.audioSession.SimpleAudioVolume.Mute = muted;
        }
        else if (assignment.aType == AssignmentType.Master)
        {
            muted = !deviceVolume.Mute;
            deviceVolume.Mute = muted;
        }
        NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.MuteState, cse.fader, muted));
    }
}
*/