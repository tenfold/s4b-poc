
using System;
using System.Collections.Generic;
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

      public RestApi(mslm.LyncClient lyncClient)
      {
         this.lyncClient = lyncClient;
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
         switch (cntx.Request.RawUrl.Split('?')[0].Trim('/'))
         {
            case "act/pickup":
               buffout = this.actPickup(ref cntx);
               break;
            case "act/dial":
               buffout = this.actDial(ref cntx);
               break;
            case "act/hangup":
               buffout = this.actHandup(ref cntx);
               break;
            default:
               break;
         }
         /* - - */
         cntx.Response.ContentType = "text/plain";
         byte[] bytebuff = buffout.xToBytes();
         cntx.Response.ContentLength64 = bytebuff.LongLength;
         cntx.Response.OutputStream.Write(bytebuff, 0, bytebuff.Length);
         cntx.Response.OutputStream.Flush();
         cntx.Response.OutputStream.Close();
      }

      private string actPickup(ref HttpListenerContext cntx)
      {
         //this.lyncClient.ConversationManager.Conversations.ad
         return "OK";
      }

      private string actDial(ref HttpListenerContext cntx)
      {
         string num = cntx.Request.QueryString["num"];
         Console.WriteLine("dial: {0}".xFormat(num));
         return "OK";
      }

      private string actHandup(ref HttpListenerContext cntx)
      {
         if (this.lyncClient.ConversationManager.Conversations.Count == 0)
            this.lyncClient.ConversationManager.Conversations[0].End();
         return "OK";
      }
   }
}
