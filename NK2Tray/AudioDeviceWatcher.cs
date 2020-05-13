using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NAudio.CoreAudioApi.AudioSessionManager;

namespace NK2Tray
{
    class AudioDeviceWatcher
    {
        private readonly MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
        private readonly NotificationClient notificationClient;
        private Dictionary<MMDevice, SessionCreatedDelegate> sessionHandlerMap = new Dictionary<MMDevice, SessionCreatedDelegate>();

        public MMDevice DefaultDevice;
        public List<MMDevice> Devices;

        public AudioDeviceWatcher()
        {
            notificationClient = new NotificationClient(this);
            deviceEnum.RegisterEndpointNotificationCallback(notificationClient);
            Bootstrap();
        }

        private void Bootstrap()
        {
            Devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            Devices.ForEach(SetupDevice);
        }

        private void OnSessionCreated(MMDevice device, object sender, IAudioSessionControl session)
        {
            string sessionDisplayName;
            session.GetDisplayName(out sessionDisplayName);
            Console.WriteLine("Session created on {0} for {1}", device.DeviceFriendlyName, sessionDisplayName);
        }

        public SessionCreatedDelegate GetSessionCreatedHandler(MMDevice device)
        {
            return (object sender, IAudioSessionControl session) => OnSessionCreated(device, sender, session);
        }

        private void SetupDevice(MMDevice device)
        {
            sessionHandlerMap[device] = GetSessionCreatedHandler(device);
            device.AudioSessionManager.OnSessionCreated += sessionHandlerMap[device];
        }

        private void DisposeDevice(MMDevice device)
        {
            device.AudioSessionManager.OnSessionCreated -= sessionHandlerMap[device];
            sessionHandlerMap.Remove(device);
        }

        public MMDevice FindDevice(string deviceId)
        {
            return Devices.Find(d => d.ID == deviceId);
        }

        public void SetDefaultDevice(string deviceId)
        {
            var device = FindDevice(deviceId);
            if (device == null) return;

            DefaultDevice = device;
        }

        public void AddDevice(MMDevice device)
        {
            SetupDevice(device);
            Devices.Add(device);
        }

        public void AddDevice(string deviceId)
        {
            var device = FindDevice(deviceId);
            if (device == null) return;

            AddDevice(device);
        }

        public void RemoveDevice(MMDevice device)
        {
            DisposeDevice(device);
            Devices.Remove(device);
        }

        public void RemoveDevice(string deviceId)
        {
            var device = FindDevice(deviceId);
            if (device == null) return;

            RemoveDevice(device);
        }
    }

    class NotificationClient : IMMNotificationClient
    {
        private AudioDeviceWatcher audioDeviceWatcher;

        public NotificationClient(AudioDeviceWatcher audioDeviceWatcher)
        {
            //_realEnumerator.RegisterEndpointNotificationCallback();
            if (System.Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }

            this.audioDeviceWatcher = audioDeviceWatcher;
        }

        public void OnDefaultDeviceChanged(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
        {
            //Do some Work
            Console.WriteLine("OnDefaultDeviceChanged --> {0}", dataFlow.ToString());
            
            if (
                dataFlow == DataFlow.Render
                && (deviceRole == Role.Console || deviceRole == Role.Multimedia)
            )
            {
                audioDeviceWatcher.SetDefaultDevice(defaultDeviceId);
            }
        }

        public void OnDeviceAdded(string deviceId)
        {
            //Do some Work
            Console.WriteLine("OnDeviceAdded --> {0}", deviceId);
            audioDeviceWatcher.AddDevice(deviceId);
        }

        public void OnDeviceRemoved(string deviceId)
        {

            Console.WriteLine("OnDeviceRemoved --> {0}", deviceId);
            //Do some Work
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            Console.WriteLine("OnDeviceStateChanged\n Device Id -->{0} : Device State {1}", deviceId, newState);
            //Do some Work
            if (newState == DeviceState.Active)
            {
                audioDeviceWatcher.AddDevice(deviceId);
            }
            else
            {
                audioDeviceWatcher.RemoveDevice(deviceId);
            }
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey)
        {
            //Do some Work
            //fmtid & pid are changed to formatId and propertyId in the latest version NAudio
            Console.WriteLine("OnPropertyValueChanged: formatId --> {0}  propertyId --> {1}", propertyKey.formatId.ToString(), propertyKey.propertyId.ToString());
        }

    }
}
