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
using System.Runtime.InteropServices;

namespace NK2Tray
{
    public class SysTrayApp : Form
    {
        [STAThread]
        public static void Main() => Application.Run(new SysTrayApp());

        private NotifyIcon trayIcon;
        public MidiDevice midiDevice;
        public AudioDevice audioDevice;

        public SysTrayApp()
        {
            Console.WriteLine($@"NK2 Tray {DateTime.Now}");
            trayIcon = new NotifyIcon();
            trayIcon.Text = "NK2 Tray";
            trayIcon.Icon = new Icon(Properties.Resources.nk2tray, 40, 40);

            trayIcon.ContextMenu = new ContextMenu();
            trayIcon.ContextMenu.Popup += OnPopup;

            trayIcon.Visible = true;

            audioDevice = new AudioDevice();
            midiDevice = new MidiDevice("nano");
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        }

        private void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            midiDevice.ResetAllLights();
            midiDevice.faders.Last().SetRecordLight(true);
            Environment.Exit(1);
        }

        private void OnPopup(object sender, EventArgs e)
        {
            ContextMenu trayMenu = (ContextMenu)sender;
            trayMenu.MenuItems.Clear();

            var mixerSessions = audioDevice.GetMixerSessions();
            var masterMixerSession = new MixerSession("Master", SessionType.Master, AudioDevice.GetDeviceVolumeObject());

            foreach (var fader in midiDevice.faders)
            {
                MenuItem faderMenu = new MenuItem($@"Fader {fader.faderNumber} - {(fader.assigned ? fader.assignment.label : "")}");
                trayMenu.MenuItems.Add(faderMenu);

                // Add master mixerSession to menu
                MenuItem masterItem = new MenuItem(masterMixerSession.label, AssignFader);
                masterItem.Tag = new object[] { fader, masterMixerSession };
                faderMenu.MenuItems.Add(masterItem);

                // Add application mixer sessions to each fader
                foreach (var mixerSession in mixerSessions)
                {
                    MenuItem si = new MenuItem(mixerSession.label, AssignFader);
                    si.Tag = new object[] { fader, mixerSession };
                    faderMenu.MenuItems.Add(si);
                }

                // Add unassign option
                MenuItem unassignItem = new MenuItem("UNASSIGN", UnassignFader);
                unassignItem.Tag = new object[] { fader };
                faderMenu.MenuItems.Add(unassignItem);

            }

            trayMenu.MenuItems.Add("Exit", OnExit);
        }

        private void AssignFader(object sender, EventArgs e)
        {
            var fader = (Fader)((object[])((MenuItem)sender).Tag)[0];
            var mixerSession = (MixerSession)((object[])((MenuItem)sender).Tag)[1];
            fader.Assign(mixerSession);
        }

        private void UnassignFader(object sender, EventArgs e)
        {
            var fader = (Fader)((object[])((MenuItem)sender).Tag)[0];
            fader.Unassign();
        }

        /*
        private void AssignFader(object sender, EventArgs e)
        {
            var assignment = new Assignment(sender);
            //NanoKontrol2.Respond(ref midiOut, new ControlSurfaceDisplay(ControlSurfaceDisplayType.AssignedState, assignment.fader, true));
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
        */

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
        }

    }
}
