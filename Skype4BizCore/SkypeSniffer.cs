
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
using Microsoft.Lync.Model.Extensibility;
using Microsoft.Lync.Model.Conversation;

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
        private IList<mslm.Conversation.Conversation> conversations = null;


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
            var conversation = e.Conversation;

            Console.WriteLine("\n\tIncomingConversationAdded...");

            this.SetUserNameUserSIP(e.Conversation);

            this.UpdateStatusFile(0, "n/m", "IncomingConversationAdded", "n/m");

            /* monitor conv prop changes */
            e.Conversation.PropertyChanged += this.Conversation_PropertyChanged;
            this.ProcessConversation(e.Conversation);
        }

        private void ProcessConversation(mslm.Conversation.Conversation c)
        {
            var participants = c.Participants.Where(i => !i.IsSelf);

            var participant = participants.FirstOrDefault();
            this.currentIcomingUri = participant.Contact.Uri;
            Console.WriteLine("incoming uri: {0}".xFormat(this.currentIcomingUri));
            string inuri = this.IncomingUri(c.Participants);
            string guid = Guid.NewGuid().ToString();
            string telnum = this.currentIcomingUri.Replace("tel:", "");
            // this.FireEvent("Ringing", "Inbound", telnum, this.tenfoldExt, guid);
        }


        private void Conversation_PropertyChanged(object sender,
           mslm.Conversation.ConversationPropertyChangedEventArgs e)
        {
            Console.WriteLine("\n--- Conversation_PropertyChanged ---");
            mslm.Conversation.Conversation c = (mslm.Conversation.Conversation)sender;

            /* - - */
            /* inviter is self here */
            if (e.Property.ToString().Contains("Inviter"))
            {
                mslm.Contact contact = (e.Value as mslm.Contact);
                Console.WriteLine(" -> InviterUri: {0}".xFormat(contact.Uri));
            }
        }

        private void SetUserNameUserSIP(mslm.Conversation.Conversation c)
        {
            mslm.Conversation.ParticipantProperty name = mslm.Conversation.ParticipantProperty.Name;
            this.currentUserName = (string)c.SelfParticipant.Properties[name];
            this.currentUserSIP = c.SelfParticipant.Contact.Uri;
        }

        /// <summary>
        /// Terminate the call
        /// </summary>
        /// <param name="num"></param>
        public void Hangup(string num = null)
        {
            conversations = this.lyncClient.ConversationManager.Conversations;
            num = num ?? "CurrentConversation";
            this.UpdateStatusFile(0, "n/m", "Hangup", num);
            /* wierd it seems there are multiple convs */
            for (int i = 0; i < conversations.Count; i++)
            {
                mslm.Conversation.Conversation c = conversations[i];
                if (c != null)
                {
                    c.End();
                }
            }
            num = num ?? this.currentIcomingUri;
            //return this.FireEvent("Hangup", "Inbound", num, this.tenfoldExt, Guid.NewGuid().ToString());

        }

        /// <summary>
        /// Mute user side of the conversation
        /// </summary>
        public void MuteSelf()
        {

            conversations = this.lyncClient.ConversationManager.Conversations;

            try
            {
                if (conversations.Count != 0)
                {
                    for (int i = 0; i < conversations.Count; i++)
                    {
                        mslm.Conversation.Conversation c = conversations[i];
                        mslm.Conversation.Participant p = c.Participants.Single(k => k.IsSelf);

                        if (p != null && !p.IsMuted)
                        {
                            p.BeginSetMute(true, null, null);
                        }

                        else if (p != null && p.IsMuted)
                        {
                            p.BeginSetMute(false, null, null);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Accept the incoming ('ringing') conversation
        /// </summary>
        /// <returns></returns>
        public string Answer()
        {
            conversations = this.lyncClient.ConversationManager.Conversations;
            try
            {
                for (int i = 0; i < conversations.Count; i++)
                {
                    mslm.Conversation.Conversation c = conversations[i];
                    if (c != null)
                    {
                        c.Modalities[mslm.Conversation.ModalityTypes.AudioVideo].Accept();
                    }
                }
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.ToString());
            }

            return "";
        }

        /// <summary>
        /// Dial to a live US number via Skype
        /// </summary>
        /// <param name="uri"></param>
        public void MakeCall(string uri)
        {
            //hardcoded for test purposes
            uri = "tel:+13475147298";
            List<string> participant = new List<string>();
            participant.Add(uri);
            Console.WriteLine("\tdialing: {0}".xFormat(uri));
            var automation = mslm.LyncClient.GetAutomation();
            automation.BeginStartConversation(AutomationModalities.Audio, participant, null, null, automation);

        }

        /// <summary>
        /// Hold and Retrieve the on hold conversation
        /// </summary>
        public void Hold()
        {
            conversations = this.lyncClient.ConversationManager.Conversations;

            for (int i = 0; i < conversations.Count; i++)
            {
                mslm.Conversation.Conversation c = conversations[i];
                if (c != null)
                {
                    if (c.Modalities[ModalityTypes.AudioVideo].State == ModalityState.OnHold)
                    {
                        object[] asyncState = { c.Modalities[ModalityTypes.AudioVideo], "RETRIEVE" };
                        c.Modalities[ModalityTypes.AudioVideo].BeginRetrieve(null, asyncState);
                    }

                    else if (c.Modalities[ModalityTypes.AudioVideo].State == ModalityState.Connected)
                    {
                        object[] asyncState = { c.Modalities[ModalityTypes.AudioVideo], "HOLD" };
                        c.Modalities[ModalityTypes.AudioVideo].BeginHold(null, asyncState);
                    }
                }
            }

        }

        public void UpdateStatusFile(int erCode, string erMsg, string evId, string evMsg)
        {
            string dts = DateTime.UtcNow.ToString("yyyy/MM/d HH:mm:ss.ms");
            string jsonbuff = this.statusFileTemplate.xFormat(dts, erCode.ToString(), erMsg, evId, evMsg);
            using (StreamWriter sw = new StreamWriter(this.statusFile, false))
                sw.WriteLine(jsonbuff);
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
    }
}
