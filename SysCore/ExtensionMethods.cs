
using System;
using System.Text;
using System.Text.RegularExpressions;


namespace SysCore
{
   static public class ExtensionMethods
   {
      public static string xf(this String str, params string[] args)
      {
         return String.Format(str, args);
      }

      public static bool istel(this String str, string local = "US")
      {

         return true;
      }

      public static byte[] tobytes(this String str)
      {
         return Encoding.UTF8.GetBytes(str);
      }
   }
}
