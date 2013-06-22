using System;

namespace P2PBackup.Common {

	public class MailParameters{

		public int BackupSetId{get;set;}
		public string From{get;set;}
		public string To{get;set;}
		public string Subject{get;set;}
		public string Body{get;set;}
		public bool IsHtml{get;set;}

		public MailParameters(){
			this.From = null;
			this.To = null;
			this.Subject = null;
			this.Body = null;
			this.IsHtml = false;
		}
	}
}

