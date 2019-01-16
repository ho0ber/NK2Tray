using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
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
            session_alive = true;
            assigned = true;
        }

        public Assignment(AudioSessionControl session, int f)
        {
            var process = Process.GetProcessById((int)session.GetProcessID);
            processName = process.ProcessName;
            windowName = process.MainWindowTitle;
            pid = (int)session.GetProcessID;
            fader = f;
            aType = AssignmentType.Process;
            sessionIdentifier = session.GetSessionIdentifier;
            instanceIdentifier = session.GetSessionInstanceIdentifier;
            audioSession = session;
            session_alive = true;
            assigned = true;
        }

        public Assignment(String ident, int f)
        {
            processName = "";
            windowName = "";
            pid = -1;
            fader = f;
            aType = AssignmentType.Process;
            sessionIdentifier = ident;
            instanceIdentifier = "";
            audioSession = null;
            session_alive = false;
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
            session_alive = true;
            assigned = true;
        }

        public bool CheckHealth()
        {
            if (!assigned)
                return false;

            if (!IsAlive())
                return HealSession();

            return true;
        }

        public bool IsAlive()
        {
            //Dump();
            if (!assigned || !session_alive)
                return false;

            if (aType == AssignmentType.Master)
                return true;

            if (audioSession.State == AudioSessionState.AudioSessionStateExpired)
            {
                session_alive = false;
                return false;
            }

            return true;
        }

        public void UpdateSession(AudioSessionControl session)
        {
            audioSession = session;
            session_alive = true;
        }

        public bool HealSession()
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessions = device.AudioSessionManager.Sessions;
            if (sessions != null)
            {
                foreach (var i in Enumerable.Range(0, sessions.Count))
                {
                    var session = sessions[i];
                    if (session.GetSessionIdentifier == sessionIdentifier)
                    {
                        audioSession = session;
                        return true;
                    }
                }
            }
            return false;
        }

        public void Dump()
        {
            Console.WriteLine(processName);
            Console.WriteLine(sessionIdentifier);
            Console.WriteLine(assigned);
            Console.WriteLine(session_alive);
        }
    }
}
