
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Web;
using System.Configuration;
using mslm = Microsoft.Lync.Model;
/* core */
using SysCore;


namespace Skype4BizCore
{
   public class SkypeWatcher
   {

      private mslm.LyncClient lyncClient = null;
      //private mslm.Conversation.ConversationManager conversationManager = null;
      private string orgID = null;
      private string eventSinkUrl = null;
      private string eventTemplate = null;
      private string[] extsList = null;


      public SkypeWatcher()
      {
         this.Init();
      }

      private void Init()
      {
         this.lyncClient = mslm.LyncClient.GetClient();
         this.lyncClient.ConversationManager.ConversationAdded += this.onConversationAdded;
         ExeConfigurationFileMap dllConfiguration = new ExeConfigurationFileMap();
         dllConfiguration.ExeConfigFilename = "SkypeWatcher.config";
         Configuration conf = ConfigurationManager.OpenMappedExeConfiguration(dllConfiguration, ConfigurationUserLevel.None);
         this.orgID = conf.AppSettings.Settings["orgID"].Value;
         this.eventSinkUrl = conf.AppSettings.Settings["eventSinkUrl"].Value;
         this.eventTemplate = conf.AppSettings.Settings["eventTemplate"].Value;
         string tmp = conf.AppSettings.Settings["extsList"].Value;
         this.extsList = tmp.Split(new char[] { ';', ',' });
      }

      public void Run()
      {

      }

      private void onConversationAdded(object sender,
         mslm.Conversation.ConversationManagerEventArgs e)
      {
         e.Conversation.PropertyChanged += Conversation_PropertyChanged;
         foreach (var p in e.Conversation.Participants)
         {
            Console.WriteLine(p.Contact.Uri);
         }
      }

      private static void Conversation_PropertyChanged(object sender,
         mslm.Conversation.ConversationPropertyChangedEventArgs e)
      {
         Console.WriteLine(e.Property.ToString());
         Console.WriteLine(e.Value);
      }

      public int FireInboundEvent(string eventName, string numIn, string skypeNum, string pbxID)
      {
         try
         {
            /* - - */
            string jbuff = this.eventTemplate.xFormat(eventName, "Inbound", numIn, skypeNum, pbxID);
            byte[] bytes = jbuff.xToBytes();
            string eventurl = this.eventSinkUrl.xFormat(this.orgID);
            WebRequest webRequest = (HttpWebRequest)WebRequest.Create(eventurl);
            webRequest.Method = "post";
            webRequest.ContentType = "application/json";
            webRequest.ContentLength = bytes.LongLength;
            Stream outstream = webRequest.GetRequestStream();
            outstream.Write(bytes, 0, bytes.Length);
            outstream.Flush();
            outstream.Close();
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
            string inbuff = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();

         }
         catch (Exception x)
         {
            AppLogger.Save(x);
         }

         return 0;

      }
   }
}
