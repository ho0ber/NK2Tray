using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NK2Tray
{
    public struct SessionAndMeta
    {
        public AudioSessionControl session;
        public bool duplicate;
        public int instanceNumber;

        public SessionAndMeta(AudioSessionControl s, bool d, int i)
        {
            session = s;
            duplicate = d;
            instanceNumber = i;
        }
    }

    public class SessionProcessor
    {
        public static List<SessionAndMeta> GetSessionMeta(ref SessionCollection sessions)
        {
            var outSessions = new List<SessionAndMeta>();
            var sessionsByIdent = new Dictionary<String, List<AudioSessionControl>>();

            Console.WriteLine("Getting sessions grouped by ident");
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (!sessionsByIdent.ContainsKey(session.GetSessionIdentifier))
                    sessionsByIdent[session.GetSessionIdentifier] = new List<AudioSessionControl>();
                sessionsByIdent[session.GetSessionIdentifier].Add(session);
            }
            Console.WriteLine("Done!");

            Console.WriteLine("Building SessionAndMeta for each");
            foreach (var ident in sessionsByIdent.Keys.ToList())
            {
                Console.WriteLine("Working on " + ident);
                var ordered = sessionsByIdent[ident].OrderBy(i => i.GetSessionInstanceIdentifier.Split('|').Last().Split('b').Last()).ToList();
                bool dup = ordered.Count > 1;
                for (int i = 0; i < ordered.Count; i++)
                {
                    outSessions.Add(new SessionAndMeta(ordered[i], dup, i));
                    Console.WriteLine("" + i + ordered[i].GetSessionInstanceIdentifier);
                }
            }

            return outSessions;
        }
    }
}
