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
			Thread t = new Thread(new ThreadStart(initWs));
			t.IsBackground = true;
			t.Start();
			//ws = initWebSocket();
			// init file
			l.logg("", false);
		}

		public WebSocket getWs()
		{
			return ws;
		}

		private void initWs()
		{
			ws = initWebSocket();
		}
		
		private WebSocket initWebSocket()
		{
			WebSocket ws = new WebSocket(Config.wsSource);


			ws.OnMessage += (sender, e) => {


				var resp = JsonConvert.DeserializeObject<dynamic>(e.Data);


				string type = resp.type;

				if ("authorize".Equals(type))
				{
					bool success = resp.success;
					l.log("new auth: " + success);
					if (success)
					{
						l.log("success auth");
						signinWindow.dispatchOpenMainWindow();
					}
					else
					{
						l.log("bad data for user");
					}
				}
				
				// пришло новое сообщение - отображаем в списке сообщений
				else if ("message".Equals(type))
				{
					l.log("new message");
					MessageResponse m = dynamicToMessageResponse(resp);
					while (mainWindow == null)
					{
						l.log("another sleep");
						Thread.Sleep(100);
					}
					mainWindow.dispatchShowMessage(m);
				}

				else if ("register".Equals(type))
				{
					l.log("registration answer");
					bool success = resp.success;
					if (success)
					{
						while (signupWindow == null)
						{
							l.log("another sleep");
							Thread.Sleep(100);
						}
						signupWindow.dispatchOpenSigninWindow();
					}
					else
					{
						l.log("user already exists");
					}
				}

			};
			// establish again
			ws.OnError += (sender, e) =>
			{
				l.log("error: " + e.Message);
				tryToConnect(ws);
			};

			tryToConnect(ws);
			return ws;
		}

		private void tryToConnect(WebSocket ws)
		{
			while (!ws.IsAlive)
			{
				// about 2 second to connect
				ws.Connect(); ;
			}
			l.log("ws connected");
		}

		private MessageResponse dynamicToMessageResponse(dynamic obj)
		{
			string from = obj.from;
			string type = obj.type;
			long   time = obj.time;
			string message = obj.message;
			return new MessageResponse(type, message, from, time);
		}
		
	}

	// only for checking response type
	public class CommonResponse
	{
		public string type;
	}

	public class RegRequest
	{
		public string type;
		public string user;
		public string email;
		public string password;
	}

	public class RegResponse
	{
		public string type;
		public bool success;
	}

	public class AuthResponse
	{
		public string type;
		public bool success;
		public string[] online;
	}

	public class AuthRequest
	{
		public string type;
		public string user;
		public string password;
	}

	public class MessageResponse
	{
		public MessageResponse() {}

		public MessageResponse(string type, string message, string @from, long time)
		{
			this.type = type;
			this.message = message;
			this.@from = @from;
			this.time = time;
		}

		public string type;
		public string message;
		public string from;
		// ms from 1970
		public long time;
	}
}
