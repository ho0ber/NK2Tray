﻿using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using NAudio;
using NAudio.Midi;

namespace NK2Tray
{
    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        public MidiDevice midiDevice;
        public AudioDevice audioDevices;
        public bool logarithmic;

        private Dispatcher _workerDispatcher;
        private Thread _workerThread;

        public string midiInName;
        public string midiOutName;

        public ControllerType controllerType;

        public SysTrayApp()
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            Console.WriteLine($@"NK2 Tray {DateTime.Now}");

            // Set up a worker thread to always run SetupDevice and PopUp inside of
            _workerThread = new Thread(new ThreadStart(() =>
            {
                _workerDispatcher = Dispatcher.CurrentDispatcher;
                Dispatcher.Run();
            }));

            _workerThread.Start();

            trayIcon = new NotifyIcon
            {
                Text = "NK2 Tray",
                Icon = new Icon(Properties.Resources.nk2tray, 40, 40),
                ContextMenu = new ContextMenu()
            };

            trayIcon.ContextMenu.Popup += (object sender, EventArgs e) =>
                _workerDispatcher.Invoke(() => OnPopup(sender, e));

            trayIcon.Visible = true; 
            
            while (_workerDispatcher == null)
            {

                /*
                 * weird threading situation.
                 * _workerDispatcher = Dispatcher.CurrentDispatcher; and Dispatcher.Run(); must be set in another thread otherwise the gui doesn't work or the program doesn't exit cleanly.
                 * but on faster machines we race to Invoke(SetupDevice); before the other thread has instantiated the _workerDispatcher
                 * easy way to solve is to just wait here until _workerDispatcher is set 
                 */

                Thread.Sleep(10);
            }

