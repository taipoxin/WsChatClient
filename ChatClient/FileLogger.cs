using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
	class FileLogger
	{
		private string file;

		public FileLogger(string f)
		{
			file = f;
		}
		// with \n
		public void log(string s, bool toEnd)
		{
			string ss = s;
			if (toEnd)
			{
				string dt = System.DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
				ss = "[" + dt + "]: " + s;
			}
			StreamWriter writer = new StreamWriter(file, toEnd);
			writer.WriteLine(ss);
			writer.Close();
		}
		// without \n
		public void logg(string s, bool toEnd)
		{
			string ss = s;
			if (toEnd)
			{
				string dt= System.DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
				ss = "[" + dt + "]: " + s;
			}
			StreamWriter writer = new StreamWriter(file, toEnd);
			writer.Write(ss);
			writer.Close();
		}

		public void log(string s)
		{
			log(s, true);
		}
	}
}
