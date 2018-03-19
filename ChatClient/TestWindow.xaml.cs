﻿using System;
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
using Newtonsoft.Json;
using WebSocketSharp;

namespace ChatClient
{
	/// <summary>
	/// Логика взаимодействия для TestWindow.xaml
	/// </summary>
	public partial class TestWindow: Window
	{
		public TestWindow()
		{
			InitializeComponent();
		}


		private string currentChannel;
		// using when send user's messages
		private string userIdentification = "Me";

		private WsController wsController;
		private FileLogger l = new FileLogger("signin.txt");

		public void setWsController(WsController c)
		{
			c.setMainWindow(this);
			wsController = c;
		}


		private Grid createChannelGrid(string name, int newM, int users)
		{
			var g1 = GenericsWPF<Grid>.DeepDarkCopy(ChannelSampleGrid);
			g1.Visibility = Visibility.Visible;
			var ch = g1.Children;
			((TextBlock)ch[0]).Text = name;
			((TextBlock)ch[1]).Text = "" + newM;
			((TextBlock)ch[2]).Text = users + " участников";
			return g1;
		}

		private int indx = 1;

		private void EllipseChannels_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			ChannelList.Items.Add(createChannelGrid("канал " + indx, 2 + indx, 5 + indx));
			indx++;
		}



		/**
		 * low level method, return grid object with needed parameters
		 */ 
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

		private Grid createMyMessageGrid(string message, string time)
		{
			return createMessageGrid(userIdentification, message, time, MyMessageGrid);
		}
		private Grid createAnotherMessageGrid(string name, string message, string time)
		{
			return createMessageGrid(name, message, time, AnotherMessageGrid);
		}


		private string getCurrentDatenTime()
		{
			return DateTime.Now.ToString(@"dd\.MM HH\:mm");
		}
		private string getCurrentTime()
		{
			return DateTime.Now.ToString(@"HH\:mm");
		}

