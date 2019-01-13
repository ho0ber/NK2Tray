using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.Midi;
using System.Collections.Generic;

namespace NK2Tray
{
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

    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        private MidiIn midiIn;
        public Assignment[] assignments = new Assignment[8];

        public SysTrayApp()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NK2 Tray";
            trayIcon.Icon = new Icon(Properties.Resources.nk2tray, 40, 40);

            trayIcon.ContextMenu = new ContextMenu();
            trayIcon.ContextMenu.Popup += OnPopup;

            trayIcon.Visible = true;
            ListenForMidi();
        }

        private void OnPopup(object sender, EventArgs e)
        {
            ContextMenu trayMenu = (ContextMenu)sender;
            trayMenu.MenuItems.Clear();


            // Make all of the fader menu items
            foreach (var i in Enumerable.Range(0, 8))
            {
                MenuItem faderMenu = new MenuItem(String.Format("Fader {0} - {1}", i, assignments[i].process));
                trayMenu.MenuItems.Add(faderMenu);
            }

            // Add audio devices to each fader menu items
            IList<AudioSession> sessions = GetAndPruneAudioSessions();
            ISet<String> duplicates = FindDuplicates(sessions);

            foreach (AudioSession session in sessions)
            {
                if (session.Process != null)
                {
                    //DumpProps(session);
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        String sessionTitle;
                        if (duplicates.Contains(session.Process.ProcessName) && session.Process.MainWindowTitle.Length > 0)
                            sessionTitle = String.Format("{0} ({1})", session.Process.ProcessName, string.Concat(session.Process.MainWindowTitle.Take(15)));
                        else
                            sessionTitle = session.Process.ProcessName;

                        MenuItem si = new MenuItem(sessionTitle, AssignFader);
                        // Tag is unpacked in AssignFader - using this to plumb through
                        si.Tag = new object[] { session.Process.ProcessName, session.Process.MainWindowTitle, session.Process.Id,  i };
                        trayMenu.MenuItems[i].MenuItems.Add(si);
                    }

                }
            }

            // Add the exit at the end
            trayMenu.MenuItems.Add("Exit", OnExit);
        }

        private void DumpProps(AudioSession session)
        {
            Console.WriteLine("=================================");
            Console.WriteLine("DisplayName = " + session.DisplayName);
            Console.WriteLine("GroupingParam = " + session.GroupingParam);
            Console.WriteLine("IconPath = " + session.IconPath);
            Console.WriteLine("Identifier = " + session.Identifier);
            Console.WriteLine("InstanceIdentifier = " + session.InstanceIdentifier);
            Console.WriteLine("ProcessName = " + session.Process.ProcessName);
            Console.WriteLine("Process.SessionId = " + session.Process.SessionId);
            Console.WriteLine("ProcessId = " + session.ProcessId);
            Console.WriteLine("State = " + session.State);
        }

        private IList<AudioSession> GetAndPruneAudioSessions()
        {
            List<AudioSession> sessions = (List <AudioSession>)AudioUtilities.GetAllSessions();
            sessions.RemoveAll(listitem => (listitem.Process == null));
            return sessions.OrderBy(o => o.Process.ProcessName).ToList();
        }

        private ISet<String> FindDuplicates(IList<AudioSession> sessions)
        {
            ISet<String> seen = new HashSet<String>();
            ISet<String> duplicates = new HashSet<String>();

            foreach (AudioSession session in sessions)
            {
                if (session.Process != null)
                {
                    if (seen.Contains(session.Process.ProcessName))
                        duplicates.Add(session.Process.ProcessName);
                    else
                        seen.Add(session.Process.ProcessName);
                }
            }

            return duplicates;
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

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e) => Application.Exit();

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

        public void ListenForMidi()
        {
            midiIn = new MidiIn(0);
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
        }

        public void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        public void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode == MidiCommandCode.ControlChange)
            {
                ControlChangeEvent me = (ControlChangeEvent)e.MidiEvent;

                if (me.Channel == 1)
                {
                    //Console.Write("Fader event; ");
                    int fader = (int)me.Controller;
                    if (assignments[fader].process != null)
                    {
                        //Console.Write("Got assignment " + fader + "; " + assignments[fader].windowName + "; ");

                        VolumeMixer.SetApplicationVolume(assignments[fader].pid, me.ControllerValue / 128f * 100f);
                        //Console.Write("changed vol");
                    }
                    //Console.WriteLine();
                }
            }
        }
    }
}