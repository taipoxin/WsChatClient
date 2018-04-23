using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
	public class Entities
	{

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
			public AddUser() { }
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

		public class User
		{
			public User() { }

			public User(string login, string type)
			{
				this.login = login;
				this.type = type;
			}

			public string login;
			public string type;
		}

		public class GetChannelUsers
		{
			public GetChannelUsers() { }

			public GetChannelUsers(string sender, string channel, List<User> users, string type)
			{
				this.sender = sender;
				this.channel = channel;
				this.users = users;
				this.type = type;
			}

			public string sender;
			public string channel;
			public List<User> users;
			// get_channel_users
			public string type;
		}

		// universal (for req(w/o users) and resp)
		public class GetOnlineUsers
		{
			public GetOnlineUsers() { }
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
			public NewChannelRequest() { }
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
			public NewChannelResponse() { }
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
			public GetChannelMessagesReq() { }

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
			public ChannelRequest() { }

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
			public RegRequest() { }

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
			public RegResponse() { }

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
			public AuthResponse() { }

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
			public MessageResponse() { }

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

}

