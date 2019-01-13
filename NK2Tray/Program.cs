using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.Midi;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace NK2Tray
{
    public enum AssignmentType
    {
        Process,
        Master
    }

    public struct Assignment
    {
        public String process;
        public String windowName;
        public int pid;
        public AssignmentType aType;


        public Assignment(String p, String wn, int inPid, AssignmentType at)
        {
            process = p;
            windowName = wn;
            pid = inPid;
            aType = at;
        }
    }

    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        private MidiIn midiIn;
        public Assignment[] assignments = new Assignment[8];
        //CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
        private MMDevice dev;


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

        private void SetMasterVol(float vol)
        {
            //try
            //{

            //Instantiate an Enumerator to find audio devices
            NAudio.CoreAudioApi.MMDeviceEnumerator MMDE = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            //Get all the devices, no matter what condition or status
            //NAudio.CoreAudioApi.MMDeviceCollection DevCol = MMDE.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.All, NAudio.CoreAudioApi.DeviceState.All);

            dev = MMDE.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            //Set at maximum volume
            dev.AudioEndpointVolume.MasterVolumeLevel = (vol*0.96f) - 96;

            //Get its audio volume
            System.Diagnostics.Debug.Print("Volume of " + dev.FriendlyName + " is " + dev.AudioEndpointVolume.MasterVolumeLevel.ToString());

            //Mute it
            //mastervol.AudioEndpointVolume.Mute = true;
            //System.Diagnostics.Debug.Print(mastervol.FriendlyName + " is muted");

            //}
            //catch (Exception ex)
            //{
            //    //Do something with exception when an audio endpoint could not be muted
            //    System.Diagnostics.Debug.Print(dev.FriendlyName + " could not be changed");
            //}
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
                si.Tag = new object[] { "", "", -1, i };
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

        private void AssignFader(object sender, EventArgs e)
        {
            String proc = (String)((object[])((MenuItem)sender).Tag)[0];
            String name = (String)((object[])((MenuItem)sender).Tag)[1];
            int pid = (int)((object[])((MenuItem)sender).Tag)[2];
            int fader = (int)((object[])((MenuItem)sender).Tag)[3];
            AssignmentType aType = pid >= 0 ? AssignmentType.Process : AssignmentType.Master;

            Console.WriteLine(String.Format("Assigning fader {0} to {1} - {2}", fader, proc, name));
            assignments[fader] = new Assignment(proc, name, pid, aType);
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
                    if (fader < 8 && assignments[fader].process != null)
                    {
                        //Console.Write("Got assignment " + fader + "; " + assignments[fader].windowName + "; ");
                        if (assignments[fader].aType == AssignmentType.Process)
                            VolumeMixer.SetApplicationVolume(assignments[fader].pid, me.ControllerValue / 127f * 100f);
                        else if (assignments[fader].aType == AssignmentType.Master)
                        {
                            float vol = me.ControllerValue / 127f * 100f;
                            Console.WriteLine(String.Format("Setting master volume to: {0} {1}", vol, me.ControllerValue));
                            SetMasterVol(vol);
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