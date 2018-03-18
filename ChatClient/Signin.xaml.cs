using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using WebSocketSharp;

namespace ChatClient
{
	/// <summary>
	/// Логика взаимодействия для Startup.xaml
	/// </summary>
	public partial class Signin: Window
	{
		public Signin()
		{
			InitializeComponent();
		}

		private WebSocket ws;

		private double leftPos;
		private double topPos;
		private bool isPosition = false;

		public void SetWindowPositions(double left, double top)
		{
			leftPos = left;
			topPos = top;
			isPosition = true;
		}

		private double getCenter(double resolution, double actual)
		{
			return (resolution - actual) / 2;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (isPosition)
			{
				this.Left = leftPos;
				this.Top = topPos;
			}
			// первый запуск, надо центрировать
			else
			{
				this.Top = getCenter(SystemParameters.PrimaryScreenHeight, ActualHeight);
				this.Left= getCenter(SystemParameters.PrimaryScreenWidth, ActualWidth);
			}
			// init ws
			ws = initWebSocket();
			StreamWriter writer = new StreamWriter("wsLog.txt", false);

		}

		private void log(string s)
		{
			StreamWriter writer = new StreamWriter("wsLog.txt", true);
			writer.WriteLine(s);
			writer.Close();
		}

		private WebSocket initWebSocket()
		{
			WebSocket ws = new WebSocket(Config.wsSource);
			ws.OnMessage += (sender, e) =>
			{
				AuthResponse resp = JsonConvert.DeserializeObject<AuthResponse>(e.Data);
				if ("authorize".Equals(resp.type))
				{
					// успешная авторизация
					if (resp.success)
					{
						log("success");
						Dispatcher.BeginInvoke(new ThreadStart(delegate
						{
							openMainWindow();
						}));
					}
				}
			};

			ws.Connect();
			return ws;
		}


		private void openMainWindow()
		{
			// в конце отображаем окно
			TestWindow w = new TestWindow();
			// показываем новое окно
			w.Show();

			// закрываем текущее окно логина
			var window = Application.Current.Windows[0];
			if (window != null)
			{
				window.Close();
			}
		}

		private void openSignUpWindow()
		{
			Signup w = new Signup();
			w.SetWindowPositions(this.Left, this.Top);
			// показываем новое окно
			w.Show();

			// закрываем текущее окно логина
			var window = Application.Current.Windows[0];
			if (window != null)
				window.Close();
		}

		private AuthRequest createRequest(string user, string password)
		{
			AuthRequest r = new AuthRequest();
			r.type = "authorize";
			r.user = user;
			r.password = password;
			return r;
		}

		// Sign in
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			// пока что используется как наводка на использование backend
			if (RememberCheck.IsChecked != null && (bool)RememberCheck.IsChecked)
			{
				string u = UserBox.Text;
				string p = PasswordBox.Password;
				string jsonReq = JsonConvert.SerializeObject(createRequest(u, p));
				ws.Send(jsonReq);
			}
			else
			{
				openMainWindow();
			}
		}

		// Create account
		private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			openSignUpWindow();
		}


	}

	public class AuthResponse
	{
		public string type;
		public bool success;
		public string[] online;
	}

	public class AuthRequest
	{
		public string type;
		public string user;
		public string password;

	}
}
