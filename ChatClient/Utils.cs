using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Newtonsoft.Json;
using WebSocketSharp;

namespace ChatClient
{
	public class Utils
	{
		private static FileLogger l = new FileLogger(Config.logFileName);

		// return true if successfully sended
		public static bool sendRequest(object obj, WsController c)
		{
			var ws = c.getWs();
			if (ws != null)
			{
				ws.Send(JsonConvert.SerializeObject(obj));
				return true;
			}
			return false;
		}

		public static void sendAddUserRequest(string user, string channel, string fullname, WsController c)
		{

			Entities.AddUser req = new Entities.AddUser();
			req.sender = Config.userName;
			req.user = user;
			req.channel = channel;
			req.fullname = fullname;
			req.type = "add_user";
			l.log("try send add user " + user + " to " + channel + " request");
			if (Utils.sendRequest(req, c))
			{
				l.log("sended");
			}
			else
			{
				l.log("sending aborted");
			}
		}
		public static void sendGetOnlineUsersRequest(WsController c)
		{
			var ws = c.getWs();
			if (ws != null)
			{
				l.log("sending online users request");
				Entities.GetOnlineUsers req = new Entities.GetOnlineUsers();
				req.sender = Config.userName;
				req.type = "get_online_users";
				ws.Send(JsonConvert.SerializeObject(req));
			}
		}

		public static void sendGetChannelUsersRequest(string channel, WsController c)
		{
			var ws = c.getWs();
			if (ws != null)
			{
				l.log("sending get channel users request");
				Entities.GetChannelUsers req = new Entities.GetChannelUsers();
				req.sender = Config.userName;
				req.type = "get_channel_users";
				req.channel = channel;
				ws.Send(JsonConvert.SerializeObject(req));
			}
		}



		/// <summary>
		/// we retrieve long time from server and view it in message grids
		/// </summary>
		public static string longToDateTime(long time)
		{
			return new DateTime(time * 10000).ToString(@"dd\.MM HH\:mm");
		}

		/// <summary>
		/// current date and time
		/// </summary>
		public static string getCurrentDatenTime()
		{
			return DateTime.Now.ToString(@"dd\.MM HH\:mm");
		}
		/// <summary>
		/// current time
		/// </summary>
		public static string getCurrentTime()
		{
			return DateTime.Now.ToString(@"HH\:mm");
		}

		
		

		/// <summary>
		/// return center position 
		/// </summary>
		/// <param name="resolution">is the full length of pixels</param>
		/// <param name="actual">size of app in pixels</param>
		public static double getCenter(double resolution, double actual)
		{
			return (resolution - actual) / 2;
		}

	}
}
