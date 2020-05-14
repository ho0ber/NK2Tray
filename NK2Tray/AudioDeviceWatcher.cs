using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static NAudio.CoreAudioApi.AudioSessionManager;

namespace NK2Tray
{
    /**
     * Manages keeping an eye over audio devices and their volume mixer sessions, keeping
     * them updated and properly linked at all times.
     *
     * First, a DeviceNotificationClient is attached to a generic MMDeviceEnumerator so
     * we can handle when audio devices are added/removed/updated, including setting a
     * new default device.
     *
     * For each device registered, we register a SessionCreatedDelegate to watch for created
     * sessions so we can register them. A session in this instance is an entry in the
     * Windows Volume Mixer. We also iterate through existing sessions and register them too.
     *
     * For each session registered, we register a SessionNotificationClient used to watch
     * for things like volume changes, state changes, and renames.
     *
     * All of this received information is stored in two easy lists: Devices and Sessions,
     * available for consumption by any part of the app.
     */
    public class AudioDeviceWatcher
    {
        private readonly MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
        // Used once to watch for incoming/outgoing devices
        private readonly DeviceNotificationClient deviceNotificationClient;
        // Used on each device to see created sessions
        private Dictionary<MMDevice, SessionCreatedDelegate> sessionHandlerMap = new Dictionary<MMDevice, SessionCreatedDelegate>();
        // Used on each device to track device volume changes
        private Dictionary<MMDevice, AudioEndpointVolumeNotificationDelegate> deviceVolumeHandlerMap = new Dictionary<MMDevice, AudioEndpointVolumeNotificationDelegate>();
        // Used per session to track activity like volume change and disconnection
        private Dictionary<AudioSessionControl, SessionNotificationClient> sessionEventMap = new Dictionary<AudioSessionControl, SessionNotificationClient>();

        public MMDevice DefaultDevice;
        public List<MMDevice> Devices = new List<MMDevice>();
        // QuickDeviceNames here is used as a simple map to set friendly names against devices.
        // The usual method of MMDevice.FriendlyName actually reaches out to the device and is
        // a lot slower as a result, causing UI/performance lag.
        public Dictionary<MMDevice, string> QuickDeviceNames = new Dictionary<MMDevice, string>();
        public Dictionary<MMDevice, string> QuickDeviceIds = new Dictionary<MMDevice, string>();
        public Dictionary<string, List<AudioSessionControl>> Sessions = new Dictionary<string, List<AudioSessionControl>>();
        public MidiDevice MidiDevice;
        public EventHandler<SessionVolumeChangedEventArgs> OnSessionVolumeChange;
        public EventHandler<DeviceVolumeChangedEventArgs> OnDeviceVolumeChange;

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

            var defaultDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            var localDefaultDevice = devices.Find(localDev => localDev.ID == defaultDevice.ID);
            SetDefaultDevice(localDefaultDevice);
            defaultDevice.Dispose();
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

        private void SetupDevice(MMDevice device)
        {
            // Watch created sessions
            sessionHandlerMap[device] = GetSessionCreatedHandler(device);
            device.AudioSessionManager.OnSessionCreated += sessionHandlerMap[device];

            // Set device details for later
            QuickDeviceNames.Add(device, device.FriendlyName);
            QuickDeviceIds.Add(device, device.ID);

            // Watch for volume changes
            deviceVolumeHandlerMap[device] = GetDeviceVolumeChangeHandler(device);
            device.AudioEndpointVolume.OnVolumeNotification += deviceVolumeHandlerMap[device];

            for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
            {
                var session = device.AudioSessionManager.Sessions[i];
                AddSession(device, session);
            }
        }

        private AudioEndpointVolumeNotificationDelegate GetDeviceVolumeChangeHandler (MMDevice device)
        {
            return (AudioVolumeNotificationData data) => AudioEndpointVolume_OnVolumeNotification(device, data);
        }

        // This needs throttling like the session volume control.
        private void AudioEndpointVolume_OnVolumeNotification(MMDevice device, AudioVolumeNotificationData data)
        {
            var eventArgs = new DeviceVolumeChangedEventArgs(){
                device = device,
                isMuted = data.Muted,
                volume = data.MasterVolume
            };

            OnDeviceVolumeChange?.Invoke(this, eventArgs);
        }

