using NAudio.Midi;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class NanoKontrol2 : MidiDeviceTemplate
    {
        public override bool hasLights => true;
        public override int lightCount => 128;
        public override MidiCommandCode lightMessageType => MidiCommandCode.ControlChange;
        public override bool hasVolumeIndicator => false;

        public override int fadersCount => 8;

        public override Button[] buttons => new Button[]
        {
            new Button(midiDevice, ButtonType.MediaPrevious, 43, true),
            new Button(midiDevice, ButtonType.MediaNext,     44, true),
            new Button(midiDevice, ButtonType.MediaStop,     42, false),
            new Button(midiDevice, ButtonType.MediaPlay,     41, true),
            new Button(midiDevice, ButtonType.MediaRecord,   45, false)
        };

        //public override string SearchString => "nano";
        public override FaderDef[] faderDefs => new FaderDef[]{
            new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            true,  // selectPresent
            true,  // mutePresent
            true,  // recordPresent
            true,  // subFaderPresent
            0,     // faderOffset
            32,    // selectOffset
            48,    // muteOffset
            64,    // recordOffset
            16,    // subFaderOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange, // recordCode
            MidiCommandCode.ControlChange  // subFaderCode
            )
        };

        public NanoKontrol2(MidiDevice midiDev) : base(midiDev)
        {
            
        }

        public override void LightShow(ref List<Fader> faders)
        {
            //List<LightShowBackup> lightBackups = new List<LightShowBackup>();

            //backup settings and turn them all off
            foreach (var fader in faders)
            {
                //lightBackups.Add(new LightShowBackup(fader));
                fader.SetLightTemp(false);
            }
            foreach (var button in buttons)
            {
                //lightBackups.Add(new LightShowBackup(button));
                button.SetLightTemp(false);
            }

            //do light show
            int travelSpeed = 50;
            foreach (var button in buttons)
            {
                button.SetLightTemp(true);
                System.Threading.Thread.Sleep(travelSpeed);
                button.SetLightTemp(false);
            }
            foreach (var fader in faders)
            {
                fader.SetLightTemp(true);
                System.Threading.Thread.Sleep(travelSpeed);
                fader.SetLightTemp(false);
            }
            //reverse
            for (int i = faders.Count - 1; i >= 0; i--)
            {
                var fader = faders[i];
                fader.SetLightTemp(true);
                System.Threading.Thread.Sleep(travelSpeed);
            }
            for (int i = buttons.Count() - 1; i >= 0; i--)
            {
                var button = buttons[i];
                button.SetLightTemp(true);
                System.Threading.Thread.Sleep(travelSpeed);
            }

            //reset settings
            /*foreach (var lightBackup in lightBackups)
            {
                lightBackup.reset();
            }*/
            foreach(var fader in faders)
            {
                fader.refreshLights();
            }
            foreach(var button in buttons)
            {
                button.refreshLight();
            }

        }

        /*private class LightShowBackup
        {
            private bool[] lightSettings;
            private Fader fader = null;
            private Button button = null;

            public LightShowBackup(Fader inFader)
            {
                lightSettings = new bool[3];
                lightSettings[0] = inFader.GetSelectLight();
                lightSettings[1] = inFader.GetMuteLight();
                lightSettings[2] = inFader.GetRecordLight();
                fader = inFader;
            }

            public LightShowBackup(Button inButton)
            {
                lightSettings = new bool[1];
                lightSettings[0] = inButton.GetLight();
                button = inButton;
            }

            public void reset()
            {
                if (fader != null)
                {
                    fader.SetSelectLight(lightSettings[0]);
                    fader.SetMuteLight(lightSettings[1]);
                    fader.SetRecordLight(lightSettings[2]);
                }
                else if (button != null)
                {
                    button.SetLight(lightSettings[0]);
                }
            }
        }*/
    }
}
