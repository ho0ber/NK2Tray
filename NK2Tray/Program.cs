using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NAudio.Midi;

namespace NK2Tray
{
    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private MidiIn midiIn;
        private int SpotifyPID;
        private int DiscordPID;

        public SysTrayApp()
        {
            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NK2 Tray";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
            BeginListening();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public void EnumerateDevices()
        {
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                Console.WriteLine("InDevice:" + device);
                Console.WriteLine(MidiIn.DeviceInfo(device).ProductName);
            }
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                Console.WriteLine("OutDevice:" + device);
                Console.WriteLine(MidiOut.DeviceInfo(device).ProductName);
            }
        }

        public void BeginListening()
        {
            midiIn = new MidiIn(0);
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
            SpotifyPID = GetSpotifyPID();
            DiscordPID = GetDiscordPID();
        }

        public void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        public void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            //Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
            //    e.Timestamp, e.RawMessage, e.MidiEvent));

            if (e.MidiEvent.CommandCode == MidiCommandCode.ControlChange)
            {
                ControlChangeEvent me = (ControlChangeEvent)e.MidiEvent;
                //Console.WriteLine(String.Format("Channel = {0}  CC = {1}  other = {2}", me.Channel, me.CommandCode, me.GetType()));
                if (me.Channel == 1 && (int)me.Controller == 7)
                {
                    //Console.WriteLine(String.Format("Found matching events: {0} {1}", (int)me.Controller, me.ControllerValue));
                    if (me.ControllerValue % 3 == 0 || me.ControllerValue == 127)
                        VolumeMixer.SetApplicationVolume(SpotifyPID, me.ControllerValue / 128f * 100f);
                }
                if (me.Channel == 1 && (int)me.Controller == 6)
                {
                    //Console.WriteLine(String.Format("Found matching events: {0} {1}", (int)me.Controller, me.ControllerValue));
                    if (me.ControllerValue % 3 == 0 || me.ControllerValue == 127)
                        VolumeMixer.SetApplicationVolume(DiscordPID, me.ControllerValue / 128f * 100f);
                }
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public int GetSpotifyPID()
        {
            var hWnd = FindWindow("Chrome_WidgetWin_0", "");
            if (hWnd == IntPtr.Zero)
                return 0;

            uint pID;
            GetWindowThreadProcessId(hWnd, out pID);
            if (pID == 0)
                return 0;

            Console.WriteLine(pID);
            return Convert.ToInt32(pID);
        }

        public int GetDiscordPID()
        {
            {
                var hWnd = FindWindow("Discord", "");
                if (hWnd == IntPtr.Zero)
                    return 0;

                uint pID;
                GetWindowThreadProcessId(hWnd, out pID);
                if (pID == 0)
                    return 0;

                Console.WriteLine(pID);
                return Convert.ToInt32(pID);
            }
            
        }
    }

}