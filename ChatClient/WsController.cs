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
					Entities.MessageResponse m = dynamicToMessageResponse(resp);
					while (mainWindow == null)
					{
						l.log("another sleep");
						Thread.Sleep(100);
					}
					// show
					Dispatchers.dispatchShowMessage(m, mainWindow);

					// serialise
					string channelName = m.channel;
					var ent = new Entities.MessageEntity {from = m.@from, message = m.message, time = m.time};
					using (var db = new LiteDatabase(@"LocalData.db"))
					{
						var messagesCollection = db.GetCollection<Entities.MessageEntity>(channelName + "_mes");
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
					Dispatchers.dispatchShowChannels(chList, counts, mainWindow);
				}

				else if ("new_channel".Equals(type))
				{
					Entities.NewChannelResponse m = dynamicToNewChannelResponse(resp);
					Dispatchers.dispatchCreateChannel(m, mainWindow);
				}
				else if ("get_channel_messages".Equals(type))
				{
					JArray array = resp.messages;
					List<dynamic> ll = array.ToObject<List<dynamic>>();
					string ch = resp.channel;

					l.log("received messages for channel " + ch + " are " + array.ToString());
					// show
					Dispatchers.dispatchShowChannelMessages(ch, ll, mainWindow);

					// serialise
					var entities = listDynamicToMessageEntities(ll);
					try
					{
						using (var db = new LiteDatabase(@"LocalData.db"))
						{
							var messagesCollection = db.GetCollection<Entities.MessageEntity>(ch + "_mes");
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
					Entities.GetOnlineUsers obj = new Entities.GetOnlineUsers();
					obj.sender = resp.sender;
					obj.users = array.ToObject<List<string>>();
					obj.type = resp.type;
					Dispatchers.dispatchGetOnlineUsers(obj, mainWindow);
				}

				else if ("add_user".Equals(type))
				{
					Entities.AddUser evnt = new Entities.AddUser();
					evnt.sender = resp.sender;
					evnt.user = resp.user;
					evnt.channel = resp.channel;
					evnt.success = resp.success;
					evnt.type = resp.type;


					if (evnt.sender == Config.userName)
					{
						// notify about successful or not adding	
						// display info about adding (using add user menu)
						Dispatchers.dispatchNotifyOnAddingUserResponse(evnt.user, evnt.channel, evnt.success, mainWindow);
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
							Dispatchers.dispatchIncrementChannelMembersView(evnt.channel, mainWindow);
						}
					}
				}
				else if ("get_channel_users".Equals(type))
				{
					JArray array = resp.users;
					Entities.GetChannelUsers obj = new Entities.GetChannelUsers();
					obj.sender = resp.sender;
					obj.channel = resp.channel;
					obj.users = array.ToObject<List<Entities.User>>();
					obj.type = resp.type;
					Dispatchers.dispatchGetChannelUsers(obj, mainWindow);
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

		private List<Entities.MessageEntity> listDynamicToMessageEntities(List<dynamic> ll)
		{
			List<Entities.MessageEntity> entities = new List<Entities.MessageEntity>(ll.Capacity);
			foreach (dynamic m in ll)
			{
				string fr = m.from;
				string mes = m.message;
				long t = m.time;
				var mess = new Entities.MessageEntity {from = fr, message = mes, time = t};
				entities.Add(mess);
			}
			return entities;
		}

		private void tryToConnect(WebSocket ws)
		{
			while (!ws.IsAlive)
			{
				// about 2 second to connect
				ws.Connect();
				;
			}
			l.log("ws connected");
		}

		private Entities.MessageResponse dynamicToMessageResponse(dynamic obj)
		{
			string from = obj.from;
			string type = obj.type;
			long time = obj.time;
			string message = obj.message;
			string channel = obj.channel;
			return new Entities.MessageResponse(type, message, from, time, channel);
		}

		private Entities.NewChannelResponse dynamicToNewChannelResponse(dynamic obj)
		{
			var ch = new Entities.NewChannelResponse();
			ch.name = obj.name;
			ch.fullname = obj.fullname;
			ch.admin = obj.admin;
			ch.success = obj.success;
			ch.type = obj.type;
			return ch;
		}

	}
}
