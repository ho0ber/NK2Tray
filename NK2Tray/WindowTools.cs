using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NK2Tray
{
    class WindowTools
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public int GetPidByName(string name)
        {
            {
                var hWnd = FindWindow(name, "");
                if (hWnd == IntPtr.Zero)
                    return 0;

                uint pID;
                GetWindowThreadProcessId(hWnd, out pID);
                if (pID == 0)
                    return 0;

                Console.WriteLine(pID);
                return Convert.ToInt32(pID);
            }

        }

        public int GetForegroundPID()
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return 0;

            uint pID;
            GetWindowThreadProcessId(hWnd, out pID);
            if (pID == 0)
                return 0;

            return Convert.ToInt32(pID);
        }

        public int GetSpotifyPID()
        {
            var hWnd = FindWindow("Chrome_WidgetWin_0", "");
            if (hWnd == IntPtr.Zero)
                return 0;

            uint pID;
            GetWindowThreadProcessId(hWnd, out pID);
            if (pID == 0)
                return 0;

            Console.WriteLine(pID);
            return Convert.ToInt32(pID);
        }

        public int GetDiscordPID()
        {
            {
                var hWnd = FindWindow("Discord", "");
                if (hWnd == IntPtr.Zero)
                    return 0;

                uint pID;
                GetWindowThreadProcessId(hWnd, out pID);
                if (pID == 0)
                    return 0;

                Console.WriteLine(pID);
                return Convert.ToInt32(pID);
            }

        }
    }
}
