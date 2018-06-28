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

		public GlobalChannel globalChannel;
		public GlobalMessages globalMessages;

		private string currentChannel;
		// using when send user's messages
		public string userIdentification = "Me";
		// used for counting created channels
		private int indx = 1;

		// if ChannelGrid state is Collapsed
		private bool isCollapsed = false;

		// контроль отправки на сервер
		public bool sending = false;
		// время отправки запроса на сервер
		private long sendTime;

		public WsController wsController;
		private FileLogger l = new FileLogger(Config.logFileName);


		/// <summary>
		/// runs after rendering window; 
		/// send request for all channels 
		/// </summary>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			globalChannel = new GlobalChannel(this);
			globalMessages = new GlobalMessages(this);
			// center Window after loaded
			this.Top = Utils.getCenter(SystemParameters.PrimaryScreenHeight, ActualHeight);
			this.Left = Utils.getCenter(SystemParameters.PrimaryScreenWidth, ActualWidth);

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


		public void setWsController(WsController c)
		{
			c.setMainWindow(this);
			wsController = c;
		}

		/// <summary>
		/// on 'Enter' key method sends message
		/// </summary>
		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				if (((TextBlock) ChatHeader.Children[0]).Text != "")
				{
					globalMessages.readMessageFromTextBoxAndSendIt();
				}
			}
		}

		private void EllipseMessage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (((TextBlock) ChatHeader.Children[0]).Text != "")
			{
				globalMessages.readMessageFromTextBoxAndSendIt();
			}
		}

	

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
		/// changed selection
		/// </summary>
		private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Grid channel = (Grid) e.AddedItems[0];
			TextBlock nameBlock = (TextBlock)channel.Children[0];
			TextBlock countBlock = (TextBlock)channel.Children[2];
			globalChannel.changeChatHeader(nameBlock, countBlock);

			// меняем шапку
			// очищаем messageList (и сохраняем messageList иного канала)
			// загружаем историю сообщений для данного канала
			// отправляем запрос на получение истории данного канала
			string chName = ((TextBlock)channel.Children[3]).Text;
			MessageList.Items.Clear();
			globalMessages.loadLocalAndRemoteChannelMessages(chName);
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
		


	

		// запрос на получение онлайн пользователей
		private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// {sender, type : get_online_users }
			l.log("yellow button down");
			if (ChannelList.SelectedItems.Count == 0) return;
			Grid ch = (Grid)ChannelList.SelectedItems[0];
			string chName = ((TextBlock)ch.Children[3]).Text;

			Utils.sendGetChannelUsersRequest(chName, wsController);
		}

		

		// cancel
		private void OnlineUserExitClick(object sender, RoutedEventArgs e)
		{
			globalChannel.closeOnlineUsersGrid();
		}


		// TODO: потом сделать типо форму меню
		public void ChannelSampleGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			globalChannel.currentRightClickGrid = (Grid)sender;
		}

		

		// +1 channel's members displayed
		public void incrementChannelMembersView(string channel)
		{
			Grid g = globalChannel.findChannelGridByName(channel);
			if (g == null)
			{
				l.log("no any grid with name " + channel);
				return;
			}
			var usersCountBlock = (TextBlock) g.Children[2];
			string current = usersCountBlock.Text;
			var spl = current.Split();
			int count = Convert.ToInt32(spl[0]);

			string newStr = (count + 1) +" " + spl[1];
			usersCountBlock.Text = newStr;
			l.log("updated channel members count");
		}


		private void OnlineUsersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
			{
				return;
			}
			if (globalChannel.isAddingUser)
			{
				// TODO: index out of range
				TextBlock item = (TextBlock) e.AddedItems[0];
				string userSelected = item.Text;
				// отправить запрос на добавление пользователя в канал
				Utils.sendAddUserRequest(userSelected, globalChannel.addingUserChannel, globalChannel.addingUserChannelDesc, wsController);

			}
		}


		public void notifyOnAddingUserResponse(string userName, string channelName, bool isSuccess)
		{
			if (isSuccess)
			{
				AddingUserResponseLabel.Content = userName + " успешно добавлен";
				AddingUserResponseLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5AF133"));
				incrementChannelMembersView(channelName);
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
				if (!globalChannel.isAddingUser)
				{
					Grid g = globalChannel.currentRightClickGrid;
					string chName = ((TextBlock)g.Children[3]).Text;
					string chDesc = ((TextBlock)g.Children[0]).Text;
					globalChannel.addUserToChannelTaskSending(chName, chDesc);
				}

			}
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
