using System;
using Windows.Devices.Enumeration;

namespace NK2Tray
{
    class MidiDeviceWatcher
    {
        private DeviceWatcher deviceWatcher;
        private string deviceSelectorString;
        private Func<bool> UpdateDevices;
        private bool firstEnumerationComplete;

        public DeviceInformationCollection DeviceInformationCollection { get; set; }

        public MidiDeviceWatcher(string midiDeviceSelectorString, Func<bool> _updateDevices)
        {
            deviceSelectorString = midiDeviceSelectorString;
            UpdateDevices = _updateDevices;

            deviceWatcher = DeviceInformation.CreateWatcher(deviceSelectorString);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
        }

        ~MidiDeviceWatcher()
        {
            deviceWatcher.Added -= DeviceWatcher_Added;
            deviceWatcher.Removed -= DeviceWatcher_Removed;
            deviceWatcher.Updated -= DeviceWatcher_Updated;
            deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
            deviceWatcher = null;
        }

        public void StartWatcher()
        {
            deviceWatcher.Start();
        }

        public void StopWatcher()
        {
            deviceWatcher.Stop();
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (firstEnumerationComplete) UpdateDevices();
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (firstEnumerationComplete) UpdateDevices();
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (firstEnumerationComplete) UpdateDevices();
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            firstEnumerationComplete = true;
            UpdateDevices();
        }
    }
}
