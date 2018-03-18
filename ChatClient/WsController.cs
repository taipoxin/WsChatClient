using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using WebSocketSharp;

namespace ChatClient
{
	public class WsController
	{
		private WebSocket ws;
		private FileLogger l = new FileLogger("ws.txt");

		private Signin signinWindow;
		private Signup signupWindow;
		private TestWindow mainWindow;

		public void setSigninWindow(Signin w)
		{
			signinWindow = w;
		}

		public void setSignupWindow(Signup w)
		{
			signupWindow = w;
		}

		public void setMainWindow(TestWindow w)
		{
			mainWindow = w;
		}



		public WsController()
		{
			ws = initWebSocket();
			// init file
			l.log("", false);
		}

		public WebSocket getWs()
		{
			return ws;
		}
		

		
		private WebSocket initWebSocket()
		{
			WebSocket ws = new WebSocket(Config.wsSource);
			ws.OnMessage += (sender, e) => {
				AuthResponse resp = JsonConvert.DeserializeObject<AuthResponse>(e.Data);
				if ("authorize".Equals(resp.type))
				{
					// успешная авторизация
					if (resp.success)
					{
						l.log("success auth");
						signinWindow.dispatchOpenMainWindow();
					}
				}
			};

			ws.Connect();
			return ws;
		}
		
	}
}
