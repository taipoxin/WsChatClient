using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChatClient
{
	public class GlobalChannel
	{
		private FileLogger l = new FileLogger(Config.logFileName);

		public GlobalChannel(MainWindow w)
		{
			this.w = w;
		}
		private MainWindow w;

		public bool isAddingUser;
		public string addingUserChannel;
		public string addingUserChannelDesc;

		private bool isGettingChannelUsers;
		private string gettingChannelUsers;

		public Grid currentRightClickGrid;


		/// <summary>
		/// create channelGrid with selected params;
		/// other params are getted from pattern;
		/// </summary>
		private Grid createChannelGrid(string fullname, int newM, int users, string name)
		{
			var g1 = GenericsWPF<Grid>.DeepDarkCopy(w.ChannelSampleGrid);
			g1.Visibility = Visibility.Visible;
			var ch = g1.Children;
			((TextBlock)ch[0]).Text = fullname;
			((TextBlock)ch[1]).Text = "" + newM;
			((TextBlock)ch[2]).Text = users + " участников";
			((TextBlock)ch[3]).Text = name;
			g1.MouseRightButtonDown += w.ChannelSampleGrid_MouseRightButtonDown;
			return g1;
		}

		public void generateChannelRequest(string channelName)
		{
			Entities.ChannelRequest req = new Entities.ChannelRequest();
			req.type = "get_channel";
			req.name = channelName;
			req.@from = Config.userName;
			l.log("try send get channel " + channelName + " request");
			if (Utils.sendRequest(req, w.wsController))
			{
				l.log("sended");
			}
			else
			{
				l.log("sending aborted");
			}
		}

		/// <summary>
		/// Calling on exit button in onlineusergrid
		/// </summary>
		public void closeOnlineUsersGrid()
		{
			w.GetOnlineUsersGrid.Visibility = Visibility.Hidden;
			w.AddingUserResponseLabel.Visibility = Visibility.Hidden;
			isAddingUser = false;
			addingUserChannel = null;
			addingUserChannelDesc = null;
		}

		/// <summary>
		/// Calling when we want to add user to channel using server
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="chDesc"></param>
		public void addUserToChannelTaskSending(string channelName, string chDesc)
		{
			Utils.sendGetOnlineUsersRequest(w.wsController);
			isAddingUser = true;
			addingUserChannel = channelName;
			addingUserChannelDesc = chDesc;
		}


		/// <summary>
		/// get channel members from server
		/// </summary>
		/// <param name="channelName"></param>
		public void getChannelUsersTaskSending(string channelName)
		{
			Utils.sendGetChannelUsersRequest(channelName, w.wsController);
			isGettingChannelUsers = true;
			gettingChannelUsers = channelName;
		}


		public void getChannelUsersReceived(Entities.GetChannelUsers obj)
		{
			List<Entities.User> users = obj.users;
			w.ChannelUsersList.Items.Clear();
			foreach (Entities.User user in users)
			{
				TextBlock l = new TextBlock();
				if (user.type != null)
				{
					l.Text = user.login + " (" + user.type + ")";
				}
				else
				{
					l.Text = user.login;
				}
				w.ChannelUsersList.Items.Add(l);
			}
			w.GetChannelUsers.Visibility = Visibility.Visible;
		}



		public void getOnlineUsersReceived(Entities.GetOnlineUsers obj)
		{
			List<String> users = obj.users;
			w.OnlineUsersList.Items.Clear();
			foreach (string user in users)
			{
				TextBlock l = new TextBlock();
				l.Text = user;
				w.OnlineUsersList.Items.Add(l);
			}
			w.GetOnlineUsersGrid.Visibility = Visibility.Visible;
		}

		/// <summary>
		/// change chatHeaders name and count of members from parameters text variables
		/// </summary>
		public void changeChatHeader(TextBlock nameBlockFrom, TextBlock countBlockFrom)
		{
			TextBlock chatNameBlock = (TextBlock)w.ChatHeader.Children[0];
			TextBlock chatCountBlock = (TextBlock)w.ChatHeader.Children[1];

			chatNameBlock.Text = nameBlockFrom.Text;
			chatCountBlock.Text = countBlockFrom.Text;
		}

	
		public Grid findChannelGridByName(string channelName)
		{
			foreach (Grid chGrid in w.ChannelList.Items)
			{
				if (((TextBlock)chGrid.Children[3]).Text == channelName)
				{
					return chGrid;
				}
			}
			return null;
		}

		/// <summary>
		/// add channels from server to ChannelList
		/// </summary>
		public void getChannelsReceived(List<dynamic> channels, List<Int32> counts)
		{
			if (channels.Count == 0)
			{
				// TODO написать, что у пользователя пока нет ни одного канала

			}
			for (int i = 0; i < channels.Count; i++)
			{
				dynamic ch = channels[i];
				int user_count = counts[i];
				string n = ch.name;
				string fn = ch.fullname;
				Grid g = createChannelGrid(fn, 0, user_count, n);
				w.ChannelList.Items.Add(g);
			}
			
		}

		/// <summary>
		/// retrieve new loaded channel
		/// </summary>
		public void createChannelReceived(Entities.NewChannelResponse m)
		{
			if (m.success)
			{
				l.log("nice channel creation: " + m.name);

				// очищаем поля (после успешного создания канала)
				w.ChannelIDBox.Text = "";
				w.ChannelNameBox.Text = "";
				w.CrChBadData.Visibility = Visibility.Hidden;
				w.CreateChannelParentGrid.Visibility = Visibility.Hidden;

				// отображаем новый канал
				w.ChannelList.Items.Add(createChannelGrid(m.fullname, 0, 1, m.name));
			}
			else
			{
				l.log("server response: bad channel creation: " + m.name);
				w.CrChBadData.Content = "Ответ сервера: неверные данные";
				w.CrChBadData.Visibility = Visibility.Visible;
				w.CreateChannelParentGrid.Visibility = Visibility.Visible;
			}
			// возвращаем управление
			w.sending = false;
		}
	}
}
