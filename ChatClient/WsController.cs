using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using LiteDB;
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
					// show
					mainWindow.dispatchShowMessage(m);

					// serialise
					string channelName = m.channel;
					var ent = new MessageEntity { from = m.@from, message = m.message, time = m.time};
					using (var db = new LiteDatabase(@"LocalData.db"))
					{
						var messagesCollection = db.GetCollection<MessageEntity>(channelName + "_mes");
						messagesCollection.Insert(ent);
					}

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
					JArray channels = resp.channels;
					List<dynamic> chList = channels.ToObject<List<dynamic>>();
					l.log("received channels:  " + channels.ToString());

					JArray userCounts = resp.user_counts;
					List<Int32> counts = userCounts.ToObject<List<Int32>>();
					l.log("received counts: " + userCounts.ToString());
					mainWindow.dispatchShowChannels(chList, counts);
				}

				else if ("new_channel".Equals(type))
				{
					NewChannelResponse m = dynamicToNewChannelResponse(resp);
					mainWindow.dispatchCreateChannel(m);
				}
				else if ("get_channel_messages".Equals(type))
				{
					JArray array = resp.messages;
					List<dynamic> ll = array.ToObject<List<dynamic>>();
					string ch = resp.channel;

					l.log("received messages for channel " + ch + " are " + array.ToString());
					// show
					mainWindow.dispatchShowChannelMessages(ch, ll);
					
					// serialise
					var entities = listDynamicToMessageEntities(ll);
					try
					{
						using (var db = new LiteDatabase(@"LocalData.db"))
						{
							var messagesCollection = db.GetCollection<MessageEntity>(ch + "_mes");
							messagesCollection.EnsureIndex(x => x.time);
							messagesCollection.Insert(entities);
						}
					}
					catch (LiteException)
					{
						l.log("exception in serialise (invalid ch format probably)");
					}
				}
				else if ("get_online_users".Equals(type))
				{
					JArray array = resp.users;
					GetOnlineUsers obj = new GetOnlineUsers();
					obj.sender = resp.sender;
					obj.users = array.ToObject<List<string>>();
					obj.type = resp.type;
					mainWindow.dispatchGetOnlineUsers(obj);
				}

				else if ("add_user".Equals(type))
				{
					AddUser evnt = new AddUser();
					evnt.sender = resp.sender;
					evnt.user = resp.user;
					evnt.channel = resp.channel;
					evnt.success = resp.success;
					evnt.type = resp.type;
					
					
					if (evnt.sender == Config.userName)
					{
						// notify about successful or not adding	
						// display info about adding (using add user menu)
						mainWindow.dispatchNotifyOnAddingUserResponse(evnt.user, evnt.channel, evnt.success);
					}
					// evnt.success apriori true here
					else
					{
						// user have been added
						if (evnt.user == Config.userName)
						{
							// show new channel
							mainWindow.getChannelRequest(evnt.channel);
						}
						// other members of channel (exclude admin)
						else
						{
							// update channel data
							// +1 channel's members displayed
							mainWindow.dispatchIncrementChannelMembersView(evnt.channel);
						}
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

		private List<MessageEntity> listDynamicToMessageEntities(List<dynamic> ll)
		{
			List<MessageEntity> entities = new List<MessageEntity>(ll.Capacity);
			foreach (dynamic m in ll)
			{
				string fr = m.from;
				string mes = m.message;
				long t = m.time;
				var mess = new MessageEntity { from = fr, message = mes, time = t };
				entities.Add(mess);
			}
			return entities;
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

	public class MessageEntity
	{
		public Guid Id { get; set; }
		public string from { get; set; }
		public string message { get; set; }
		public long time { get; set; }
	}

	//unviversal (for req(w/o success) and resp(w/o  fullname))
	public class AddUser
	{
		public AddUser() {}
		public AddUser(string sender, string user, string channel, string fullname, bool success, string type)
		{
			this.sender = sender;
			this.user = user;
			this.channel = channel;
			this.fullname = fullname;
			this.success = success;
			this.type = type;
		}

		public string sender;
		public string user;
		public string channel;
		public string fullname;
		public bool success;
		public string type;
	}

	public class GetChannelUsers
	{
		public GetChannelUsers(){}

		public GetChannelUsers(string sender, string channel, List<string> users, string type)
		{
			this.sender = sender;
			this.channel = channel;
			this.users = users;
			this.type = type;
		}

		public string sender;
		public string channel;
		public List<String> users;
		// get_channel_users
		public string type;
	}

	// universal (for req(w/o users) and resp)
	public class GetOnlineUsers
	{
		public GetOnlineUsers() {}
		public GetOnlineUsers(string sender, List<string> users, string type)
		{
			this.sender = sender;
			this.users = users;
			this.type = type;
		}

		public string sender;
		public List<String> users;
		// "get_online_users"
		public string type;
	}


	// only for checking response type
	public class CommonResponse
	{
		public string type;
	}


	public class NewChannelRequest
	{
		public NewChannelRequest() {}
		public NewChannelRequest(string name, string fullname, string admin, string type)
		{
			this.name = name;
			this.fullname = fullname;
			this.admin = admin;
			this.type = type;
		}

		public string name;
		public string fullname;
		public string admin;
		public string type;
	}

	public class NewChannelResponse
	{
		public NewChannelResponse() {}
		public NewChannelResponse(string name, string fullname, string admin, string type, bool success)
		{
			this.name = name;
			this.fullname = fullname;
			this.admin = admin;
			this.type = type;
			this.success = success;
		}

		public string name;
		public string fullname;
		public string admin;
		public string type;
		public bool success;
	}

	public class GetChannelMessagesReq
	{
		public GetChannelMessagesReq(){}

		public GetChannelMessagesReq(string @from, string channel, long time, string type)
		{
			this.@from = @from;
			this.channel = channel;
			this.time = time;
			this.type = type;
		}

		public string from;
		public string channel;
		public long time;
		// "get_channel_messages"
		public string type;

	}


	public class ChannelRequest
	{
		public ChannelRequest() {}

		public ChannelRequest(string type, string name, string @from)
		{
			this.type = type;
			this.name = name;
			this.@from = @from;
		}

		public string type;
		// if name == '*' return all channels
		public string name;
		public string from;
	}

	public class RegRequest
	{
		public RegRequest() {}

		public RegRequest(string type, string user, string email, string password)
		{
			this.type = type;
			this.user = user;
			this.email = email;
			this.password = password;
		}

		public string type;
		public string user;
		public string email;
		public string password;
	}

	public class RegResponse
	{
		public RegResponse() {}

		public RegResponse(string type, bool success)
		{
			this.type = type;
			this.success = success;
		}

		public string type;
		public bool success;
	}

	public class AuthResponse
	{
		public AuthResponse() {}

		public AuthResponse(string type, bool success, string[] online)
		{
			this.type = type;
			this.success = success;
			this.online = online;
		}

		public string type;
		public bool success;
		public string[] online;
	}

	public class AuthRequest
	{
		public AuthRequest() { }
		public AuthRequest(string type, string user, string password)
		{
			this.type = type;
			this.user = user;
			this.password = password;
		}

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
