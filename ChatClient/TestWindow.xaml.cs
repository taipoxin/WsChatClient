﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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




		/*
		 <Grid Width="374" Height="80" Background="#FFFFE8E8">
            <TextBlock HorizontalAlignment="Left" Margin="10,10,0,0" TextWrapping="Wrap" Text="Канал для успешных трейдеров" VerticalAlignment="Top" Height="32" Width="307" FontSize="14" FontFamily="Tahoma" FontWeight="Bold"/>
            <TextBlock HorizontalAlignment="Left" Margin="322,10,0,0" TextWrapping="Wrap" Text="8" VerticalAlignment="Top" Height="32" Width="42" FontSize="20" TextAlignment="Center" FontFamily="Tahoma"/>
            <TextBlock HorizontalAlignment="Left" Margin="10,42,0,0" TextWrapping="Wrap" Text="128 участников" VerticalAlignment="Top" Height="22" Width="163" FontFamily="Tahoma" FontSize="14"/>
        </Grid>
		 */
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






		/*
		 <Grid Name="ChannelSampleGrid" Width="374" VerticalAlignment="Top" Background="#FFBEE7E8" Visibility="Collapsed">
            <Grid.RowDefinitions>
                <RowDefinition Height="32"></RowDefinition>
                <RowDefinition Height="*"></RowDefinition>
            </Grid.RowDefinitions>
            <TextBlock Grid.Row="0" HorizontalAlignment="Left" Margin="10,10,0,0" TextWrapping="Wrap" Text="Username" VerticalAlignment="Top" Height="32" Width="307" FontSize="14" FontFamily="Tahoma" FontWeight="Bold"/>
            <TextBlock Grid.Row="1" HorizontalAlignment="Left" Margin="10,0,0,0" TextWrapping="Wrap" Text="some message dddddddddddddddddddddddddddddddddddddddddddddddddd" VerticalAlignment="Top"  Width="300" FontFamily="Tahoma" FontSize="14"/>
        </Grid>
		 */

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


		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				if (MessageTextBox.Text != "")
				{
					string time = getCurrentTime();
					MessageList.Items.Add(createMyMessageGrid(MessageTextBox.Text, time));
					MessageTextBox.Text = "";
					ScrollMessageListToEnd();
				}
			}
		}

		private void ScrollMessageListToEnd()
		{
			MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
		}

		private int indxx = 1;
		private void EllipseMessage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			string username = "Somebody";
			string message = "какое-то сообщение номер " + indxx;
			string time = getCurrentTime();
			MessageList.Items.Add(createAnotherMessageGrid(username, message, time));
			ScrollMessageListToEnd();
			indxx++;
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
			//MessageList.Items.Add(createAnotherMessageGrid("clicked", ((TextBlock)((Grid)e.AddedItems[0]).Children[0]).Text));

			Grid channel = (Grid) e.AddedItems[0];
			TextBlock nameBlock = (TextBlock)channel.Children[0];
			TextBlock countBlock = (TextBlock)channel.Children[2];

			changeHeader(nameBlock, countBlock);
		}
	}
}
