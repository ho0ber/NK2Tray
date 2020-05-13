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
        // Used once to watch for incoming/outgoing devices
        private readonly DeviceNotificationClient deviceNotificationClient;
        // Used on each device to see created sessions
        private Dictionary<MMDevice, SessionCreatedDelegate> sessionHandlerMap = new Dictionary<MMDevice, SessionCreatedDelegate>();
        // Used per session to track activity like volume change and disconnection
        private Dictionary<AudioSessionControl, SessionNotificationClient> sessionEventMap = new Dictionary<AudioSessionControl, SessionNotificationClient>();

        public MMDevice DefaultDevice;
        public List<MMDevice> Devices = new List<MMDevice>();
        public List<AudioSessionControl> Sessions = new List<AudioSessionControl>();

        public AudioDeviceWatcher()
        {
            deviceNotificationClient = new DeviceNotificationClient(this);
            deviceEnum.RegisterEndpointNotificationCallback(deviceNotificationClient);
            Bootstrap();
        }

        private void Bootstrap()
        {
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            devices.ForEach(AddDevice);
        }

        private void OnSessionCreated(MMDevice device, object sender, IAudioSessionControl session)
        {
            var resolvedSession = new AudioSessionControl(session);
            AddSession(device, resolvedSession);
        }

        private SessionCreatedDelegate GetSessionCreatedHandler(MMDevice device)
        {
            return (object sender, IAudioSessionControl session) => OnSessionCreated(device, sender, session);
        }

        /*
        private SessionNotificationClient GetSessionEventHandler(MMDevice device)
        {
            return new SessionNotificationClient(this, device);
        }
        */

        private void SetupDevice(MMDevice device)
        {
            sessionHandlerMap[device] = GetSessionCreatedHandler(device);
            device.AudioSessionManager.OnSessionCreated += sessionHandlerMap[device];

            // This never triggers lol. Have to do per session
            //device.AudioSessionManager.AudioSessionControl.RegisterEventClient(new SessionNotificationClient(this, device));

            for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
            {
                var session = device.AudioSessionManager.Sessions[i];
                AddSession(device, session);
            }

            /*
            device.AudioSessionManager.AudioSessionControl.RegisterEventClient

            foreach (var session in device.AudioSessionManager.Sessions)
            {
                sessionEventMap[device] = GetSessionEventHandler(device);
                device.AudioSessionManager.AudioSessionControl.RegisterEventClient(sessionEventMap[device]);
            }
            */
        }

        public void AddSession(MMDevice device, AudioSessionControl session)
        {
            if (session.State == AudioSessionState.AudioSessionStateExpired) return;
            sessionEventMap[session] = new SessionNotificationClient(this, device, session);
            session.RegisterEventClient(sessionEventMap[session]);
            Sessions.Add(session);
        }

        private void DisposeSession(AudioSessionControl session)
        {
            session.UnRegisterEventClient(sessionEventMap[session]);
            sessionEventMap.Remove(session);
        }

        public void RemoveSession(AudioSessionControl session)
        {
            DisposeSession(session);
            Sessions.Remove(session);
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

    class DeviceNotificationClient : IMMNotificationClient
    {
        private AudioDeviceWatcher audioDeviceWatcher;

        public DeviceNotificationClient(AudioDeviceWatcher audioDeviceWatcher)
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

    class SessionNotificationClient: IAudioSessionEventsHandler
    {
        private AudioDeviceWatcher _audioDeviceWatcher;
        private MMDevice _device;
        private AudioSessionControl _session;
        private VolumeChangedEventArgs _latestVolumeArgs;
        private readonly System.Timers.Timer _throttleTimer;
        private static readonly double throttleMs = 100;

        public SessionNotificationClient(AudioDeviceWatcher audioDeviceWatcher, MMDevice device, AudioSessionControl session)
        {
            _audioDeviceWatcher = audioDeviceWatcher;
            _device = device;
            _session = session;
            _throttleTimer = new System.Timers.Timer(SessionNotificationClient.throttleMs);
            _throttleTimer.Elapsed += _throttleTimer_Elapsed;
        }

        public void OnVolumeChanged(float volume, bool isMuted) => ThrottleVolumeChange(volume, isMuted);

        private void ThrottleVolumeChange(float volume, bool isMuted)
        {
            _latestVolumeArgs = new VolumeChangedEventArgs() { volume = volume, isMuted = isMuted };
            if (_throttleTimer.Enabled) return;

            SendVolumeChange();
            _throttleTimer.Start();
        }
        private void _throttleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _throttleTimer.Stop();
            SendVolumeChange();
        }

        private void SendVolumeChange()
        {
            if (_latestVolumeArgs == null) return;
            // _mixerSession.OnVolumeChanged(_latestVolumeArgs);
            Console.WriteLine("Changing volume of {0} to {1}. Muted? {2}", _device.DeviceFriendlyName, _latestVolumeArgs.volume, _latestVolumeArgs.isMuted);
            _latestVolumeArgs = null;
        }

        //
        // Summary:
        //     notification of the client that the volume level of an audio channel in the session
        //     submix has changed
        //
        // Parameters:
        //   channelCount:
        //     The channel count.
        //
        //   newVolumes:
        //     An array of volumnes cooresponding with each channel index.
        //
        //   channelIndex:
        //     The number of the channel whose volume level changed.
        public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }
        //
        // Summary:
        //     notification of display name changed
        //
        // Parameters:
        //   displayName:
        //     the current display name
        public void OnDisplayNameChanged(string displayName) { }
        //
        // Summary:
        //     notification of the client that the grouping parameter for the session has changed
        //
        // Parameters:
        //   groupingId:
        //     >The new grouping parameter for the session.
        public void OnGroupingParamChanged(ref Guid groupingId) { }
        //
        // Summary:
        //     notification of icon path changed
        //
        // Parameters:
        //   iconPath:
        //     the current icon path
        public void OnIconPathChanged(string iconPath) { }
        //
        // Summary:
        //     notification of the client that the session has been disconnected
        //
        // Parameters:
        //   disconnectReason:
        //     The reason that the audio session was disconnected.
        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            _audioDeviceWatcher.RemoveSession(_session);
        }
        //
        // Summary:
        //     notification of the client that the stream-activity state of the session has
        //     changed
        //
        // Parameters:
        //   state:
        //     The new session state.
        public void OnStateChanged(AudioSessionState state) { }
    }
}
