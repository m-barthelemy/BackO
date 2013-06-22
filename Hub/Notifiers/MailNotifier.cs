using System;
using System.Net.Mail;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackupHub.Utilities;

namespace P2PBackupHub.Notifiers {
	
	public class MailNotifier:INotifier {
		
		public MailNotifier() {
		}
		
		public void Fire(Task t){
			
			SmtpClient smtp = new SmtpClient();
			smtp.Host = ConfigurationManager.AppSettings["Notifiers.Mail.Host"];
			/*MailParameters paramz = (new DBHandle()).GetMailConfiguration(t.BackupSet.Id, t.RunStatus);
			if(paramz.To == null)
				return;
			MailAddress from = new MailAddress(paramz.From);
			MailAddress to = new MailAddress(paramz.To);
			MailMessage mail = new MailMessage(from, to);
			mail.Headers.Add("Message-ID", t.Id.ToString());
			mail.Headers.Add("In-Reply-To", t.Id.ToString());
			mail.Subject = BuildBody(paramz.Subject, t);
			
			mail.IsBodyHtml = false;
			mail.Body = BuildBody(paramz.Body, t);
			try{
				smtp.Send(mail);
			}
			catch(Exception e){
				Logger.Append("HUBRN", Severity.ERROR, "Could not send email to '"+paramz.To+"' about task "+t.Id+": "+e.Message);
			}*/
		}
	
		private string BuildBody(string bodyTemplate, Task t){
			Regex r = new Regex(@"{((\w+(\.?)))*}",  RegexOptions.IgnoreCase);
			MatchCollection mc = r.Matches(bodyTemplate);
			foreach(Match m in mc){
				string rawExpr = m.Groups[0].Value;
				string usefulExpr = m.Groups[0].Value.Trim(new char[]{'{','}'});
				string[] members = usefulExpr.Split('.');
				string wantedValue = String.Empty;
				int curIndex = 0;
				if(members[0].ToLower() == t.GetType().Name)
					curIndex++;
				System.Reflection.PropertyInfo pi = null;
				Object o = t;
				for(int i=curIndex; i<members.Length; i++){
					if(pi == null)
						o = t;
					else
						o = pi.GetValue(o, null);
					pi = o.GetType().GetProperty(members[i]);
					//Console.WriteLine("member="+members[i]);
					try{
						//if(o.GetType().GetInterface("System.Collections.ICollection") != null){ //  .FindInterfaces(myFilter, myInterfaceList).Length > 0){
						if(typeof(System.Collections.ICollection).IsAssignableFrom(pi.GetValue(o, null).GetType()) && pi.GetValue(o, null).GetType().IsGenericType){
							//Console.WriteLine("found ienumerable : "+pi.GetValue(o, null).GetType().ToString());
							//ICollection<Object> listObject =  Convert.ChangeType(pi.GetValue(o, null), System.Collections.Generic.ICollection);
							var list = (System.Collections.IList)pi.GetValue(o,null);
								//Console.WriteLine("casted to icollection");
	    						foreach(Object obj in list){
								try{
									wantedValue += obj.GetType().GetProperty(members[i+1]).GetValue(obj, null).ToString()+", ";
								}
								catch{ // list object property is null
									wantedValue += "<NOTFOUND>, ";
								}
							}
						}
					}
					catch(Exception _e){
						Console.WriteLine("mail.BuildBody() failure: "+_e.Message+"\r\n"+_e.StackTrace);
					}
				}
				
				
				if(wantedValue == String.Empty && pi != null && pi.GetValue(o, null)!= null)
						wantedValue = pi.GetValue(o, null).ToString();
				else if(wantedValue == String.Empty && pi == null)
					wantedValue = "{"+usefulExpr+":<NOTFOUND>}";
				bodyTemplate = bodyTemplate.Replace(rawExpr, wantedValue);
				Console.WriteLine("value of "+usefulExpr+"="+wantedValue);
			}
			
			return bodyTemplate;
		}
					
		public static bool InterfaceFilterCallback(System.Type type, object criteria) {
		        foreach (System.Type interfaceName in (System.Type[])criteria){
		            if (type.IsInstanceOfType(interfaceName)){
		                return (true);
		            }
		        }
		
		    	return (false);
		}
					
	}
	

}