		private void ScrollMessageListToEnd()
		{
			MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
		}

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				if (MessageTextBox.Text != "")
				{
					string time = getCurrentTime();
					MessageList.Items.Add(createMyMessageGrid(MessageTextBox.Text, time));
					MessageResponse mes = new MessageResponse("message", MessageTextBox.Text, Config.userName, DateTimeOffset.Now.ToUnixTimeMilliseconds());
					string jsonReq = JsonConvert.SerializeObject(mes);
					l.log("sending message");
					wsController.getWs().Send(jsonReq);

					MessageTextBox.Text = "";
					ScrollMessageListToEnd();
				}
			}
		}

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

		public void dispatchShowMessage(MessageResponse mes)
		{
			Dispatcher.BeginInvoke(new ThreadStart(delegate {
				showMessage(mes);
			}));
		}


		public void showMessage(MessageResponse mes)
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
		}

		// TODO: дописать как для итеративности добавлять много обьектов в json
		// TODO: также дописать сохранение в файл
		private void EllipseMessage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			MessageBean[] beans = MessageListGridsToObjectArray();
			string beansJ = WriteFromArrayOfObjectsToJson(beans);
			File.WriteAllText("MessageList.json", beansJ);

		}

		// if ChannelGrid state is Collapsed
		private bool isCollapsed = false;

		private void BackRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!SetChannelGridVisibilityCollapsed())
			{
				SetChannelGridVisibilityVisible();
			}
		}

		/**
		 * @return true if successfully change statement
		 */ 
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

		/**
		 * @return true if successfully change statement
		 */
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

		//private bool smalled = false;
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
		/**
		 * change chatHeaders name and count of members from parameters text variables
		 */ 
		private void changeHeader(TextBlock nameBlockFrom, TextBlock countBlockFrom) 
		{
			TextBlock chatNameBlock = (TextBlock)ChatHeader.Children[0];
			TextBlock chatCountBlock = (TextBlock)ChatHeader.Children[1];

			chatNameBlock.Text = nameBlockFrom.Text;
			chatCountBlock.Text = countBlockFrom.Text;
		}

		/**
		 * parameters, except targetChannel can be null, or throw exception
		 * method change at least one parameter, or throw exception
		 */ 
		private void changeChannelParams(Grid targetChannel, string channelName, string channelNew, string channelCount)
		{
			int i = 0;
			if (targetChannel != null)
			{
				var ch = targetChannel.Children;
				if (channelName != null)
				{
					((TextBlock) ch[0]).Text = channelName;
					i++;
				}
				if (channelNew!= null)
				{
					((TextBlock)ch[1]).Text = channelNew;
					i++;
				}
				if (channelCount != null)
				{
					((TextBlock)ch[2]).Text = channelCount;
					i++;
				}

				if (i == 0)
				{
					throw new  Exception("all grid's params are null");
				}
			}
			else
			{
				throw new Exception("target cannot be null");
			}
		}


		private void ChannelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Grid channel = (Grid) e.AddedItems[0];
			TextBlock nameBlock = (TextBlock)channel.Children[0];
			TextBlock countBlock = (TextBlock)channel.Children[2];

			changeHeader(nameBlock, countBlock);
		}

		//TODO: websocketClient
		private void wsClientTest()
		{
			using (var ws = new WebSocket(Config.wsSource))
			{
				ws.OnMessage += (sender, e) =>
					Console.WriteLine("Laputa says: " + e.Data);

				ws.Connect();
				ws.Send("BALUS");
				Console.ReadKey(true);
			}
		}


		//TODO: разобрать все методы ниже в соответсвии с задачами
		void mMain()
		{
			Task t = new Task(DownloadPageAsync);
			t.Start();
			Console.WriteLine("Downloading page...");
			Console.ReadLine();
		}

		async void DownloadPageAsync()
		{
			// ... Target page.
			string page = "http://en.wikipedia.org/";

			// ... Use HttpClient.
			using (HttpClient client = new HttpClient())
			using (HttpResponseMessage response = await client.GetAsync(page))
			using (HttpContent content = response.Content)
			{
				// ... Read the string.
				string result = await content.ReadAsStringAsync();
				
			}
		}

		// регистрация
		static string Register(string email, string password)
		{
			var registerModel = new {
				Email = email,
				Password = password,
				ConfirmPassword = password
			};
			using (var client = new HttpClient())
			{
				var response = client.PostAsJsonAsync("/api/Account/Register", registerModel).Result;
				return response.StatusCode.ToString();
			}
		}
		// получение токена
		static Dictionary<string, string> GetTokenDictionary(string userName, string password)
		{
			var pairs = new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>( "grant_type", "password" ),
					new KeyValuePair<string, string>( "username", userName ),
					new KeyValuePair<string, string> ( "Password", password )
				};
			var content = new FormUrlEncodedContent(pairs);

			using (var client = new HttpClient())
			{
				var response =
					client.PostAsync("/Token", content).Result;
				var result = response.Content.ReadAsStringAsync().Result;
				// Десериализация полученного JSON-объекта
				Dictionary<string, string> tokenDictionary =
					JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
				return tokenDictionary;
			}
		}

		// создаем http-клиента с токеном 
		static HttpClient CreateClient(string accessToken = "")
		{
			var client = new HttpClient();
			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				client.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
			}
			return client;
		}

		// получаем информацию о клиенте 
		static string GetUserInfo(string token)
		{
			using (var client = CreateClient(token))
			{
				var response = client.GetAsync("/api/Account/UserInfo").Result;
				return response.Content.ReadAsStringAsync().Result;
			}
		}

		// обращаемся по маршруту api/values 
		static string GetValues(string token)
		{
			using (var client = CreateClient(token))
			{
				var response = client.GetAsync("/api/values").Result;
				return response.Content.ReadAsStringAsync().Result;
			}
		}

		private double getCenter(double resolution, double actual)
		{
			return (resolution - actual) / 2;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.Top = getCenter(SystemParameters.PrimaryScreenHeight, ActualHeight);
			this.Left = getCenter(SystemParameters.PrimaryScreenWidth, ActualWidth);
		}
	}
}
