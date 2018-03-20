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

		private WsController wsController;
		private FileLogger l = new FileLogger("signin.txt");

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
			l.logg("", false);
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
			//init wsController
			wsController = initWsController();
			Visibility = Visibility.Visible;
		}

		private WsController initWsController()
		{
			WsController c = new WsController();
			c.setSigninWindow(this);
			return c;
		}

		public void setWsController(WsController c)
		{
			c.setSigninWindow(this);
			wsController = c;
		}


		public void dispatchOpenMainWindow()
		{
			Dispatcher.BeginInvoke(new ThreadStart(delegate
			{
				openMainWindow();
			}));
		}


		public void openMainWindow()
		{
			TestWindow w = new TestWindow();
			w.setWsController(wsController);
			w.Show();

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
			w.setWsController(wsController);
			w.Show();

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
				Config.userName = u;
				string p = PasswordBox.Password;
				string jsonReq = JsonConvert.SerializeObject(createRequest(u, p));
				
				WebSocket w = wsController.getWs();
				// пытаемся отправить сообщение об авторизации
				if (w != null && w.IsAlive)
				{
					l.log("sending auth request");
					w.Send(jsonReq);
				}
				// если не получается, то 
				else
				{
					l.log("auth fail");
					Thread t = new Thread(delegate() { checkConnectAndSendRequest(w, jsonReq); });
					t.IsBackground = true;
					t.Start();
				}
			}
			else
			{
				openMainWindow();
			}
		}

		private void checkConnectAndSendRequest(WebSocket w, String jsonReq)
		{
			while (w == null || w.IsAlive)
			{
				Thread.Sleep(100);
			}
			l.log("sending auth request");
			w.Send(jsonReq);
		}

		// Create account
		private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			openSignUpWindow();
		}


	}

	
}
