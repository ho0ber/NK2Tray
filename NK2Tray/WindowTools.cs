using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        public static int GetPidByName(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);
            int pid = 0;
            int maxHandleCount = 0;
            foreach (Process process in processes)
            {
                if (process.Id != 0 && process.HandleCount > maxHandleCount)
                {
                    maxHandleCount = process.HandleCount;
                    pid = process.Id;
                }
            }
            return pid;
            /*{
                var hWnd = FindWindow(name, "");
                if (hWnd == IntPtr.Zero)
                    return 0;

                uint pID;
                GetWindowThreadProcessId(hWnd, out pID);
                if (pID == 0)
                    return 0;

                Console.WriteLine(pID);
                return Convert.ToInt32(pID);
            }*/

        }

        public static int GetForegroundPID()
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

        public static bool ProcessExists(uint processId)
        {
            try
            {
                var process = Process.GetProcessById((int)processId);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static void Dump(object ob)
        {
            Console.WriteLine("================");
            foreach (var prop in ob.GetType().GetProperties())
                Console.WriteLine($@"{prop.Name} = {prop.GetValue(ob, null)}");
        }

        public static bool IsProcessByNameRunning(string processName)
        {
            return GetPidByName(processName) != 0;
        }

        public static void StartApplication(string applicationPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(applicationPath));
            }
            catch (InvalidOperationException)
            {
                // Application cannot be started for this button
            } 
        }
    }
}
