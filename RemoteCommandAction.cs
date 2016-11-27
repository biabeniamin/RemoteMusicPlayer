using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicAlarm
{
    public enum RemoteCommandAction
    {
        Play=0,
        Pause=1,
        Next=2,
        Previous=3,
        VolumeUp=4,
        VolumeDown,
        Close,
        None
    }
}
