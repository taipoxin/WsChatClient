using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Newtonsoft.Json;
using WebSocketSharp;

namespace ChatClient
{
	/// <summary>
	/// Логика взаимодействия для Signup.xaml
	/// </summary>
	public partial class Signup: Window
	{
		public Signup()
		{
			InitializeComponent();
		}

		private WsController wsController;
		private FileLogger l = new FileLogger("signup.txt");

		private double leftPos;
		private double topPos;
		private bool isPosition = false;

		public void SetWindowPositions(double left, double top)
		{
			leftPos = left;
			topPos = top;
			isPosition = true;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			l.logg("", false);
			if (isPosition)
			{
				this.Left = leftPos;
				this.Top = topPos;
			}
		}

		public void setWsController(WsController c)
		{
			c.setSignupWindow(this);
			wsController = c;
		}

		private RegRequest createRegRequest(string user, string email, string password)
		{
			RegRequest r = new RegRequest();
			r.type = "register";
			r.user = user;
			r.email = email;
			r.password = password;
			return r;
		}

		// return success
		private bool registerFromForm()
		{
			string user = LoginBox.Text;
			string email = EmailBox.Text;
			string pass = PasswordBox.Password;
			string cpass = ConfirmBox.Password;

			if (!pass.Equals(cpass))
			{
				return false;
			}
			string jsonReq = JsonConvert.SerializeObject(createRegRequest(user, email, pass));

			WebSocket w = wsController.getWs();
			// пытаемся отправить сообщение об регистрации
			if (w != null && w.IsAlive)
			{
				l.log("sending auth request");
				w.Send(jsonReq);
			}
			// если не получается, то траим
			else
			{
				l.log("reg fail");
				Thread t = new Thread(delegate() { checkConnectAndSendRequest(w, jsonReq); });
				t.IsBackground = true;
				t.Start();
			}
			return true;

		}

		private void checkConnectAndSendRequest(WebSocket w, String jsonReq)
		{
			while (w == null && w.IsAlive)
			{
				Thread.Sleep(100);
			}
			l.log("sending auth request");
			w.Send(jsonReq);
		}

		public void dispatchOpenSigninWindow()
		{
			Dispatcher.BeginInvoke(new ThreadStart(delegate {
				openSigninWindow();
			}));
		}


		private void openSigninWindow()
		{
			Signin w = new Signin();
			w.SetWindowPositions(this.Left, this.Top);
			// показываем новое окно
			w.Show();

			// закрываем текущее окно логина
			var window = Application.Current.Windows[0];
			if (window != null)
				window.Close();
		}

		// Cancel
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			openSigninWindow();

		}


		// Sign up
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{

			bool res = registerFromForm();
			if (res)
			{
				l.log("try to register");
			}
			else
			{
				l.log("bad data for register");
			}

		}

	}
}
