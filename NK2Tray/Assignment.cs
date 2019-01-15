using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Windows.Forms;

namespace NK2Tray
{
    public enum AssignmentType
    {
        Process,
        Master
    }

    public class Assignment
    {
        public String processName;
        public String windowName;
        public int pid;
        public int fader;
        public String sessionIdentifier;
        public String instanceIdentifier;
        public AssignmentType aType;
        public AudioSessionControl audioSession;
        public bool assigned = false;
        public bool session_alive = false;

        public Assignment() { }

        public Assignment(object sender)
        {
            //Assignment from MenuItem Tag
            processName = (String)((object[])((MenuItem)sender).Tag)[0];
            windowName = (String)((object[])((MenuItem)sender).Tag)[1];
            pid = (int)((object[])((MenuItem)sender).Tag)[2];
            fader = (int)((object[])((MenuItem)sender).Tag)[3];
            aType = pid >= 0 ? AssignmentType.Process : AssignmentType.Master;
            sessionIdentifier = (String)((object[])((MenuItem)sender).Tag)[4];
            instanceIdentifier = (String)((object[])((MenuItem)sender).Tag)[5];
            audioSession = (AudioSessionControl)((object[])((MenuItem)sender).Tag)[6];
            assigned = true;
        }

        public Assignment(String p, String wn, int inPid, AssignmentType at, String sid, String iid, AudioSessionControl audsess)
        {
            processName = p;
            windowName = wn;
            pid = inPid;
            aType = at;
            sessionIdentifier = sid;
            instanceIdentifier = iid;
            audioSession = audsess;
            assigned = true;
        }

        public bool isAlive()
        {
            if (audioSession.State == AudioSessionState.AudioSessionStateExpired)
            {
                return false;
            }

            return false;
        }
    }
}
