using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LiteDB;
using Newtonsoft.Json;
using WebSocketSharp;

namespace ChatClient
{
	/// <summary>
	/// Логика взаимодействия для TestWindow.xaml
	/// </summary>
	public partial class MainWindow: Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// return center position 
		/// </summary>
		/// <param name="resolution">is the full length of pixels</param>
		/// <param name="actual">size of app in pixels</param>
		private double getCenter(double resolution, double actual)
		{
			return (resolution - actual) / 2;
		}

		/// <summary>
		/// runs after rendering window; 
		/// send request for all channels 
		/// </summary>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// center Window after loaded
			this.Top = getCenter(SystemParameters.PrimaryScreenHeight, ActualHeight);
			this.Left = getCenter(SystemParameters.PrimaryScreenWidth, ActualWidth);

			// отправляем запрос всех доступных каналов
			// возвращается массив объектов {name, fullname, admin}
			var ws = wsController.getWs();
			if (ws != null)
			{
				var getChannels = new Entities.ChannelRequest();
				getChannels.type = "get_channel";
				getChannels.name = "*";
				getChannels.from = Config.userName;
				string getAllCh = JsonConvert.SerializeObject(getChannels);
				ws.Send(getAllCh);
			}
		}




		private string currentChannel;
		// using when send user's messages
		private string userIdentification = "Me";
		// used for counting created channels
		private int indx = 1;


		private WsController wsController;
		private FileLogger l = new FileLogger(Config.logFileName);

		public void setWsController(WsController c)
		{
			c.setMainWindow(this);
			wsController = c;
		}

		/// <summary>
		/// create channelGrid with selected params;
		/// other params are getted from pattern;
		/// </summary>
		private Grid createChannelGrid(string fullname, int newM, int users, string name)
		{
			var g1 = GenericsWPF<Grid>.DeepDarkCopy(ChannelSampleGrid);
			g1.Visibility = Visibility.Visible;
			var ch = g1.Children;
			((TextBlock)ch[0]).Text = fullname;
			((TextBlock)ch[1]).Text = "" + newM;
			((TextBlock)ch[2]).Text = users + " участников";
			((TextBlock)ch[3]).Text = name;
			g1.MouseRightButtonDown += ChannelSampleGrid_MouseRightButtonDown;
			return g1;
		}

		


		/// <summary>
		/// low level method, for create message view
		/// <see cref="createChannelGrid"/>
		/// </summary>
		/// <returns>grid object with needed parameters</returns>
		private Grid createMessageGrid(string name, string message, string time, Grid obj)
		{
			var g1 = GenericsWPF<Grid>.DeepDarkCopy(obj);
			g1.Visibility = Visibility.Visible;
			var ch = g1.Children;
			((TextBlock)ch[0]).Text = name;
			((TextBlock)ch[1]).Text = message;
			((TextBlock)ch[2]).Text = time;
			return g1;
		}

		/// <summary>
		/// shortcat for <see cref="createMessageGrid"/> for current user's messages
		/// </summary>
		private Grid createMyMessageGrid(string message, string time)
		{
			return createMessageGrid(userIdentification, message, time, MyMessageGrid);
		}
		/// <summary>
		/// shortcat for <see cref="createMessageGrid"/> for another users messages
		/// </summary>
		private Grid createAnotherMessageGrid(string name, string message, string time)
		{
			return createMessageGrid(name, message, time, AnotherMessageGrid);
		}

		/// <summary>
		/// we retrieve long time from server and view it in message grids
		/// </summary>
		private string longToDateTime(long time)
		{
			return new DateTime(time*10000).ToString(@"dd\.MM HH\:mm");
		}

		/// <summary>
		/// current date and time
		/// </summary>
		private string getCurrentDatenTime()
		{
			return DateTime.Now.ToString(@"dd\.MM HH\:mm");
		}
		/// <summary>
		/// current time
		/// </summary>
		private string getCurrentTime()
		{
			return DateTime.Now.ToString(@"HH\:mm");
		}

		/// <summary>
		/// scrolls messageList, used after sending (and receiving (?)) messages
		/// </summary>
		private void ScrollMessageListToEnd()
		{
			MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
		}


		private void readFromTextBoxAndSend()
		{
			if (MessageTextBox.Text != "")
			{
				var ws = wsController.getWs();
				if (ws != null)
				{
					// берем имя выбранного канала
					Grid ch = (Grid)ChannelList.SelectedItems[0];
					string name = ((TextBlock)ch.Children[3]).Text;

					string time = getCurrentTime();
					// show
					MessageList.Items.Add(createMyMessageGrid(MessageTextBox.Text, time));
					// send
					Entities.MessageResponse mes = new Entities.MessageResponse("message", MessageTextBox.Text, Config.userName, DateTimeOffset.Now.ToUnixTimeMilliseconds(), name);
					string jsonReq = JsonConvert.SerializeObject(mes);
					l.log("sending message");
					ws.Send(jsonReq);

					MessageTextBox.Text = "";
					ScrollMessageListToEnd();

					// serialise
					using (var db = new LiteDatabase(@"LocalData.db"))
					{
						var messages = db.GetCollection<Entities.MessageEntity>(mes.channel + "_mes");
						var ent = new Entities.MessageEntity { from = mes.@from, message = mes.message, time = mes.time };
						messages.Insert(ent);
					}
				}
			}
		}

		/// <summary>
		/// on 'Enter' key method sends message
		/// </summary>
		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				readFromTextBoxAndSend();
			}
		}

		private void EllipseMessage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			readFromTextBoxAndSend();
		}

		// serialisation methods
		private string WriteFromArrayOfObjectsToJson(MessageBean[] beans)
		{
			return JsonConvert.SerializeObject(beans);
		}
		private MessageBean MessageGridToObject(Grid messageGrid)
		{
			var ch = messageGrid.Children;
			string sender =  ((TextBlock)ch[0]).Text;
			string message = ((TextBlock)ch[1]).Text;
			string time =    ((TextBlock)ch[2]).Text;
			return new MessageBean(sender, message, time);
		}
		private MessageBean[] MessageListGridsToObjectArray()
		{
			int count = MessageList.Items.Count;
			MessageBean[] beans = new MessageBean[count];
			for (int i = 0; i < count; i++)
			{
				MessageBean b = MessageGridToObject((Grid)MessageList.Items[i]);
				beans[i] = b;
			}
			return beans;
		}


		
		
		/// <summary>
		/// retrieve new loaded channel
		/// </summary>
		public void createChannel(Entities.NewChannelResponse m)
		{
			if (m.success)
			{
				l.log("nice channel creation: " + m.name);

				// очищаем поля (после успешного создания канала)
				ChannelIDBox.Text = "";
				ChannelNameBox.Text = "";
				CrChBadData.Visibility = Visibility.Hidden;
				CreateChannelParentGrid.Visibility = Visibility.Hidden;

				// отображаем новый канал
				ChannelList.Items.Add(createChannelGrid(m.fullname, 0, 1, m.name));
			}
			else
			{
				l.log("server response: bad channel creation: " + m.name);
				CrChBadData.Content = "Ответ сервера: неверные данные";
				CrChBadData.Visibility = Visibility.Visible;
				CreateChannelParentGrid.Visibility = Visibility.Visible;
			}
			// возвращаем управление
			sending = false;
		}


		
		
		/// <summary>
		/// add channels from server to ChannelList
		/// </summary>
		public void showChannels(List<dynamic> channels, List<Int32> counts)
		{
			for (int i = 0; i < channels.Count; i++)
			{
				dynamic ch = channels[i];
				int user_count = counts[i];
				string n = ch.name;
				string fn = ch.fullname;
				Grid g = createChannelGrid(fn, 0, user_count, n);
				ChannelList.Items.Add(g);
			}
		}


		
		/// <summary>
		/// show new messages from server for current channel
		/// </summary>
		public void showMessage(Entities.MessageResponse mes)
		{
			Grid ch = (Grid)ChannelList.SelectedItems[0];
			string name = ((TextBlock)ch.Children[3]).Text;
			if (mes.channel == name)
			{
				Grid mGrid;
				if (mes.@from.Equals(Config.userName))
				{
					mGrid = createMyMessageGrid(mes.message, getCurrentTime());
				}
				else
				{
					mGrid = createAnotherMessageGrid(mes.@from, mes.message, getCurrentTime());
				}
				MessageList.Items.Add(mGrid);
				ScrollMessageListToEnd();
			}
		}


		

		/// <summary>
		/// display channel messages from server, if channel is chosen
		/// </summary>
		/// <param name="channelName"></param>
		/// <param name="messages"></param>
		public void showChannelMessages(string channelName, List<dynamic> messages)
		{
			// смотрим, какой канал сейчас выбран
			Grid ch = (Grid) ChannelList.SelectedItems[0];
			string name = ((TextBlock)ch.Children[3]).Text;
			if (channelName == name)
			{
				//MessageList.Items.Clear();
				// рендерим список сообщений
				foreach (dynamic m in messages)
				{
					string mes = m.message;
					string fr = m.from;
					long time = m.time;
					Grid mGrid;
					if (fr.Equals(Config.userName))
					{
						mGrid = createMyMessageGrid(mes, longToDateTime(time));
					}
					else
					{
						mGrid = createAnotherMessageGrid(fr, mes, longToDateTime(time));
					}
					MessageList.Items.Add(mGrid);
					ScrollMessageListToEnd();
				}
			}
		}


		

		// if ChannelGrid state is Collapsed
		private bool isCollapsed = false;

		/// <summary>
		/// button hide channel list
		/// </summary>
		private void BackRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!SetChannelGridVisibilityCollapsed())
			{
				SetChannelGridVisibilityVisible();
			}
		}

		/// <returns>true if successfully change statement</returns>
		private bool SetChannelGridVisibilityCollapsed()
		{
			if (!isCollapsed)
			{
				ChannelGrid.Visibility = Visibility.Collapsed;
				isCollapsed = true;
				return true;
			}
			return false;
		}
		
		/// <returns>true if successfully change statement</returns>
		private bool SetChannelGridVisibilityVisible()
		{
			if (isCollapsed)
			{
				ChannelGrid.Visibility = Visibility.Visible;
				isCollapsed = false;
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// hide channel list
		/// </summary>
		private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
				if (e.NewSize.Width <= 800)
				{
					SetChannelGridVisibilityCollapsed();
				}
				// > 800
				else
				{
					SetChannelGridVisibilityVisible();
				}
		}


		/// <summary>
		/// change chatHeaders name and count of members from parameters text variables
		/// </summary>
		private void changeHeader(TextBlock nameBlockFrom, TextBlock countBlockFrom) 
		{
			TextBlock chatNameBlock = (TextBlock)ChatHeader.Children[0];
			TextBlock chatCountBlock = (TextBlock)ChatHeader.Children[1];

			chatNameBlock.Text = nameBlockFrom.Text;
			chatCountBlock.Text = countBlockFrom.Text;
		}

		private Grid getChannelGridByName(string channelName)
		{
			foreach (Grid chGrid in ChannelList.Items)
			{
				if (((TextBlock) chGrid.Children[3]).Text == channelName)
				{
					return chGrid;
				}
			}
			return null;
		}


		public void showChannelInnerMessages(string channelName, List<Entities.MessageEntity> messages)
		{
			// смотрим, какой канал сейчас выбран
			Grid ch = (Grid)ChannelList.SelectedItems[0];
			string name = ((TextBlock)ch.Children[3]).Text;
			if (channelName == name)
			{
				// рендерим список сообщений
				foreach (Entities.MessageEntity m in messages)
				{
					string mes = m.message;
					string fr = m.from;
					long time = m.time;
					Grid mGrid;
					if (fr.Equals(Config.userName))
					{
						mGrid = createMyMessageGrid(mes, longToDateTime(time));
					}
					else
					{
						mGrid = createAnotherMessageGrid(fr, mes, longToDateTime(time));
					}
					MessageList.Items.Add(mGrid);
					ScrollMessageListToEnd();
				}
			}
		}

		private void findAndRequireMessages(string channelName)
		{
			// resp: {message, from, channel, time, type: 'message'}
			// храним: {message, from, time}
			using (var db = new LiteDatabase(@"LocalData.db"))
			{
				var messages = db.GetCollection<Entities.MessageEntity>(channelName + "_mes");

				var mes = messages.FindAll();
				mes = mes.OrderBy(x => x.time);

				// отправляем запрос на все
				if (mes.Count() == 0)
				{
					var ws = wsController.getWs();
					if (ws != null)
					{
						var getChannelMessages = new Entities.GetChannelMessagesReq();
						getChannelMessages.type = "get_channel_messages";
						getChannelMessages.channel = channelName;
						getChannelMessages.from = Config.userName;
						string getChM = JsonConvert.SerializeObject(getChannelMessages);
						ws.Send(getChM);
					}
				}
				// запрашиваем с сервера только новые сообщения (время больше чем максимальное)
				// и сразу их сериализуем
				else
				{
					var t = mes.Last().time;
					// запрашиваем с сервера сообщения где time > t
					// отображаем наши сообщения
					showChannelInnerMessages(channelName, mes.ToList());
					// все пришедшие с сервера сообщения записываем в конец
					var ws = wsController.getWs();
					if (ws != null)
					{
						var getChannelMessages = new Entities.GetChannelMessagesReq();
						getChannelMessages.type = "get_channel_messages";
						getChannelMessages.channel = channelName;
						getChannelMessages.from = Config.userName;
						getChannelMessages.time = t;
						string getChM = JsonConvert.SerializeObject(getChannelMessages);
						ws.Send(getChM);
					}
				}
			}
		}

		/// <summary>
		/// changed selection
		/// </summary>
		private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Grid channel = (Grid) e.AddedItems[0];
			TextBlock nameBlock = (TextBlock)channel.Children[0];
			TextBlock countBlock = (TextBlock)channel.Children[2];
			changeHeader(nameBlock, countBlock);

			// меняем шапку
			// очищаем messageList (и сохраняем messageList иного канала)
			// загружаем историю сообщений для данного канала
			// отправляем запрос на получение истории данного канала
			string chName = ((TextBlock)channel.Children[3]).Text;
			MessageList.Items.Clear();
			findAndRequireMessages(chName);
			


		}


		/// <summary>
		/// press the button create new channel:
		/// it send request for creation a channel, 
		/// and if response.success == true, it show the channel
		/// <see cref="createChannel"/>
		/// </summary>
		private void EllipseChannels_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (CreateChannelParentGrid.Visibility != Visibility.Hidden)
			{
				l.log("something wrong with visibility of createChannelForm: setting visibility to hidden");
				CreateChannelParentGrid.Visibility = Visibility.Hidden;
				return;
			}
			CreateChannelParentGrid.Visibility = Visibility.Visible;

		}

		// контроль отправки на сервер
		private bool sending = false;
		// время отправки запроса на сервер
		private long sendTime;
		private void CreateChannelButtonClick(object sender, RoutedEventArgs e)
		{
			if (sending)
			{
				// delay 5 sec
				var delay = (new DateTime().Ticks * 10000) - sendTime;
				if (delay > 5000)
				{
					l.log("сервер недоступен: timeout create_channel, delay:  " + delay);
					CrChBadData.Content = "Сервер временно недоступен (timeout)";
					CrChBadData.Visibility = Visibility.Visible;
				}
				return;
			}
			string name = ChannelIDBox.Text;
			string fullname = ChannelNameBox.Text;
			if (name == fullname || name == "" || fullname == "")
			{
				l.log("bad channel data");
				CrChBadData.Content = "Неверные данные";
				CrChBadData.Visibility = Visibility.Visible;
				return;
			}

			// отправляем запрос о создании канала
			var ws = wsController.getWs();
			if (ws != null)
			{
				Entities.NewChannelRequest r = new Entities.NewChannelRequest();
				r.type = "new_channel";
				r.name = name;
				r.fullname = fullname;
				r.admin = Config.userName;
				ws.Send(JsonConvert.SerializeObject(r));
				sending = true;
				sendTime = new DateTime().Ticks * 10000;
			}
			else
			{
				l.log("сервер недоступен: create_channel");
				CrChBadData.Content = "Сервер временно недоступен";
				CrChBadData.Visibility = Visibility.Visible;
			}
			
		}

		private void CreateChannelExitClick(object sender, RoutedEventArgs e)
		{
			CreateChannelParentGrid.Visibility = Visibility.Hidden;
			sending = false;
		}

		

		public void getChannelUsers(Entities.GetChannelUsers obj)
		{
			List<Entities.User> users = obj.users;
			ChannelUsersList.Items.Clear();
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
				ChannelUsersList.Items.Add(l);
			}
			GetChannelUsers.Visibility = Visibility.Visible;
		}

		

		public void getOnlineUsers(Entities.GetOnlineUsers obj)
		{
			List<String> users = obj.users;
			OnlineUsersList.Items.Clear();
			foreach (string user in users)
			{
				TextBlock l = new TextBlock();
				l.Text = user;
				OnlineUsersList.Items.Add(l);
			}
			GetOnlineUsersGrid.Visibility = Visibility.Visible;
		}

		private void sendGetOnlineUsersRequest()
		{
			var ws = wsController.getWs();
			if (ws != null)
			{
				l.log("sending online users request");
				Entities.GetOnlineUsers req = new Entities.GetOnlineUsers();
				req.sender = Config.userName;
				req.type = "get_online_users";
				ws.Send(JsonConvert.SerializeObject(req));
			}
		}

		private void sendGetChannelUsersRequest(string channel)
		{
			var ws = wsController.getWs();
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

		// запрос на получение онлайн пользователей
		private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// {sender, type : get_online_users }
			l.log("yellow button down");
			if (ChannelList.SelectedItems.Count == 0) return;
			Grid ch = (Grid)ChannelList.SelectedItems[0];
			string chName = ((TextBlock)ch.Children[3]).Text;

			sendGetChannelUsersRequest(chName);
		}

		// cancel
		private void OnlineUserExitClick(object sender, RoutedEventArgs e)
		{
			GetOnlineUsersGrid.Visibility = Visibility.Hidden;
			isAddingUser = false;
			addingUserChannel = null;
			addingUserChannelDesc = null;
			AddingUserResponseLabel.Visibility = Visibility.Hidden;
		}

		private bool isAddingUser;
		private string addingUserChannel;
		private string addingUserChannelDesc;
		private void addUserToChannelTask(string channelName, string chDesc)
		{
			sendGetOnlineUsersRequest();
			isAddingUser = true;
			addingUserChannel = channelName;
			addingUserChannelDesc = chDesc;
		}


		private bool isGettingChannelUsers;
		private string gettingChannelUsers;
		private void getChannelUsersTask(string channelName)
		{
			sendGetChannelUsersRequest(channelName);
			isGettingChannelUsers = true;
			gettingChannelUsers = channelName;
		}



		private Grid currentRightClickGrid;

		// TODO: потом сделать типо форму меню
		private void ChannelSampleGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			currentRightClickGrid = (Grid)sender;

		}

		// return true if successfully sended
		public bool sendRequest(object obj)
		{
			var ws = wsController.getWs();
			if (ws != null)
			{
				ws.Send(JsonConvert.SerializeObject(obj));
				return true;
			}
			return false;
		}

		private void sendAddUserRequest(string user, string channel, string fullname)
		{
			
			Entities.AddUser req = new Entities.AddUser();
			req.sender = Config.userName;
			req.user = user;
			req.channel = channel;
			req.fullname = fullname;
			req.type = "add_user";
			l.log("try send add user " + user + " to " + channel + " request");
			if (sendRequest(req))
			{
				l.log("sended");
			}
			else
			{
				l.log("sending aborted");
			}
		}

		

		// +1 channel's members displayed
		public void incrementChannelMembersView(string channel)
		{
			Grid g = getChannelGridByName(channel);
			if (g == null)
			{
				l.log("no any grid with name " + channel);
				return;
			}
			var usersCountBlock = (TextBlock) g.Children[2];
			string current = usersCountBlock.Text;
			var spl = current.Split();
			int count = Convert.ToInt32(spl[0]);

			string newStr = (count + 1) + spl[1];
			usersCountBlock.Text = newStr;
			l.log("updated channel members count");
		}


		public void getChannelRequest(string channelName)
		{
			Entities.ChannelRequest req = new Entities.ChannelRequest();
			req.type = "get_channel";
			req.name = channelName;
			req.@from = Config.userName;
			l.log("try send get channel " + channelName + " request");
			if (sendRequest(req))
			{
				l.log("sended");
			}
			else
			{
				l.log("sending aborted");
			}
		}



		private void OnlineUsersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
			{
				return;
			}
			if (isAddingUser)
			{
				// TODO: index out of range
				TextBlock item = (TextBlock) e.AddedItems[0];
				string userSelected = item.Text;
				// отправить запрос на добавление пользователя в канал
				sendAddUserRequest(userSelected, addingUserChannel, addingUserChannelDesc);

			}
		}


		public void notifyOnAddingUserResponse(string userName, string channelName, bool isSuccess)
		{
			if (isSuccess)
			{
				AddingUserResponseLabel.Content = userName + " успешно добавлен";
				AddingUserResponseLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5AF133"));
			}
			else
			{
				AddingUserResponseLabel.Content = "Не удалось добавить " + userName;
				AddingUserResponseLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEE2323"));
			}

			GetOnlineUsersGrid.Visibility = Visibility.Visible;
			AddingUserResponseLabel.Visibility = Visibility.Visible;
		}

		private void Menu_Item_Click_Event(object sender, RoutedEventArgs e)
		{
			if (((MenuItem)sender).Name == "AddUserItem")
			{
				if (!isAddingUser)
				{
					Grid g = currentRightClickGrid;
					string chName = ((TextBlock)g.Children[3]).Text;
					string chDesc = ((TextBlock)g.Children[0]).Text;
					addUserToChannelTask(chName, chDesc);
				}

			}
		}

		private void openChannelUsersGrid()
		{
			// отправляем реквест на пользователей данного канала
			// при получении загружаем этот список
		}
		// TODO: пофиксить
		// шапка канала - надпись о списке участников
		private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			openChannelUsersGrid();
		}

		private void closeChannelUsersGrid()
		{
			GetChannelUsers.Visibility = Visibility.Hidden;
			UserActionResponseLabel.Visibility = Visibility.Hidden;
		}

		private void ExitChannelUsers(object sender, RoutedEventArgs e)
		{
			closeChannelUsersGrid();
		}
	}
}
