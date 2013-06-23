using System;

namespace Backo.Api.WebServices {

	public class MainAppClass {

		public static void Main (string[] args){

			WSRunner webservicesHost = new WSRunner();
			webservicesHost.Start();

		}
	}
}

