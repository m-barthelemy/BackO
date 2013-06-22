using System;
using System.Collections;

namespace Node{
	public class HubWaiter{
		
		private Hashtable waitList;
		//private KeyValuePair<string, string> waitList;
		internal HubWaiter (){
			waitList = new Hashtable();
		}
		
		internal  string WaitFor(string message){
			waitList.Add(message, null);
			//waitList[message] == null;
			while(waitList[message] == null)	
				System.Threading.Thread.Sleep(100);
			return (string)waitList[message];
		}
		
		internal bool AddReceived(string sKey, string sValue){
			lock(waitList){
				if(waitList.ContainsKey(sKey)){
				   	waitList[sKey] = sValue;
					return true;
				}	
			}
			return false;
		}
		
		
	}
}