            _workerDispatcher.Invoke(SetupDevice);
        }

        private Boolean SetupDevice()
        {
            audioDevices = new AudioDevice();

            /*midiDevice = new NanoKontrol2(audioDevices);

            if (!midiDevice.Found)
                midiDevice = new XtouchMini(audioDevices);

            if (!midiDevice.Found)
                midiDevice = new OP1(audioDevices);

             if (!midiDevice.Found)
               midiDevice = new EasyControl(audioDevices);*/

            midiInName = ConfigSaver.GetAppSettings("midiin");
            midiOutName = ConfigSaver.GetAppSettings("midiout");
            try
            {
                controllerType = (ControllerType)Enum.Parse(typeof(ControllerType), ConfigSaver.GetAppSettings("controllertype"), true);
            }
            catch (Exception)
            {
                controllerType = ControllerType.NanoKontrol2;
            }
            
            if (midiInName == null) midiInName = "nanoKONTROL2";
            if (midiOutName == null) midiOutName = "nanoKONTROL2";

            if (midiDevice != null) midiDevice.Close();
            midiDevice = new MidiDevice(audioDevices, controllerType, midiInName, midiOutName);


            audioDevices.midiDevice = midiDevice;

            logarithmic = System.Convert.ToBoolean(ConfigSaver.GetAppSettings("logarithmic"));
            SaveLogarithmic();

            return midiDevice.Found;
        }

        private void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            MessageBox.Show(e.ExceptionObject.ToString(), "NK2 Tray Error", MessageBoxButtons.OK);

            if (midiDevice != null)
            {
                try
                {
                    midiDevice.ResetAllLights();
                    midiDevice.faders.Last().SetRecordLight(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            Application.Exit();
        }

        private String getProgramLabel(Fader fader)
        {
            if (fader.assigned)
                return fader.assignment.label;
            
            if(fader.identifier != null && fader.identifier.Length > 0 && fader.identifier.Contains(".exe"))
            {
                String identifier = fader.identifier;
                int lastBackSlash = identifier.LastIndexOf('\\') + 1;
                int programNameLength = identifier.IndexOf(".exe") - lastBackSlash;
                String progamName = identifier.Substring(lastBackSlash, programNameLength);
                return progamName;
            }
            return "";
        }

        private void OnPopup(object sender, EventArgs e)
        {
            ContextMenu trayMenu = (ContextMenu)sender;

            // Clean old menu items out
            var oldMenuItems = trayMenu.MenuItems.Cast<MenuItem>().ToArray();
            foreach (var item in oldMenuItems)
                item.Dispose();

            trayMenu.MenuItems.Clear();

            var mixerSessions = audioDevices.GetCachedMixerSessions();

            var masterMixerSessionList = new List<MixerSession>();
            foreach(MMDevice mmDevice in audioDevices.outputDevices)
            {
                masterMixerSessionList.Add(new MixerSession(mmDevice.ID, audioDevices, "Master", SessionType.Master));
            }

            /*
            // Commented out Iput device code because it breaks the session lists when input and ouput is controlled at the same time
            var micMixerSessionList = new List<MixerSession>();
            foreach (MMDevice mmDevice in audioDevices.inputDevices)
            {
                micMixerSessionList.Add(new MixerSession(mmDevice.ID, audioDevices, "Microphone", SessionType.Master));
            }
            */

            MixerSession focusMixerSession;
            focusMixerSession = new MixerSession("", audioDevices, "Focus", SessionType.Focus);

            // Dont create context menu if no midi device is connected
            /*if(!midiDevice.Found)
            {
                if (!SetupDevice()) // This setup call can be removed once proper lifecycle management is implemented, for now this also adds a nice way to reconnect the controller
                {
                    MessageBox.Show("No midi device detected. Are you sure your device is plugged in correctly?");
                    return;
                }
            }*/
            if (midiDevice.Found)
            {

                foreach (var fader in midiDevice.faders)
                {
                    MenuItem faderMenu = new MenuItem($@"Fader {fader.faderNumber + 1} - " + getProgramLabel(fader));
                    trayMenu.MenuItems.Add(faderMenu);

                    // Add master mixerSession to menu
                    foreach (MixerSession mixerSession in masterMixerSessionList)
                    {
                        MenuItem masterItem = new MenuItem(mixerSession.label, AssignFader);
                        masterItem.Tag = new object[] { fader, mixerSession };
                        faderMenu.MenuItems.Add(masterItem);
                    }

                    /* 
                    // Commented out Iput device code because it breaks the session lists when input and ouput is controlled at the same time
                    faderMenu.MenuItems.Add("-");

                    foreach (MixerSession mixerSession in micMixerSessionList)
                    {
                        MenuItem micItem = new MenuItem(mixerSession.label, AssignFader);
                        micItem.Tag = new object[] { fader, mixerSession };
                        faderMenu.MenuItems.Add(micItem);
                    }
                    */

                    faderMenu.MenuItems.Add("-");

                    // Add focus mixerSession to menu                
                    MenuItem focusItem = new MenuItem(focusMixerSession.label, AssignFader);
                    focusItem.Tag = new object[] { fader, focusMixerSession };
                    faderMenu.MenuItems.Add(focusItem);

                    faderMenu.MenuItems.Add("-");

                    // Add application mixer sessions to each fader
                    foreach (var mixerSession in mixerSessions)
                    {
                        MenuItem si = new MenuItem(mixerSession.label, AssignFader);
                        si.Tag = new object[] { fader, mixerSession };
                        faderMenu.MenuItems.Add(si);
                    }

                    faderMenu.MenuItems.Add("-");

                    // Add unassign option
                    MenuItem unassignItem = new MenuItem("Unassign", UnassignFader);
                    unassignItem.Tag = new object[] { fader };
                    faderMenu.MenuItems.Add(unassignItem);
                }

                trayMenu.MenuItems.Add("-");
            }

            // Add toggle option for logarithmic volume curve
            MenuItem logCheck = new MenuItem("Logarithmic", ToggleLogarithmic);
            logCheck.Checked = logarithmic;
            trayMenu.MenuItems.Add(logCheck);

            trayMenu.MenuItems.Add("-");
            MenuItem midiInDevs = trayMenu.MenuItems.Add("Midi input device");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                string name = MidiIn.DeviceInfo(i).ProductName;
                MenuItem item = midiInDevs.MenuItems.Add(name, SetMidiInputDevice);
                if(name == midiInName)
                {
                    item.Checked = true;
                }
            }

            MenuItem midiOutDevs = trayMenu.MenuItems.Add("Midi output device");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                string name = MidiOut.DeviceInfo(i).ProductName;
                MenuItem item = midiOutDevs.MenuItems.Add(name, SetMidiOutputDevice);
                if (name == midiOutName)
                {
                    item.Checked = true;
                }
            }

            MenuItem deviceTypeMenu = trayMenu.MenuItems.Add("Device type");
            foreach(ControllerType ctType in (ControllerType[]) Enum.GetValues(typeof(ControllerType))){
                string name = ctType.ToString();
                MenuItem item = deviceTypeMenu.MenuItems.Add(name, SetDeviceType);
                if(ctType == controllerType)
                {
                    item.Checked = true;
                }
            }


            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", OnExit);
        }

        private void SetDeviceType(object sender, EventArgs e)
        {
            ConfigSaver.AddOrUpdateAppSettings("controllertype", ((MenuItem)sender).Text);
            _workerDispatcher.Invoke(SetupDevice);
        }

        private void ToggleLogarithmic(object sender, EventArgs e)
        {
            logarithmic = !logarithmic;
            ConfigSaver.AddOrUpdateAppSettings("logarithmic", System.Convert.ToString(logarithmic));
            SaveLogarithmic();
        }

        private void SaveLogarithmic()
        {
            midiDevice.SetCurve(logarithmic ? 2f : 1f);
        }

        private void SetMidiInputDevice(object sender, EventArgs e)
        {
            //((MenuItem)sender).Checked = true;
            midiInName = ((MenuItem)sender).Text;
            ConfigSaver.AddOrUpdateAppSettings("midiin", midiInName);
            midiDevice.FindMidiIn(midiInName);
        }

        private void SetMidiOutputDevice(object sender, EventArgs e)
        {
            midiOutName = ((MenuItem)sender).Text;
            ConfigSaver.AddOrUpdateAppSettings("midiout", midiOutName);
            midiDevice.FindMidiOut(midiOutName);
        }

        private void AssignFader(object sender, EventArgs e)
        {
            var fader = (Fader)((object[])((MenuItem)sender).Tag)[0];
            var mixerSession = (MixerSession)((object[])((MenuItem)sender).Tag)[1];
            fader.Assign(mixerSession);
            midiDevice.SaveAssignments();
        }

        private void UnassignFader(object sender, EventArgs e)
        {
            var fader = (Fader)((object[])((MenuItem)sender).Tag)[0];
            fader.Unassign();
            midiDevice.SaveAssignments();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            try
            {
                midiDevice.ResetAllLights();
                midiDevice.SaveAssignments();
            }
            catch (MmException ex)
            {
                if (ex.Message.StartsWith("InvalidHandle calling midiOutShortMsg"))
                {
                    // Exit program safely if device was already disconnected
                }
            }

            Application.Exit();
            _workerDispatcher.InvokeShutdown();
        }

    }
}
