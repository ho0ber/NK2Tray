using System;
using System.Drawing;
using System.Linq;
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

        public struct Assignment
        {
            public String process;
            public String windowName;
            public int pid;

            public Assignment(String p, String wn, int inPid)
            {
                process = p;
                windowName = wn;
                pid = inPid;
            }
        }

        public Assignment[] assignments = new Assignment[8];

        public SysTrayApp()
        {
            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NK2 Tray";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = new ContextMenu();
            trayIcon.ContextMenu.Popup += OnPopup; // (sender, args) => Console.WriteLine("Opening #2");
            //trayIcon.Click += OnClick;
            trayIcon.Visible = true;
            BeginListening();
        }

        private void OnPopup(object sender, EventArgs e)
        {
            ContextMenu trayMenu = (ContextMenu)sender;
            trayMenu.MenuItems.Clear();
            trayMenu.MenuItems.Add("Exit", OnExit);

            foreach (var i in Enumerable.Range(0, 8))
            {
                MenuItem faderMenu = new MenuItem(String.Format("Fader {0} - {1}", i, assignments[i].process));
                trayMenu.MenuItems.Add(faderMenu);
            }

            foreach (AudioSession session in AudioUtilities.GetAllSessions())
            {
                if (session.Process != null)
                {
                    // only the one associated with a defined process
                    Console.WriteLine(session.Process.ProcessName);
                    Console.WriteLine(session.Process.MainWindowTitle);
                    Console.WriteLine("");
                    //String.Format("{0} - {1}", session.Process.ProcessName, session.Process.MainWindowTitle)
                    //MenuItem mi = new MenuItem(session.Process.ProcessName);
                    //mi.MenuItems.Add(session.Process.MainWindowTitle);
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        MenuItem si = new MenuItem(session.Process.ProcessName, AssignFader);
                        si.Tag = new object[] { session.Process.ProcessName, session.Process.MainWindowTitle, session.Process.Id,  i };
                        trayMenu.MenuItems[i+1].MenuItems.Add(si);
                    }

                }
            }
            //Console.WriteLine(e);
            
        }

        private void AssignFader(object sender, EventArgs e)
        {
            String proc = (String)((object[])((MenuItem)sender).Tag)[0];
            String name = (String)((object[])((MenuItem)sender).Tag)[1];
            int pid = (int)((object[])((MenuItem)sender).Tag)[2];
            int fader = (int)((object[])((MenuItem)sender).Tag)[3];
            Console.WriteLine(String.Format("Assigning fader {0} to {1} - {2}", fader, proc, name));
            assignments[fader] = new Assignment(proc, name, pid);
        }

        public ContextMenu BuildMenu()
        {
            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Exit", OnExit);

            foreach (AudioSession session in AudioUtilities.GetAllSessions())
            {
                if (session.Process != null)
                {
                    // only the one associated with a defined process
                    Console.WriteLine(session.Process.ProcessName);
                    Console.WriteLine(session.Process.MainWindowTitle);
                    Console.WriteLine("");
                    trayMenu.MenuItems.Add(String.Format("{0} - {1}", session.Process.ProcessName, session.Process.MainWindowTitle), OnExit);
                }
            }


            return trayMenu;
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
                /*
                //Console.WriteLine(String.Format("Channel = {0}  CC = {1}  other = {2}", me.Channel, me.CommandCode, me.GetType()));
                if (me.Channel == 1 && (int)me.Controller == 0)
                {
                    Console.WriteLine(String.Format("Found matching events: {0} {1}", (int)me.Controller, me.ControllerValue));
                    if (me.ControllerValue % 3 == 0 || me.ControllerValue == 127)
                        VolumeMixer.SetApplicationVolume(GetForegroundPID(), me.ControllerValue / 128f * 100f);
                }
                else if (me.Channel == 1 && (int)me.Controller == 7)
                {
                    //Console.WriteLine(String.Format("Found matching events: {0} {1}", (int)me.Controller, me.ControllerValue));
                    if (me.ControllerValue % 3 == 0 || me.ControllerValue == 127)
                        VolumeMixer.SetApplicationVolume(SpotifyPID, me.ControllerValue / 128f * 100f);
                }
                else if (me.Channel == 1 && (int)me.Controller == 6)
                {
                    //Console.WriteLine(String.Format("Found matching events: {0} {1}", (int)me.Controller, me.ControllerValue));
                    if (me.ControllerValue % 3 == 0 || me.ControllerValue == 127)
                        VolumeMixer.SetApplicationVolume(DiscordPID, me.ControllerValue / 128f * 100f);
                }
                */
                if (me.Channel == 1)
                {
                    Console.Write("Fader event; ");
                    int fader = (int)me.Controller;
                    if (assignments[fader].process != null)
                    {
                        Console.Write("Got assignment " + fader + "; " + assignments[fader].windowName + "; ");
                        //int pid = GetPidByName(assignments[fader].windowName);
                        if (true) //(me.ControllerValue % 3 == 0 || me.ControllerValue == 127)
                        {
                            VolumeMixer.SetApplicationVolume(assignments[fader].pid, me.ControllerValue / 128f * 100f);
                            Console.Write("changed vol");
                        }
                    }
                    Console.WriteLine();
                }
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public int GetPidByName(string name)
        {
            {
                var hWnd = FindWindow(name, "");
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

        public int GetForegroundPID()
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return 0;

            uint pID;
            GetWindowThreadProcessId(hWnd, out pID);
            if (pID == 0)
                return 0;

            return Convert.ToInt32(pID);
        }

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