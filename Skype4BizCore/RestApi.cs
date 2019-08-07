
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
         this.httpPrefix = "http://*:8090/";
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

            if (cntx.Request.RawUrl.Contains("/act/hangup"))
            {
                buffout = this.actHangup();
            }
            else if (cntx.Request.RawUrl.Contains("/act/mute"))
            {
                buffout = this.actMuteSelf();
            }

            else if (cntx.Request.RawUrl.Contains("/act/answer"))
            {
                buffout = this.actAnswer();
            }

            else if (cntx.Request.RawUrl.Contains("/act/transfer"))
            {
                buffout = this.actTransfer();
            }

            else if (cntx.Request.RawUrl.Contains("/act/makecall"))
            {
                buffout = this.actMakeCall(cntx.Request.RawUrl);
            }

            else if (cntx.Request.RawUrl.Contains("/act/hold"))
            {
                buffout = this.actHold();
            }

            else
            {
                buffout = "Error!!!";
            }
            /* - - */
            this.PushRespBuff(buffout, ref cntx);
        }

        private string actMakeCall(string rowurl)
        {
            string uri = rowurl.Replace("/act/makecall/", "tel:+13475147298");
            this.skypeSniffer.MakeCall(uri);
            return "OK";
        }

        private string actHangup()
        {
            this.skypeSniffer.Hangup();
            return "OK";
        }

        private string actMuteSelf()
        {
            this.skypeSniffer.MuteSelf();
            return "OK";
        }

        private string actAnswer()
        {
            this.skypeSniffer.Answer();
            return "OK";
        }

        private string actTransfer()
        {
            this.skypeSniffer.Transfer();
            return "OK";
        }
        private string actHold()
        {
            this.skypeSniffer.Hold();
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
