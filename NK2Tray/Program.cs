using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.Midi;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using System.Diagnostics;

namespace NK2Tray
{
    public enum AssignmentType
    {
        Process,
        Master
    }

    public struct Assignment
    {
        public String processName;
        public String windowName;
        public int pid;
        public String sessionIdentifier;
        public String instanceIdentifier;
        public AssignmentType aType;
        public AudioSessionControl audioSession;


        public Assignment(String p, String wn, int inPid, AssignmentType at, String sid, String iid, AudioSessionControl audsess)
        {
            processName = p;
            windowName = wn;
            pid = inPid;
            aType = at;
            sessionIdentifier = sid;
            instanceIdentifier = iid;
            audioSession = audsess;
        }
    }

    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        private MidiIn midiIn;
        public Assignment[] assignments = new Assignment[8];
        private MMDevice device;
        //private readonly AudioSessionControl session;

        public SysTrayApp()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NK2 Tray";
            trayIcon.Icon = new Icon(Properties.Resources.nk2tray, 40, 40);

            trayIcon.ContextMenu = new ContextMenu();
            trayIcon.ContextMenu.Popup += OnPopup;

            trayIcon.Visible = true;
            ListenForMidi();

            
            // dump all audio devices
            //Console.WriteLine("Current Volume:" + defaultPlaybackDevice.Volume);
            //VolTest();
        }

        private bool ProcessExists(uint processId)
        {
            try
            {
                var process = Process.GetProcessById((int)processId);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private void DumpProps(AudioSessionControl session)
        {
            Process process = Process.GetProcessById((int)session.GetProcessID);
            Console.WriteLine("=================================");
            Console.WriteLine(String.Format("DisplayName = {0}", session.DisplayName));
            Console.WriteLine(String.Format("AudioMeterInformation = {0}", session.AudioMeterInformation.PeakValues));
            Console.WriteLine(String.Format("IconPath = {0}", session.IconPath));
            Console.WriteLine(String.Format("GetSessionIdentifier = {0}", session.GetSessionIdentifier));
            Console.WriteLine(String.Format("GetSessionInstanceIdentifier = {0}", session.GetSessionInstanceIdentifier));
            Console.WriteLine(String.Format("IsSystemSoundsSession = {0}", session.IsSystemSoundsSession));
            Console.WriteLine(String.Format("SimpleAudioVolume = {0}", session.SimpleAudioVolume.Volume));
            Console.WriteLine(String.Format("GetProcessID = {0}", session.GetProcessID));
            Console.WriteLine(String.Format("State = {0}", session.State));

            Console.WriteLine(String.Format("lbl = {0}", process.MainWindowTitle != "" ? process.MainWindowTitle : process.ProcessName));

        }

        private void OnPopup(object sender, EventArgs e)
        {
            ContextMenu trayMenu = (ContextMenu)sender;
            trayMenu.MenuItems.Clear();


            // Make all of the fader menu items
            foreach (var i in Enumerable.Range(0, 8))
            {
                MenuItem faderMenu = new MenuItem(String.Format("Fader {0} - {1}", i, assignments[i].processName));
                trayMenu.MenuItems.Add(faderMenu);
            }

            // Add audio devices to each fader menu items
            var deviceEnumerator = new MMDeviceEnumerator();
            device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            GenerateDeviceMenuItems(ref trayMenu);

            var sessions = device.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                //AppVolumePanels = new List<VolumePanel>(sessions.Count);
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (session.IsSystemSoundsSession && ProcessExists(session.GetProcessID))
                    {
                        //Console.WriteLine(session.DisplayName);
                        //AddVolumePanel(session);
                        DumpProps(session);
                        GenerateMenuItems(ref trayMenu, session);
                        break;
                    }
                }
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (!session.IsSystemSoundsSession && ProcessExists(session.GetProcessID))
                    {
                        //AddVolumePanel(session);
                        //Console.WriteLine(session.DisplayName);
                        DumpProps(session);
                        GenerateMenuItems(ref trayMenu, session);
                    }

                }
            }
        }

        private void GenerateMenuItems(ref ContextMenu trayMenu, AudioSessionControl session)
        {
            Process process = Process.GetProcessById((int)session.GetProcessID);
            foreach (var i in Enumerable.Range(0, 8))
            {
                String sessionTitle;
                if (session.IsSystemSoundsSession)
                    sessionTitle = "System Sounds";
                else if (process.MainWindowTitle == "")
                    sessionTitle = process.ProcessName;
                else
                    sessionTitle = process.MainWindowTitle;

                MenuItem si = new MenuItem(sessionTitle, AssignFader);
                // Tag is unpacked in AssignFader - using this to plumb through
                si.Tag = new object[] { process.ProcessName, process.MainWindowTitle, (int)session.GetProcessID, i, session.GetSessionIdentifier, session.GetSessionInstanceIdentifier, session };
                trayMenu.MenuItems[i].MenuItems.Add(si);
            }
        }

        private void GenerateDeviceMenuItems(ref ContextMenu trayMenu)
        {
            foreach (var i in Enumerable.Range(0, 8))
            {
                MenuItem si = new MenuItem("Master Volume", AssignFader);
                // Tag is unpacked in AssignFader - using this to plumb through
                si.Tag = new object[] { process.ProcessName, process.MainWindowTitle, (int)session.GetProcessID, i, session.GetSessionIdentifier, session.GetSessionInstanceIdentifier, session };
                trayMenu.MenuItems[i].MenuItems.Add(si);
            }
        }

        /*
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
                    DumpProps(session);
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

            foreach (var i in Enumerable.Range(0, 8))
            {
                MenuItem si = new MenuItem("Master Volume", AssignFader);
                si.Tag = new object[] { "Master Volume", "", -1, i };
                trayMenu.MenuItems[i].MenuItems.Add(si);
            }
            // Add the exit at the end
            trayMenu.MenuItems.Add("Exit", OnExit);
        }

        private void DumpProps(AudioSession session)
        {
            Console.WriteLine("=================================");
            Console.WriteLine(String.Format("DisplayName = {0}", session.DisplayName));
            Console.WriteLine(String.Format("GroupingParam = {0}", session.GroupingParam));
            Console.WriteLine(String.Format("IconPath = {0}", session.IconPath));
            Console.WriteLine(String.Format("Identifier = {0}", session.Identifier));
            Console.WriteLine(String.Format("InstanceIdentifier = {0}", session.InstanceIdentifier));
            Console.WriteLine(String.Format("ProcessName = {0}", session.Process.ProcessName));
            Console.WriteLine(String.Format("Process.SessionId = {0}", session.Process.SessionId));
            Console.WriteLine(String.Format("ProcessId = {0}", session.ProcessId));
            Console.WriteLine(String.Format("State = {0}", session.State));
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
        */
        private void AssignFader(object sender, EventArgs e)
        {
            String proc = (String)((object[])((MenuItem)sender).Tag)[0];
            String name = (String)((object[])((MenuItem)sender).Tag)[1];
            int pid = (int)((object[])((MenuItem)sender).Tag)[2];
            int fader = (int)((object[])((MenuItem)sender).Tag)[3];
            AssignmentType aType = pid >= 0 ? AssignmentType.Process : AssignmentType.Master;
            String sid = (String)((object[])((MenuItem)sender).Tag)[4];
            String iid = (String)((object[])((MenuItem)sender).Tag)[5];
            AudioSessionControl audsess = (AudioSessionControl)((object[])((MenuItem)sender).Tag)[6];

            Console.WriteLine(String.Format("Assigning fader {0} to {1} - {2}", fader, proc, name));
            assignments[fader] = new Assignment(proc, name, pid, aType, sid, iid, audsess);
        }
        
        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e) => Application.Exit();
        /*
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
        */

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
                    if (fader < 8 && assignments[fader].processName != null)
                    {
                        Assignment assignment = assignments[fader];
                        //Console.Write("Got assignment " + fader + "; " + assignments[fader].windowName + "; ");
                        if (assignment.aType == AssignmentType.Process)
                            //VolumeMixer.SetApplicationVolume(assignments[fader].pid, me.ControllerValue / 127f * 100f);
                            assignment.audioSession.SimpleAudioVolume.Volume = me.ControllerValue / 127f;
                        else if (assignment.aType == AssignmentType.Master)
                        {
                            float vol = me.ControllerValue / 127f * 100f;
                            Console.WriteLine(String.Format("Setting master volume to: {0} {1}", vol, me.ControllerValue));
                            //SetMasterVol(vol);
                            //defaultPlaybackDevice.Volume = Math.Floor(me.ControllerValue / 128f * 100f);
                            //Console.Write("changed vol");
                        }
                    }
                    //Console.WriteLine();
                }
            }
        }
        
    }
}