
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
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
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
      //private string[] extsList = null;
      private string tenfoldExt = null;
      private string configFileName = "SkypeSniffer.config";
      private string currentUserName = null;
      private string currentUserSIP = null;
      private string currentIcomingUri = null;
      private List<string> eventBag = null;
      private string thisUser = null;
      private string statusFile = null;
      private string statusFileTemplate = null;


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
         this.thisUser = conf.AppSettings.Settings["thisUser"].Value;
         this.statusFile = conf.AppSettings.Settings["statusFile"].Value;
         this.statusFileTemplate = conf.AppSettings.Settings["statusFileTemplate"].Value;
         /* - - */
         this.tenfoldExt = conf.AppSettings.Settings["tenfoldExt"].Value;
         Console.WriteLine("ext: {0}".xFormat(this.tenfoldExt));
         /* set fields */
         this.eventBag = new List<string>();
      }

      public void Run()
      {
         try
         {
            /* attach to skype */
            this.lyncClient = mslm.LyncClient.GetClient();
            this.lyncClient.ConversationManager.ConversationAdded += this.IncomingConversationAdded;
         }
         catch (Exception x)
         {
            AppLogger.Save(x);
         }
      }

      private void IncomingConversationAdded(object sender,
         mslm.Conversation.ConversationManagerEventArgs e)
      {

         Console.WriteLine("\n\tIncomingConversationAdded...");
         
         /* clear event bag */
         this.eventBag.Clear();
         
         /* set user name & user sip */
         this.SetUserNameUserSIP(e.Conversation);
         this.UpdateStatusFile(0, "n/m", "IncomingConversationAdded", "n/m");

         /* monitor conv prop changes */
         e.Conversation.PropertyChanged += this.Conversation_PropertyChanged;
         this.ProcessConversation(e.Conversation);

      }


      private void ProcessConversation(mslm.Conversation.Conversation c)
      {
         mslm.Conversation.Participant p = c.Participants.Single(i => !i.IsSelf);
         this.currentIcomingUri = p.Contact.Uri;
         Console.WriteLine("incoming uri: {0}".xFormat(this.currentIcomingUri));
         string inuri = this.IncomingUri(c.Participants);
         string guid = Guid.NewGuid().ToString();
         string telnum = this.currentIcomingUri.Replace("tel:", "");
         this.FireEvent("Ringing", "Inbound", telnum, this.tenfoldExt, guid);
      }

      private void Conversation_PropertyChanged(object sender,
         mslm.Conversation.ConversationPropertyChangedEventArgs e)
      {
         Console.WriteLine("\n--- Conversation_PropertyChanged ---");
         mslm.Conversation.Conversation c = (mslm.Conversation.Conversation)sender;
         string msg = "p: {0}  /  v: {1} / s: {2}".xFormat(e.Property.ToString(), e.Value?.ToString(), c.State.ToString());
         this.eventBag.Add(msg);
         Console.WriteLine(msg);
         /* - - */
         /* inviter is self here */
         if (e.Property.ToString().Contains("Inviter"))
         {
            mslm.Contact contact = (e.Value as mslm.Contact);
            Console.WriteLine(" -> InviterUri: {0}".xFormat(contact.Uri));
         }
      }

      public string FireEvent(string eStatus, string eDirection, string callingNum, string tenfoldExt, string pbxID)
      {
         string responseText = null;
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
            webRequest.GetRequestStream().Write(bytes, 0, bytes.Length);
            //webRequest.GetRequestStream().Flush();
            webRequest.GetRequestStream().Close();
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
            responseText = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
            webResponse.Close();
         }
         catch (Exception x)
         {
            AppLogger.Save(x);
            responseText = "ERROR";
         }

         /* - - */
         return responseText;

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

      private string IncomingUri(IList<mslm.Conversation.Participant> participants)
      {
         foreach (mslm.Conversation.Participant p in participants)
         {
            Console.WriteLine(p.Contact.Uri);
         }
         return "";
      }

      public string Hangup(string num = null)
      {
         num = num ?? "CurrentConversation";
         this.UpdateStatusFile(0, "n/m", "Hangup", num);
         /* wierd it seems there are multiple convs */
         for (int i = 0; i < this.lyncClient.ConversationManager.Conversations.Count; i++)
         {
            mslm.Conversation.Conversation c = this.lyncClient.ConversationManager.Conversations[i];
            if (c != null)
            {
               c.End();
            }
         }

         num = num ?? this.currentIcomingUri;
         return this.FireEvent("Hangup", "Inbound", num, this.tenfoldExt, Guid.NewGuid().ToString());

      }

      public string Pickup()
      {
         try
         {
            for (int i = 0; i < this.lyncClient.ConversationManager.Conversations.Count; i++)
            {
               mslm.Conversation.Conversation c = this.lyncClient.ConversationManager.Conversations[i];
               if (c != null)
               {
                  Console.WriteLine("state: {0}".xFormat(c.State.ToString()));
                  switch (c.State)
                  {
                     case mslm.Conversation.ConversationState.Inactive:
                        c.AddParticipant(c.SelfParticipant.Contact);
                        break;
                     case mslm.Conversation.ConversationState.Active:
                        c.AddParticipant(c.SelfParticipant.Contact);
                        //c.AddParticipant(c.SelfParticipant.Contact.CreateContactEndpoint())
                        break;
                     default:
                        break;
                  }
               }
            }
         }
         catch (Exception x)
         {
            /* {"Access is denied. (Exception from HRESULT: 0x80070005 (E_ACCESSDENIED))"} */
            Debug.WriteLine(x.ToString());
         }

         return "";

      }

      public void Dial(string uri)
      {
         this.UpdateStatusFile(0, "n/m", "Dial", uri);
         Console.WriteLine("\tdialing: {0}".xFormat(uri));
         /* switch modes */
         this.lyncClient.ConversationManager.ConversationAdded -= this.IncomingConversationAdded;
         this.lyncClient.ConversationManager.ConversationAdded += this.OutgoingConversationAdded;
         mslm.Conversation.Conversation conv = this.lyncClient.ConversationManager.AddConversation();

      }

      private void OutgoingConversationAdded(object sender, mslm.Conversation.ConversationManagerEventArgs e)
      {
         this.UpdateStatusFile(0, "n/m", "OutgoingConversationAdded", "");
         //Conversation originated with remote SIP user
         if (e.Conversation.Modalities[mslm.Conversation.ModalityTypes.AudioVideo].State != mslm.Conversation.ModalityState.Notified)
         {
            if (e.Conversation.CanInvoke(mslm.Conversation.ConversationAction.AddParticipant))
            {
               e.Conversation.ParticipantAdded += OutgoingConversation_ParticipantAdded;
               //e.Conversation.AddParticipant(_LyncClient.ContactManager.GetContactByUri("bob@contoso.com"));
            }
         }
      }

      private void OutgoingConversation_ParticipantAdded(object sender,
         mslm.Conversation.ParticipantCollectionChangedEventArgs e)
      {
         Console.WriteLine("OutgoingConversation_ParticipantAdded");
         this.lyncClient.ConversationManager.ConversationAdded -= this.OutgoingConversationAdded;
         this.lyncClient.ConversationManager.ConversationAdded += this.IncomingConversationAdded;
      }

      /* {{"dts": "{0}", "errorCode": "{1}", "errorMsg": "{2}", "eventId": "{3}", "eventMsg": "{4}"}} */
      public void UpdateStatusFile(int erCode, string erMsg, string evId, string evMsg)
      {
         string dts = DateTime.UtcNow.ToString("yyyy/MM/d HH:mm:ss.ms");
         string jsonbuff = this.statusFileTemplate.xFormat(dts, erCode.ToString(), erMsg, evId, evMsg);
         using (StreamWriter sw = new StreamWriter(this.statusFile, false))
            sw.WriteLine(jsonbuff);
      }
   }
}
