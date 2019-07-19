
using System;
using System.Text;
using System.Text.RegularExpressions;


namespace SysCore
{
   static public class ExtensionMethods
   {
      public static string xFormat(this String str, params string[] args)
      {
         return String.Format(str, args);
      }

      public static bool xIsTel(this String str, string local = "US")
      {

         return true;
      }

      public static byte[] xToBytes(this String str)
      {
         return Encoding.UTF8.GetBytes(str);
      }
   }
}
