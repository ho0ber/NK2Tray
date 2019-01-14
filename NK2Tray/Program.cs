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
    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        private MidiIn midiIn;
        public Assignment[] assignments = new Assignment[8];
        private MMDevice device;
        public AudioEndpointVolume deviceVolume;

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
            deviceVolume = device.AudioEndpointVolume;

            GenerateDeviceMenuItems(ref trayMenu);

            var sessions = device.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (session.IsSystemSoundsSession && ProcessExists(session.GetProcessID))
                    {
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
                si.Tag = new object[] { "Master Volume", "", -1, i, "", "", null };
                trayMenu.MenuItems[i].MenuItems.Add(si);
            }
        }

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
            ControlSurfaceEvent cse = NanoKontrol2.Evaluate(e);
            if (cse == null)
                return;

            switch(cse.eventType)
            {
                case ControlSurfaceEventType.FaderVolumeChange:
                    ChangeApplicationVolume(cse);
                    break;
                case ControlSurfaceEventType.FaderVolumeMute:
                    MuteApplication(cse);
                    break;
                default:
                    break;
            }
        }

        public void ChangeApplicationVolume(ControlSurfaceEvent cse)
        {
            if (assignments[cse.fader].processName != null)
            {
                Assignment assignment = assignments[cse.fader];
                if (assignment.aType == AssignmentType.Process)
                    assignment.audioSession.SimpleAudioVolume.Volume = cse.value;
                else if (assignment.aType == AssignmentType.Master)
                    deviceVolume.MasterVolumeLevelScalar = cse.value;
            }
        }

        public void MuteApplication(ControlSurfaceEvent cse)
        {
            bool muted = false;
            if (assignments[cse.fader].processName != null)
            {
                Assignment assignment = assignments[cse.fader];
                if (assignment.aType == AssignmentType.Process)
                {
                    muted = !assignment.audioSession.SimpleAudioVolume.Mute;
                    assignment.audioSession.SimpleAudioVolume.Mute = muted;
                }
                else if (assignment.aType == AssignmentType.Master)
                {
                    muted = !deviceVolume.Mute;
                    deviceVolume.Mute = muted;
                }
                NanoKontrol2.Respond(new ControlSurfaceDisplay(ControlSurfaceDisplayType.MuteState, muted));
            }
        }
    }
}