using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{

    public class MidiDeviceTemplate
    {
        public virtual bool hasLights => false;
        public virtual int lightCount => 0;
        public virtual MidiCommandCode lightMessageType => MidiCommandCode.ControlChange;
        public virtual bool hasVolumeIndicator => false;

        public virtual int fadersCount => 0;

        public virtual Button[] buttons => new Button[0];
        public virtual FaderDef[] faderDefs => new FaderDef[0];

        public MidiDevice midiDevice;


        public MidiDeviceTemplate(MidiDevice midiDev)
        {
            midiDevice = midiDev;
        }

        public virtual void LightShow(ref List<Fader> faders){}

        public virtual void ResetSuppLights(MidiOut midiOut){}

        public virtual int SelectFaderDef(int fader)
        {
            return 0;
        }
    }
}
