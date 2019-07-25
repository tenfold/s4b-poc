
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
/* - - */
using SysCore;
using Skype4BizCore;


namespace s4bPOC
{
   class __ep__
   {
      static void Main(string[] args)
      {

         SkypeSniffer procMonitor = new SkypeSniffer();
         procMonitor.Run();

         RestApi restApi = new RestApi(procMonitor.SkypeObject);
         restApi.Start();


         while (true)
            Thread.Sleep(10);

         //string numIn = "+48883279496";
         /*if (!numIn.xIsTel())
            throw new Exception("BadTelNum");*/
         //string ext = "1111";
         //Guid pbxid = Guid.NewGuid();
         //procMonitor.FireInboundEvent("Ringing", numIn, ext, pbxid.ToString());

      }
   }
}
