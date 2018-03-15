using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Xml;

namespace ChatClient
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window
	{
		public MainWindow()
		{
			InitializeComponent();
			
		}
		
		/*
		 <Grid Width="374" Height="80" Background="#FFFFE8E8">
            <TextBlock HorizontalAlignment="Left" Margin="10,10,0,0" TextWrapping="Wrap" Text="Канал для успешных трейдеров" VerticalAlignment="Top" Height="32" Width="307" FontSize="14" FontFamily="Tahoma" FontWeight="Bold"/>
            <TextBlock HorizontalAlignment="Left" Margin="322,10,0,0" TextWrapping="Wrap" Text="8" VerticalAlignment="Top" Height="32" Width="42" FontSize="20" TextAlignment="Center" FontFamily="Tahoma"/>
            <TextBlock HorizontalAlignment="Left" Margin="10,42,0,0" TextWrapping="Wrap" Text="128 участников" VerticalAlignment="Top" Height="22" Width="163" FontFamily="Tahoma" FontSize="14"/>
        </Grid>
		 */
		private Grid createChannelGrid(string name, int newM, int users)
		{
			var g1 = GenericsWPF<Grid>.DeepDarkCopy(SampleGrid);
			g1.Visibility = Visibility.Visible;
			var ch = g1.Children;
			((TextBlock) ch[0]).Text = name;
			((TextBlock) ch[1]).Text = ""+newM;
			((TextBlock) ch[2]).Text = users + " участников";
			return g1;
		}

		private int indx = 1;
		private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			ChannelList.Items.Add(createChannelGrid("канал " + indx, 2+indx, 5+indx));
			indx++;
		}


		/*
		 <Grid Name="MessageGrid" Width="374" VerticalAlignment="Top" Background="#FFBEE7E8" Visibility="Collapsed">
            <Grid.RowDefinitions>
                <RowDefinition Height="32"></RowDefinition>
                <RowDefinition Height="*"></RowDefinition>
            </Grid.RowDefinitions>
            <TextBlock Grid.Row="0" HorizontalAlignment="Left" Margin="10,10,0,0" TextWrapping="Wrap" Text="Username" VerticalAlignment="Top" Height="32" Width="307" FontSize="14" FontFamily="Tahoma" FontWeight="Bold"/>
            <TextBlock Grid.Row="1" HorizontalAlignment="Left" Margin="10,0,0,0" TextWrapping="Wrap" Text="some message dddddddddddddddddddddddddddddddddddddddddddddddddd" VerticalAlignment="Top"  Width="300" FontFamily="Tahoma" FontSize="14"/>
        </Grid>
		 */
		private Grid createMessageGrid(string name, string message)
		{
			var g1 = GenericsWPF<Grid>.DeepDarkCopy(MessageGrid);
			g1.Visibility = Visibility.Visible;
			var ch = g1.Children;
			((TextBlock) ch[0]).Text = name;
			((TextBlock) ch[1]).Text = message;
			return g1;
		}

		
		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				string username = "me";
				MessageList.Items.Add(createMessageGrid(username, MessageTextBox.Text));
				MessageTextBox.Text = "";
			}
		}
	}
}
