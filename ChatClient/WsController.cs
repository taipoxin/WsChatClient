using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace ChatClient
{
	public class WsController
	{
		// core low level ws client
		private WebSocket ws;
		private FileLogger l = new FileLogger(Config.logFileName);

		// links for view classes

		private Signin signinWindow;
		private Signup signupWindow;
		private MainWindow mainWindow;

		public void setSigninWindow(Signin w)
		{
			signinWindow = w;
		}

		public void setSignupWindow(Signup w)
		{
			signupWindow = w;
		}

		public void setMainWindow(MainWindow w)
		{
			mainWindow = w;
		}


		/// <summary>
		/// initialise webSocket in another thread
		/// </summary>
		public WsController()
		{
			Thread t = new Thread(new ThreadStart(initWs));
			t.IsBackground = true;
			t.Start();
		}

		public WebSocket getWs()
		{
			return ws;
		}

		private void initWs()
		{
			ws = initWebSocket();
		}
		
		/// <summary>
		/// initialise webSocket client with all configured listeners
		/// </summary>
		/// <returns>ws object</returns>
		private WebSocket initWebSocket()
		{
			WebSocket ws = new WebSocket(Config.wsSource);


			ws.OnMessage += (sender, e) =>
			{


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
				// рендерит в MainWindow список каналов
				else if ("get_channel".Equals(type))
				{
					JArray array = resp.channels;
					List<dynamic> ll = array.ToObject<List<dynamic>>();
					l.log("received channels:  " + ll.ToArray().ToString());
					mainWindow.dispatchShowChannels(ll);
				}

				else if ("new_channel".Equals(type))
				{
					bool success = resp.success;
					string n = resp.name;
					if (success)
					{
						l.log("nice channel creation: " + n);
						NewChannelResponse m = dynamicToNewChannelResponse(resp);
						mainWindow.dispatchCreateChannel(m);
					}
					else
					{ 
						l.log("bad channel creation: " + n);
					}
				}
				else if ("get_channel_messages".Equals(type))
				{
					JArray array = resp.messages;
					List<dynamic> ll = array.ToObject<List<dynamic>>();
					string ch = resp.channel;
					l.log("received messages for channel " + ch + " are " + array.ToString());
					mainWindow.dispatchShowChannelMessages(ch, ll);
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
			string channel = obj.channel;
			return new MessageResponse(type, message, from, time, channel);
		}

		private NewChannelResponse  dynamicToNewChannelResponse(dynamic obj)
		{
			var ch = new NewChannelResponse();
			ch.name = obj.name;
			ch.fullname = obj.fullname;
			ch.admin = obj.admin;
			ch.success = obj.success;
			ch.type = obj.type;
			return ch;
		}

	}

	// only for checking response type
	public class CommonResponse
	{
		public string type;
	}


	public class NewChannelRequest
	{
		public string name;
		public string fullname;
		public string admin;
		public string type;
	}

	public class NewChannelResponse
	{
		public string name;
		public string fullname;
		public string admin;
		public string type;
		public bool success;
	}

	public class GetChannelMessagesReq
	{
		public string from;
		public string channel;
		// "get_channel_messages"
		public string type;
	}


	public class ChannelRequest
	{
		public string type;
		// if name == '*' return all channels
		public string name;
		public string from;
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

		public MessageResponse(string type, string message, string @from, long time, string channel)
		{
			this.type = type;
			this.message = message;
			this.@from = @from;
			this.time = time;
			this.channel = channel;
		}

		public string from;
		public string message;
		public string channel;
		// ms from 1970
		public long time;
		public string type;

	}
}