        private string GetSessionId(AudioSessionControl session)
        {
            var pipeLoc = session.GetSessionIdentifier.IndexOf("|");
            var len = session.GetSessionIdentifier.Length;
            var id = session.GetSessionIdentifier.Substring(pipeLoc + 1, len - pipeLoc - 1);

            return id;
        }

        public string GetSessionIdByPid (int pid)
        {
            string sessionId = null;

            foreach (var pair in Sessions)
            {
                var hasProcess = pair.Value.Any(session => (int)session.GetProcessID == pid);

                if (hasProcess)
                {
                    sessionId = pair.Key;
                    break;
                }
            }

            return sessionId;
        }

        public void AddSession(MMDevice device, AudioSessionControl session)
        {
            if (session.State == AudioSessionState.AudioSessionStateExpired) return;

            // Get ident.
            var id = GetSessionId(session);
            var processId = (int)session.GetProcessID;

            // Set name.
            Process process = Process.GetProcessById(processId);

            if (session.DisplayName == null || session.DisplayName == "")
                session.DisplayName = process != null ? process.ProcessName : id;

            // Push
            if (!Sessions.ContainsKey(id)) Sessions[id] = new List<AudioSessionControl>();
            if (Sessions[id].Contains(session)) return;

            sessionEventMap[session] = new SessionNotificationClient(this, device, id, session);
            session.RegisterEventClient(sessionEventMap[session]);
            Sessions[id].Add(session);
        }

        public void RemoveSession(AudioSessionControl session)
        {
            session.UnRegisterEventClient(sessionEventMap[session]);
            sessionEventMap.Remove(session);

            var id = GetSessionId(session);

            Sessions[id].Remove(session);
            if (Sessions[id].Count == 0) Sessions.Remove(id);
        }

        private void DisposeDevice(MMDevice device)
        {
            // Remove session watcher
            device.AudioSessionManager.OnSessionCreated -= sessionHandlerMap[device];
            sessionHandlerMap.Remove(device);

            // Remove volume watcher
            device.AudioEndpointVolume.OnVolumeNotification -= deviceVolumeHandlerMap[device];
            deviceVolumeHandlerMap.Remove(device);

            // Let go of device details
            QuickDeviceNames.Remove(device);
            QuickDeviceIds.Remove(device);

            // Dispose
            device.Dispose();
        }

        public MMDevice FindDevice(string deviceId)
        {
            return Devices.Find(d => d.ID == deviceId);
        }

        public void SetDefaultDevice(string deviceId)
        {
            var device = FindDevice(deviceId);
            SetDefaultDevice(device);
        }

        public void SetDefaultDevice(MMDevice device)
        {
            DefaultDevice = device;
        }

        public void AddDevice(MMDevice device)
        {
            if (Devices.Contains(device)) return;
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
        private string _sessionId;
        private AudioSessionControl _session;
        private VolumeChangedEventArgs _latestVolumeArgs;
        private readonly System.Timers.Timer _throttleTimer;
        private static readonly double throttleMs = 50;
        private readonly object throttleLock = new object();

        public SessionNotificationClient(
            AudioDeviceWatcher audioDeviceWatcher,
            MMDevice device,
            string sessionId,
            AudioSessionControl session
        )
        {
            _audioDeviceWatcher = audioDeviceWatcher;
            _device = device;
            _sessionId = sessionId;
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
            lock (throttleLock)
            {
                if (_latestVolumeArgs == null) return;

                var eventArgs = new SessionVolumeChangedEventArgs(){
                    sessionId = _sessionId,
                    isMuted = _latestVolumeArgs.isMuted,
                    volume = _latestVolumeArgs.volume
                };

                _audioDeviceWatcher.OnSessionVolumeChange?.Invoke(_audioDeviceWatcher, eventArgs);
                _latestVolumeArgs = null;
            }
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
        public void OnStateChanged(AudioSessionState state)
        {
            if (state != AudioSessionState.AudioSessionStateExpired)
            {
                _audioDeviceWatcher.AddSession(_device, _session);
            }
            else
            {
                _audioDeviceWatcher.RemoveSession(_session);
            }
        }
    }

    public class VolumeChangedEventArgs : EventArgs
    {
        public float volume { get; set; }
        public bool isMuted { get; set; }
    }

    public class DeviceVolumeChangedEventArgs : VolumeChangedEventArgs
    {
        public MMDevice device;
    }

    public class SessionVolumeChangedEventArgs : VolumeChangedEventArgs
    {
        public string sessionId;
    }
}
