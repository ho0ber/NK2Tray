using NAudio.CoreAudioApi;
using System;

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
}
