using System;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;
//using ServiceStack.DataAccess.Criteria;

namespace P2PBackup.Common {
	public class Password {

		public int Id{get;set;}

		//[StringLength(32768)] // using Mysql the value field would be limited to 255, not enough to store base64 encrypted password!
		public string Value{get;set;}

		public Password ()
		{
		}
	}
}

