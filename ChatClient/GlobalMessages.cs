using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiteDB;
using Newtonsoft.Json;

namespace ChatClient
{
	public class GlobalMessages
	{
		private FileLogger l = new FileLogger(Config.logFileName);

		public GlobalMessages(MainWindow w)
		{
			this.w = w;
		}
		private MainWindow w;


		/// <summary>
		/// low level method, for create message view
		/// <see cref="createChannelGrid"/>
		/// </summary>
		/// <returns>grid object with needed parameters</returns>
		public Grid createMessageGrid(string name, string message, string time, Grid obj)
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
		public Grid createMyMessageGrid(string message, string time)
		{
			return createMessageGrid(w.userIdentification, message, time, w.MyMessageGrid);
		}
		/// <summary>
		/// shortcat for <see cref="createMessageGrid"/> for another users messages
		/// </summary>
		public Grid createAnotherMessageGrid(string name, string message, string time)
		{
			return createMessageGrid(name, message, time, w.AnotherMessageGrid);
		}

		/// <summary>
		/// scrolls messageList, used after sending (and receiving (?)) messages
		/// </summary>
		public void ScrollMessageListToEnd()
		{
			w.MessageList.ScrollIntoView(w.MessageList.Items[w.MessageList.Items.Count - 1]);
		}


		public void readFromTextBoxAndSend()
		{
			if (w.MessageTextBox.Text != "")
			{
				var ws = w.wsController.getWs();
				if (ws != null)
				{
					// берем имя выбранного канала
					Grid ch = (Grid)w.ChannelList.SelectedItems[0];
					string name = ((TextBlock)ch.Children[3]).Text;

					string time = Utils.getCurrentTime();
					// show
					w.MessageList.Items.Add(createMyMessageGrid(w.MessageTextBox.Text, time));
					// send
					Entities.MessageResponse mes = new Entities.MessageResponse("message", w.MessageTextBox.Text, Config.userName, DateTimeOffset.Now.ToUnixTimeMilliseconds(), name);
					string jsonReq = JsonConvert.SerializeObject(mes);
					l.log("sending message");
					ws.Send(jsonReq);

					w.MessageTextBox.Text = "";
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
		/// show new messages from server for current channel
		/// </summary>
		public void showMessage(Entities.MessageResponse mes)
		{
			Grid ch = (Grid)w.ChannelList.SelectedItems[0];
			string name = ((TextBlock)ch.Children[3]).Text;
			if (mes.channel == name)
			{
				Grid mGrid;
				if (mes.@from.Equals(Config.userName))
				{
					mGrid = createMyMessageGrid(mes.message, Utils.getCurrentTime());
				}
				else
				{
					mGrid = createAnotherMessageGrid(mes.@from, mes.message, Utils.getCurrentTime());
				}
				w.MessageList.Items.Add(mGrid);
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
			Grid ch = (Grid)w.ChannelList.SelectedItems[0];
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
						mGrid = createMyMessageGrid(mes, Utils.longToDateTime(time));
					}
					else
					{
						mGrid = createAnotherMessageGrid(fr, mes, Utils.longToDateTime(time));
					}
					w.MessageList.Items.Add(mGrid);
					ScrollMessageListToEnd();
				}
			}
		}




		public void showChannelInnerMessages(string channelName, List<Entities.MessageEntity> messages)
		{
			// смотрим, какой канал сейчас выбран
			Grid ch = (Grid)w.ChannelList.SelectedItems[0];
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
						mGrid = createMyMessageGrid(mes, Utils.longToDateTime(time));
					}
					else
					{
						mGrid = createAnotherMessageGrid(fr, mes, Utils.longToDateTime(time));
					}
					w.MessageList.Items.Add(mGrid);
					ScrollMessageListToEnd();
				}
			}
		}

		public void findAndRequireMessages(string channelName)
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
					var ws = w.wsController.getWs();
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
					var ws = w.wsController.getWs();
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
	}
}
