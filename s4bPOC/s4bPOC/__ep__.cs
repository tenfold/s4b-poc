
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SysCore;
using Skype4BizCore;


namespace s4bPOC
{
   class __ep__
   {
      static void Main(string[] args)
      {
         ProcMonitor procMonitor = new ProcMonitor();
         procMonitor.Run();

         string numIn = "+13459891212";
         if (!numIn.xIsTel())
            throw new Exception("BadTelNum");
         procMonitor.FireEvent("someevent", numIn, "skypenum", "blabla");
      }
   }
}
