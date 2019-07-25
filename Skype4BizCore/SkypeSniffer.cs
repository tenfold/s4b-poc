
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Web;
using System.Configuration;
using System.Diagnostics;
using mslm = Microsoft.Lync.Model;
/* core */
using SysCore;


namespace Skype4BizCore
{
   public class SkypeSniffer
   {

      private mslm.LyncClient lyncClient = null;
      private string orgID = null;
      private string eventSinkUrl = null;
      private string eventTemplate = null;
      private string[] extsList = null;
      private string configFileName = "SkypeSniffer.config";
      private string currentUserName = null;
      private string currentUserSIP = null;
      private List<string> eventBag = null;


      public SkypeSniffer()
      {
         this.Init();
      }

      private void Init()
      {
         /* load config */
         ExeConfigurationFileMap dllConfiguration = new ExeConfigurationFileMap();
         dllConfiguration.ExeConfigFilename = this.configFileName;
         Configuration conf = ConfigurationManager.OpenMappedExeConfiguration(dllConfiguration, ConfigurationUserLevel.None);
         this.orgID = conf.AppSettings.Settings["orgID"].Value;
         Console.WriteLine("orgid: {0}".xFormat(this.orgID));
         this.eventSinkUrl = conf.AppSettings.Settings["eventSinkUrl"].Value;
         this.eventTemplate = conf.AppSettings.Settings["eventTemplate"].Value;
         string tmp = conf.AppSettings.Settings["extsList"].Value;
         this.extsList = tmp.Split(new char[] { ';', ',' });
         foreach (string s in this.extsList)
            Console.WriteLine("ext: {0}".xFormat(s));
         /* set fields */
         this.eventBag = new List<string>();
      }

      public void Run()
      {
         try
         {
            /* attach to skype */
            this.lyncClient = mslm.LyncClient.GetClient();
            this.lyncClient.ConversationManager.ConversationAdded += this.onConversationAdded;
         }
         catch (Exception x)
         {
            AppLogger.Save(x);
         }
      }

      private void onConversationAdded(object sender,
         mslm.Conversation.ConversationManagerEventArgs e)
      {
         /* clear event bag */
         this.eventBag.Clear();
         /* monitor conv prop changes */
         e.Conversation.PropertyChanged += this.Conversation_PropertyChanged;
         /* set user name & user sip */
         this.SetUserNameUserSIP(e.Conversation);

         string num = e.Conversation.Participants[1].Contact.Uri.Split(':')[1];
         Console.WriteLine("incoming call: {0}".xFormat(num));

         string guid = Guid.NewGuid().ToString();
         string ext = this.extsList[0];
         this.FireEvent("Ringing", "Inbound", num, ext, guid);

         Thread.Sleep(4000);
         this.FireEvent("Hangup", "Inbound", num, ext, guid);

         /*foreach (var p in e.Conversation.Participants)
         {
            Console.WriteLine(p.Contact.Uri);
            Debug.WriteLine(p.Contact.Uri);
         }*/

      }

      private void Conversation_PropertyChanged(object sender,
         mslm.Conversation.ConversationPropertyChangedEventArgs e)
      {
         string msg = "p: {0}  /  v: {1}".xFormat(e.Property.ToString(), e.Value?.ToString());
         this.eventBag.Add(msg);
         Console.WriteLine(msg);
      }

      public int FireEvent(string eStatus, string eDirection, string callingNum, string tenfoldExt, string pbxID)
      {
         try
         {
            /* - "Inbound" - */
            /* '{{"status": "{0}", "direction": "{1}", "number": "{2}", "extension": "{3}", "pbxCallId": "{4}"}}' */
            string jbuff = this.eventTemplate.xFormat(eStatus, eDirection, callingNum, tenfoldExt, pbxID);
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

         /* - - */
         return 0;

      }

      private void SetUserNameUserSIP(mslm.Conversation.Conversation c)
      {
         mslm.Conversation.ParticipantProperty name = mslm.Conversation.ParticipantProperty.Name;
         this.currentUserName = (string)c.SelfParticipant.Properties[name];
         this.currentUserSIP = c.SelfParticipant.Contact.Uri;
         
      }

      public mslm.LyncClient SkypeObject
      {
         get { return this.lyncClient; }
      }
   }
}
