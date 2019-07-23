
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
         SkypeWatcher procMonitor = new SkypeWatcher();
         procMonitor.Run();

         string numIn = "+48883279496";
         /*if (!numIn.xIsTel())
            throw new Exception("BadTelNum");*/
         string ext = "1111";
         Guid pbxid = Guid.NewGuid();
         procMonitor.FireInboundEvent("Ringing", numIn, ext, pbxid.ToString());

      }
   }
}
