using NAudio.Midi;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public class NanoKontrol2 : MidiDevice
    {
        public override string SearchString => "nano";
        public override FaderDef DefaultFaderDef => new FaderDef(
            false, // delta
            127f,   // range
            1,     // channel
            true,  // selectPresent
            true,  // mutePresent
            true,  // recordPresent
            0,     // faderOffset
            32,    // selectOffset
            48,    // muteOffset
            64,    // recordOffset
            MidiCommandCode.ControlChange, // faderCode
            MidiCommandCode.ControlChange, // selectCode
            MidiCommandCode.ControlChange, // muteCode
            MidiCommandCode.ControlChange // recordCode
        );

        public NanoKontrol2(AudioDevice audioDev)
        {
            audioDevices = audioDev;
            FindMidiIn();
            FindMidiOut();
            if (Found)
            {
                ResetAllLights();
                InitFaders();
                InitButtons();
                LightShow();
                LoadAssignments();
                ListenForMidi();
            }
        }

        public override void ResetAllLights()
        {
            foreach (var i in Enumerable.Range(0, 128))
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());
        }

        public override void SetLight(int controller, bool state)
        {
            midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)(controller), state ? 127 : 0).GetAsShortMessage());
        }

        public override void InitFaders()
        {
            faders = new List<Fader>();
            foreach (var i in Enumerable.Range(0, 8))
            {
                Fader fader = new Fader(this, i);
                fader.ResetLights();
                faders.Add(fader);
            }
        }

        public override void InitButtons()
        {
            buttons = new List<Button>();
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPrevious, 43, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaNext,     44, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaStop,     42, false));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaPlay,     41, true));
            buttons.Add(new Button(ref midiOut,  ButtonType.MediaRecord,   45, false));

            buttonsMappingTable = new Hashtable();
            foreach (var button in buttons)
            {
                buttonsMappingTable.Add(button.controller, button);
            }
        }

        public override void LightShow()
        {
            List<LightShowBackup> lightBackups = new List<LightShowBackup>();

            //backup settings and turn them all off
            foreach (var fader in faders)
            {
                lightBackups.Add(new LightShowBackup(fader));
                fader.SetSelectLight(false);
                fader.SetSelectLight(false);
                fader.SetMuteLight(false);
                fader.SetRecordLight(false);
            }
            foreach (var button in buttons)
            {
                lightBackups.Add(new LightShowBackup(button));
                button.SetLight(false);
            }

            //do light show
            int travelSpeed = 50;
            foreach (var button in buttons)
            {
                button.SetLight(true);
                System.Threading.Thread.Sleep(travelSpeed);
                button.SetLight(false);
            }
            foreach (var fader in faders)
            {
                fader.SetSelectLight(true);
                fader.SetMuteLight(true);
                fader.SetRecordLight(true);
                System.Threading.Thread.Sleep(travelSpeed);
                fader.SetSelectLight(false);
                fader.SetMuteLight(false);
                fader.SetRecordLight(false);
            }
            //reverse
            for (int i = faders.Count - 1; i >= 0; i--)
            {
                var fader = faders[i];
                fader.SetSelectLight(true);
                fader.SetMuteLight(true);
                fader.SetRecordLight(true);
                System.Threading.Thread.Sleep(travelSpeed);
            }
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                var button = buttons[i];
                button.SetLight(true);
                System.Threading.Thread.Sleep(travelSpeed);
            }
            //flash
            /*
            for (int i = 2; i > 0; i--)
            {
                System.Threading.Thread.Sleep(travelSpeed * 2);

                if (i != 3)
                {
                    foreach (var button in buttons)
                    {
                        button.SetLight(true);
                    }
                    foreach (var fader in faders)
                    {
                        fader.SetSelectLight(true);
                        fader.SetMuteLight(true);
                        fader.SetRecordLight(true);
                    }
                }

                System.Threading.Thread.Sleep(travelSpeed * 2);

                foreach (var button in buttons)
                {
                    button.SetLight(false);
                }
                foreach (var fader in faders)
                {
                    fader.SetSelectLight(false);
                    fader.SetMuteLight(false);
                    fader.SetRecordLight(false);
                }
            }

            foreach (var button in buttons)
            {
                button.SetLight(true);
            }
            */

            //reset settings
            foreach (var lightBackup in lightBackups)
            {
                lightBackup.reset();
            }

        }

        private class LightShowBackup
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
        }
    }
}
