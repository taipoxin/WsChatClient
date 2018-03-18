using System;
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
	/// Логика взаимодействия для Signup.xaml
	/// </summary>
	public partial class Signup: Window
	{
		public Signup()
		{
			InitializeComponent();
		}

		private WsController wsController;

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


		private void formData()
		{
			string login = LoginBox.Text;
			string email = EmailBox.Text;

			string pass = PasswordBox.Password;
			string cpass = ConfirmBox.Password;

		}

		// Cancel
		private void Button_Click(object sender, RoutedEventArgs e)
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


		// Sign up
		private void Button_Click_1(object sender, RoutedEventArgs e)
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

		
	}
}
