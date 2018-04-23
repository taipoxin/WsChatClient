using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ChatClient
{
	// dispatcher for mainWindow
	class Dispatchers
	{
		public static void dispatchGetChannelUsers(Entities.GetChannelUsers obj, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate {
				w.globalChannel.getChannelUsers(obj);
			}));
		}

		public static void dispatchGetOnlineUsers(Entities.GetOnlineUsers obj, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate {
				w.globalChannel.getOnlineUsers(obj);
			}));
		}

		public static void dispatchIncrementChannelMembersView(string channel, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate {
				w.incrementChannelMembersView(channel);
			}));
		}

		public static void dispatchNotifyOnAddingUserResponse(string userName, string channelName, bool isSuccess, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate
			{
				w.notifyOnAddingUserResponse(userName, channelName, isSuccess);
			}));
		}

		public static void dispatchCreateChannel(Entities.NewChannelResponse m, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate {
				w.globalChannel.createChannel(m);
			}));
		}

		public static void dispatchShowChannels(List<dynamic> channels, List<Int32> counts, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate {
				w.globalChannel.showChannels(channels, counts);
			}));
		}

		public static void dispatchShowMessage(Entities.MessageResponse mes, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate {
				w.globalMessages.showMessage(mes);
			}));
		}

		public static void dispatchShowChannelMessages(string channelName, List<dynamic> messages, MainWindow w)
		{
			w.Dispatcher.BeginInvoke(new ThreadStart(delegate {
				w.globalMessages.showChannelMessages(channelName, messages);
			}));
		}
	}
}
