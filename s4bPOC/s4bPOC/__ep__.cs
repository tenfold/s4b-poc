
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Skype4BizCore;


namespace s4bPOC
{
   class __ep__
   {
      static void Main(string[] args)
      {
         ProcMonitor procMonitor = new ProcMonitor();
         procMonitor.Run();
         procMonitor.FireEvent("someevent", "+45909000", "skypenum", "blabla");
      }
   }
}
