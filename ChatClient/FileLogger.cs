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

		public void log(string s, bool toEnd)
		{
			StreamWriter writer = new StreamWriter(file, toEnd);
			writer.WriteLine(s);
			writer.Close();
		}

		public void log(string s)
		{
			log(s, true);
		}
	}
}
