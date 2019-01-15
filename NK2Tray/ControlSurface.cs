namespace NK2Tray
{
    public enum ControlSurfaceEventType
    {
        FaderVolumeChange,
        FaderVolumeMute,
        Information
    }

    public class ControlSurfaceEvent
    {
        public ControlSurfaceEventType eventType;
        public int fader;
        public float value;

        public ControlSurfaceEvent(ControlSurfaceEventType et, int f, float v)
        {
            eventType = et;
            fader = f;
            value = v;
        }

        public ControlSurfaceEvent(ControlSurfaceEventType et, int f)
        {
            eventType = et;
            fader = f;
            value = 0;
        }
    }

    public enum ControlSurfaceDisplayType
    {
        AssignedState,
        MuteState,
        ErrorState
    }

    public class ControlSurfaceDisplay
    {
        public ControlSurfaceDisplayType displayType;
        public int fader;
        public bool state;

        public ControlSurfaceDisplay(ControlSurfaceDisplayType dt, int f, bool s)
        {
            displayType = dt;
            fader = f;
            state = s;
        }
    }
}
