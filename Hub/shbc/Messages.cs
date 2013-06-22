using System;

namespace shbc
{
	public static class Messages
	{


		public static string GetMessage(int code, string arg1, string arg2){
			switch(code){

			// 'notice' messages
			case 601:
				return string.Format("Error creating or using session #{0}", arg1);
			// informational messages
			case 701:
				return "Snapshotting volumes";
			case 702:
				return string.Format("Processing {0} {1}", arg1, arg2);
			case 704:
				return "Processing Index";
			case 705:
				return string.Format ("Sent index, size {0} MB", arg1);
			case 706:
				return "Created synthetic index";
			case 707:
				return "Task done.";
			case 710:
				return string.Format ("Pre-task command output for {0} : {1}", arg1, arg2);
			case 711:
				return string.Format ("Post-task command output for {0} : {1}", arg1, arg2);
			case 790:
				return "Task cancelling due to hub request";
			case 791:
				return string.Format("Could not start task , will retry during {0} mn", arg1);
	
			// errors
			case 800:
				return "Node is already busy, cannot accept new task";
			case 801:
				return "Task couldn't be started and has expired.";
			case 802:
				return string.Format ("Error trying to access '{0}'.", arg1);
			case 805:
				return string.Format ("Unable to snapshot volume(s) '{0}' : {1}", arg1, arg2);
			case 806:
				return "No storage space available";
			case 809:
				return string.Format ("Couldn't save Deduplication database '{0}' : {1}", arg1, arg2);
			case 810:
				return string.Format ("Could not find index for task {0}", arg1);
			case 820:
				return string.Format("Error freezing application component : {0}  {1}", arg1, arg2 );
			case 830:
				return "Node is overquota";
			case 899:
				return string.Format ("Unexpected error, task {0} : {1}", arg1, arg2);

			//warnings
			case 901:
				return string.Format ("Could not start task , will retry during {0} mn", arg1);
			case 903:
				return string.Format ("File {0} {1} has changed during backup", arg1, arg2);
			case 906:
				return string.Format ("Unable to delete snapshot {0} : {1}", arg1, arg2);
			case 911:
				return string.Format ("Requested path {0} doesn't exist ", arg1);
			case 912:
				return string.Format ("Error reading item {0}: {1} ", arg1, arg2);
			case 999:
				return string.Format ("Unexpected warning, task {0} : {1}", arg1, arg2);
			default:
				return "<Unknown code>"+code;
				break;

			}

				
		}
	}
}

