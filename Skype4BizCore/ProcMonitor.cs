
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Web;
using mslm = Microsoft.Lync.Model;


namespace Skype4BizCore
{
   public class ProcMonitor
   {

      private mslm.LyncClient lyncClient = null;
      private mslm.Conversation.ConversationManager conversationManager = null;

      public ProcMonitor()
      {
         this.Init();
      }

      private void Init()
      {
         this.lyncClient = mslm.LyncClient.GetClient();
         this.lyncClient.ConversationManager.ConversationAdded += this.onConversationAdded;
      }

      public void Run()
      {

      }

      private void onConversationAdded(object sender,
         mslm.Conversation.ConversationManagerEventArgs e)
      {
         e.Conversation.PropertyChanged += Conversation_PropertyChanged;
         //IList<mslm.Conversation.Participant> participants
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

      private int FireEvent(string eventName, string num, params string[] args)
      {
         
         /*
            url -H ‘Content-Type: application/json' -XPOST 
            https://events.qa.tenfold.com/receive/5d1cbe0ad3c02b0007ab3ba1/phone-simulator 
            -d ‘{"status": "Ringing","direction":"Inbound","number":"'$CALLER_PHONE'","extension":"’<SKYPE_NUMBER’>",
                  "pbxCallId":"'<UNIQUE_CALL_ID>'"}'
          */
         string url = "https://events.qa.tenfold.com/receive/5d1cbe0ad3c02b0007ab3ba1/phone-simulator";
         WebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
         webRequest.Method = "post";

         //webRequest.GetResponse();

         return 0;
      }
   }
}
