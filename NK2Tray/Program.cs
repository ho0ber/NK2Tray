using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.Midi;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using NAudio.CoreAudioApi.Interfaces;
using NK2Tray.Properties;
using System.Configuration;

namespace NK2Tray
{
    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        public MidiIn midiIn;
        public MidiOut midiOut;
        public List<Assignment> assignments = new List<Assignment>();
        private MMDevice device;
        public AudioEndpointVolume deviceVolume;

        public SysTrayApp()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NK2 Tray";
            trayIcon.Icon = new Icon(Properties.Resources.nk2tray, 40, 40);

            trayIcon.ContextMenu = new ContextMenu();
            trayIcon.ContextMenu.Popup += OnPopup;

            UpdateDevice();
            //var deviceEnumerator = new MMDeviceEnumerator();
            //device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            //device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            //deviceVolume = device.AudioEndpointVolume;

            trayIcon.Visible = true;

            InitMidi();

            foreach (var i in Enumerable.Range(0, 8))
                assignments.Add(new Assignment());

            if (GetAppSettings("1") != null)
                LoadAssignments();
            else
                InitAssignments();

            ListenForMidi();
        }

        private void OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            Console.WriteLine("OnSessionCreated");
            AudioSessionControl newAudioSession = new AudioSessionControl(newSession);
            foreach (var i in Enumerable.Range(0, 8))
            {
                if (assignments[i].assigned && !assignments[i].IsAlive())
                    if (assignments[i].sessionIdentifier == newAudioSession.GetSessionIdentifier)
                    {
                        assignments[i].UpdateSession(newAudioSession);
                        NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, i, false));
                    }
            }
            //NAudioEventCallbacks callbacks = new NAudioEventCallbacks();
            //AudioSessionEventsCallback notifications = new AudioSessionEventsCallback(callbacks);
            //audioSession.RegisterEventClient(callbacks);

        }

        public void SaveAssignments()
        {
            Console.WriteLine("Saving assignments");

            foreach (var i in Enumerable.Range(0, 8))
            {
                if (assignments[i].assigned)
                {
                    if (assignments[i].aType == AssignmentType.Master)
                        AddOrUpdateAppSettings(i.ToString(), "__MASTER__");
                    else
                        AddOrUpdateAppSettings(i.ToString(), assignments[i].sessionIdentifier);
                }
                else
                    AddOrUpdateAppSettings(i.ToString(), "");
            }
        }

        public void LoadAssignments()
        {
            Console.WriteLine("Loading assignments: " + GetAppSettings("assignments"));
            UpdateDevice();

            foreach (var i in Enumerable.Range(0, 8))
            {
                //assignments.Add(new Assignment());
                Console.WriteLine("Getting setting: " + i.ToString());
                var ident = GetAppSettings(i.ToString());
                Console.WriteLine("Got setting: " + ident);
                if (ident != null)
                {
                    if (ident == "__MASTER__")
                    {
                        Console.WriteLine(String.Format("Fader {0} is {1} (master)", i, ident));
                        assignments[i] = new Assignment("Master Volume", "", -1, AssignmentType.Master, "", "", null);
                        NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, i, true));
                        Console.WriteLine("Assigned!");
                    }
                    else if (ident.Length > 0)
                    {
                        Console.WriteLine(String.Format("Fader {0} is {1} (process)", i, ident));
                        var matchingSession = FindSession(ident);
                        if (matchingSession != null)
                        {
                            assignments[i] = new Assignment(matchingSession, i);
                            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, i, true));
                            Console.WriteLine("Assigned!");
                        }
                        else
                        {
                            assignments[i] = new Assignment(ident, i);
                            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, i, true));
                            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, i, true));
                            Console.WriteLine("Assigned!");
                        }
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Fader {0} is {1} (nothing)", i, ident));
                        assignments[i] = new Assignment();
                        Console.WriteLine("Assigned!");
                    }
                }
            }
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        public static string GetAppSettings(string key)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] != null)
                    return settings[key].Value;
                else
                    return null;
            }
            catch
            {
                Console.WriteLine("Error getting app settings"); 
            }
            return null;
        }

        public AudioSessionControl FindSession(String sessionIdentifier)
        {
            var sessions = device.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                for (int i = 0; i < sessions.Count; i++)
                {
                    if (sessions[i].GetSessionIdentifier == sessionIdentifier)
                        return sessions[i];
                }
            }
            return null;
        }

        public AudioSessionControl FindSession(int pid)
        {
            var sessions = device.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                for (int i = 0; i < sessions.Count; i++)
                {
                    if (sessions[i].GetProcessID == pid)
                        return sessions[i];
                }
            }
            return null;
        }
        /*
        // Saving these for later because I'll definitely need them.
        public class NAudioEventCallbacks : IAudioSessionEventsHandler
        {
            public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { Console.WriteLine("OnChannelVolumeChanged"); }

            public void OnDisplayNameChanged(string displayName) { Console.WriteLine("OnDisplayNameChanged"); }

            public void OnGroupingParamChanged(ref Guid groupingId) { Console.WriteLine("OnGroupingParamChanged"); }

            public void OnIconPathChanged(string iconPath) { Console.WriteLine("OnIconPathChanged"); }

            public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) { Console.WriteLine("OnSessionDisconnected"); }

            public void OnStateChanged(AudioSessionState state) { Console.WriteLine("OnStateChanged"); }

            public void OnVolumeChanged(float volume, bool isMuted) { Console.WriteLine("OnVolumeChanged"); }
        }
        */

        private void InitAssignments()
        {
            assignments[7] = new Assignment("Master Volume", "", -1, AssignmentType.Master, "", "", null);
            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, 7, true));
            SaveAssignments();
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
            //Process process = Process.GetProcessById((int)session.GetProcessID);
            //Console.WriteLine(String.Format("lbl = {0}", process.MainWindowTitle != "" ? process.MainWindowTitle : process.ProcessName));
        }

        private void UpdateDevice()
        {
            // Add audio devices to each fader menu items
            var deviceEnumerator = new MMDeviceEnumerator();

            device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioSessionManager.OnSessionCreated += OnSessionCreated;
            deviceVolume = device.AudioEndpointVolume;
        }

        private void OnPopup(object sender, EventArgs e)
        {
            ContextMenu trayMenu = (ContextMenu)sender;
            trayMenu.MenuItems.Clear();

            // Make all of the fader menu items
            foreach (var i in Enumerable.Range(0, 8))
            {
                MenuItem faderMenu = new MenuItem(String.Format("Fader {0} - {1}", i, assignments[i].assigned ? assignments[i].processName : ""));
                trayMenu.MenuItems.Add(faderMenu);
            }

            UpdateDevice();
            GenerateDeviceMenuItems(ref trayMenu);

            var sessions = device.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                ISet<String> seen = new HashSet<String>();
                ISet<String> duplicates = new HashSet<String>();
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    Process process = Process.GetProcessById((int)session.GetProcessID);
                    if (seen.Contains(process.ProcessName))
                        duplicates.Add(process.ProcessName);
                    else
                        seen.Add(process.ProcessName);
                }

                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (session.IsSystemSoundsSession && ProcessExists(session.GetProcessID))
                    {
                        //DumpProps(session);
                        GenerateMenuItems(ref trayMenu, session, duplicates);
                        break;
                    }
                }
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (!session.IsSystemSoundsSession && ProcessExists(session.GetProcessID))
                    {
                        //DumpProps(session);
                        GenerateMenuItems(ref trayMenu, session, duplicates);
                    }
                }
            }

            GenerateUnassignMenuItems(ref trayMenu);

            trayMenu.MenuItems.Add("Exit", OnExit);
        }

        private void GenerateMenuItems(ref ContextMenu trayMenu, AudioSessionControl session, ISet<String> duplicates)
        {
            Process process = Process.GetProcessById((int)session.GetProcessID);
            foreach (var i in Enumerable.Range(0, 8))
            {
                String sessionTitle;
                if (session.IsSystemSoundsSession)
                    sessionTitle = "System Sounds";
                else if (duplicates.Contains(process.ProcessName) && process.MainWindowTitle != "")
                    sessionTitle = String.Format("{0} ({1})", process.ProcessName, string.Concat(process.MainWindowTitle.Take(15)));
                else
                    sessionTitle = process.ProcessName;

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

        private void GenerateUnassignMenuItems(ref ContextMenu trayMenu)
        {
            foreach (var i in Enumerable.Range(0, 8))
            {
                MenuItem si = new MenuItem("UNASSIGN", UnassignFader);
                si.Tag = new object[] { i };
                trayMenu.MenuItems[i].MenuItems.Add(si);
            }
        }

        private void AssignFader(object sender, EventArgs e)
        {
            var assignment = new Assignment(sender);
            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, assignment.fader, true));
            assignments[assignment.fader] = assignment;
            SaveAssignments();
        }

        private void UnassignFader(object sender, EventArgs e)
        {
            int fader = (int)((object[])((MenuItem)sender).Tag)[0];
            UnassignFader(fader);
        }

        private void UnassignFader(int fader)
        {
            Console.WriteLine("Unassigning fader " + fader);
            var assignment = new Assignment();
            NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, fader, false));
            assignments[fader] = assignment;
            SaveAssignments();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnExit(object sender, EventArgs e)
        {
            foreach (var i in Enumerable.Range(0, 128))
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());
            Application.Exit();
        }

        public void InitMidi()
        {
            FindMidiIn();
            FindMidiOut();

            //Reset all of the lights
            foreach (var i in Enumerable.Range(0, 128))
                midiOut.Send(new ControlChangeEvent(0, 1, (MidiController)i, 0).GetAsShortMessage());
        }

        public void ListenForMidi()
        {
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
        }

        public void FindMidiIn()
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (MidiIn.DeviceInfo(i).ProductName.ToLower().Contains("nano"))
                {
                    midiIn = new MidiIn(i);
                    Console.WriteLine(MidiIn.DeviceInfo(i).ProductName);
                    break;
                }  
            }
        }

        public void FindMidiOut()
        {
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (MidiOut.DeviceInfo(i).ProductName.ToLower().Contains("nano"))
                {
                    midiOut = new MidiOut(i);
                    Console.WriteLine(MidiOut.DeviceInfo(i).ProductName);
                    break;
                }
            }
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

            if (assignments[cse.fader].assigned && !assignments[cse.fader].IsAlive())
                NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, cse.fader, true));

            switch (cse.eventType)
            {
                case ControlSurfaceEventType.FaderVolumeChange:
                    ChangeApplicationVolume(cse);
                    break;
                case ControlSurfaceEventType.FaderVolumeMute:
                    MuteApplication(cse);
                    break;
                case ControlSurfaceEventType.Information:
                    GetAssignmentInformation(cse);
                    break;
                case ControlSurfaceEventType.Assignment:
                    AssignForegroundSession(cse);
                    break;
                default:
                    break;
            }
        }

        public void AssignForegroundSession(ControlSurfaceEvent cse)
        {
            if (assignments[cse.fader].assigned)
            {
                UnassignFader(cse.fader);
            }
            else
            {
                var pid = WindowTools.GetForegroundPID();
                UpdateDevice();
                var matchingSession = FindSession(pid);
                if (matchingSession != null)
                {
                    assignments[cse.fader] = new Assignment(matchingSession, cse.fader);
                    NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, cse.fader, true));
                    SaveAssignments();
                }
            }
        }

        public void GetAssignmentInformation(ControlSurfaceEvent cse)
        {
            if (assignments[cse.fader].session_alive)
            {
                Assignment assignment = assignments[cse.fader];
                if (assignment.aType == AssignmentType.Process)
                    DumpProps(assignment.audioSession);

                if (assignment.CheckHealth())
                    NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.ErrorState, cse.fader, false));

            }
        }

        public void ChangeApplicationVolume(ControlSurfaceEvent cse)
        {
            if (assignments[cse.fader].session_alive)
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
                NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.MuteState, cse.fader, muted));
            }
        }
    }
}
