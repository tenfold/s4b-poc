
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using mslm = Microsoft.Lync.Model;
using SysCore;


namespace Skype4BizCore
{
   public class RestApi
   {

      private HttpListener httpListener = null;
      private string httpPrefix = null;
      private mslm.LyncClient lyncClient = null;
      private SkypeSniffer skypeSniffer = null;

      public RestApi(SkypeSniffer skype)
      {
         this.skypeSniffer = skype;
         this.httpPrefix = "http://*:8080/";
      }

      public void Start()
      {
         this.httpListener = new HttpListener();
         this.httpListener.Prefixes.Add(this.httpPrefix);
         this.httpListener.Start();
         /* - - */
         while (true)
         {
            Console.WriteLine("awaiting request...");
            IAsyncResult r = this.httpListener.BeginGetContext(this.ProcessRequest, this.httpListener);
            r.AsyncWaitHandle.WaitOne();
         }
      }

      private void ProcessRequest(IAsyncResult result)
      {
         string buffout = null;
         HttpListenerContext cntx = this.httpListener.EndGetContext(result);
         Console.WriteLine(" -> {0}".xFormat(cntx.Request.RawUrl));
         /* */
         if (cntx.Request.RawUrl.Contains("/act/pickup"))
         {
            buffout = this.actPickup();
         }
         else if (cntx.Request.RawUrl.Contains("/act/dial"))
         {
            buffout = this.actDial(cntx.Request.RawUrl);
         }
         else if (cntx.Request.RawUrl.Contains("/act/hangup"))
         {
            buffout = this.actHangup();
         }
         else
         {
            buffout = "Error!!! :)";
         }
         /* - - */
         this.PushRespBuff(buffout, ref cntx);
      }

      private string actPickup()
      {
         this.skypeSniffer.Pickup();
         return "OK";
      }

      private string actDial(string rowurl)
      {
         string uri = rowurl.Replace("/act/dial/", "");
         this.skypeSniffer.Dial(uri);
         return "OK";
      }

      private string actHangup()
      {
         this.skypeSniffer.Hangup();
         return "OK";
      }

      private void PushRespBuff(string buff, ref HttpListenerContext cntx)
      {
         cntx.Response.ContentType = "text/plain";
         byte[] bytes = buff.xToBytes();
         cntx.Response.ContentLength64 = bytes.LongLength;
         cntx.Response.OutputStream.Write(bytes, 0, bytes.Length);
         cntx.Response.OutputStream.Flush();
         cntx.Response.OutputStream.Close();
      }
   }
}
