using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
    
namespace NK2Tray
{
    class KeyboardShortcut
    {
        String shortcutString;
        KeyboardShortcut(String shortcut)
        {
            shortcutString = shortcut;
        }

        void execute()
        {
            System.Windows.Forms.SendKeys.Send(shortcutString);
            
            //{ENTER}
            //https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=netframework-4.8
        }
    }
}
