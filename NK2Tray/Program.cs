using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;

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
        private AudioDeviceWatcher audioDeviceWatcher;

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

            _workerDispatcher.Invoke(SetupDevice);
        }

        private Boolean SetupDevice()
        {
            audioDeviceWatcher = new AudioDeviceWatcher();
            audioDevices = new AudioDevice();

            midiDevice = new NanoKontrol2(audioDevices);
            if (!midiDevice.Found)
                midiDevice = new XtouchMini(audioDevices);

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
            // Dont create context menu if no midi device is connected
            if(!midiDevice.Found)
            {
                if (!SetupDevice()) // This setup call can be removed once proper lifecycle management is implemented, for now this also adds a nice way to reconnect the controller
                {
                    MessageBox.Show("No midi device detected. Are you sure your device is plugged in correctly?");
                    return;
                }
            }

            ContextMenu trayMenu = (ContextMenu)sender;

            // Clean old menu items out
            var oldMenuItems = trayMenu.MenuItems.Cast<MenuItem>().ToArray();
            foreach (var item in oldMenuItems) item.Dispose();
            trayMenu.MenuItems.Clear();

            trayMenu.MenuItems.AddRange(midiDevice.faders.Select(fader =>
            {
                var faderItem = new MenuItem($@"Fader {fader.faderNumber + 1} - " + getProgramLabel(fader));

                // Add devices
                faderItem.MenuItems.AddRange(audioDeviceWatcher.Devices.Select(device =>
                    new MenuItem(audioDeviceWatcher.QuickDeviceNames[device], AssignFader){ Tag = new object[]{ fader, device }}
                ).ToArray());

                faderItem.MenuItems.Add("-");

                // Add Focus
                faderItem.MenuItems.Add(
                    new MenuItem("Focus")
                );

                faderItem.MenuItems.Add("-");

                // Add applications
                faderItem.MenuItems.AddRange(audioDeviceWatcher.Sessions.Select(pair =>
                    new MenuItem(pair.Value.First().DisplayName, AssignFader){ Tag = new object[] { fader, pair.Value }}
                ).ToArray());

                // Add unassign
                faderItem.MenuItems.Add(
                    new MenuItem("Unassign")
                );
                
                return faderItem;
            }).ToArray());

            trayMenu.MenuItems.Add("-");

            // Add toggle option for logarithmic volume curve
            var logCheck = new MenuItem("Logarithmic", ToggleLogarithmic);
            logCheck.Checked = logarithmic;
            trayMenu.MenuItems.Add(logCheck);

            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Exit", OnExit);
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
            midiDevice.ResetAllLights();
            Application.Exit();
            _workerDispatcher.InvokeShutdown();
        }

    }
}
