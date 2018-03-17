using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
	[DataContract]
	internal class MessageBean
	{
		internal MessageBean(string sender, string message, string time)
		{
			this.sender = sender;
			this.message = message;
			this.time = time;
		}

		[DataMember] internal string sender;
		[DataMember] internal string message;
		[DataMember] internal string time;

		public override string ToString()
		{
			return $"{nameof(sender)}: {sender}, {nameof(message)}: {message}, {nameof(time)}: {time}";
		}
	}
}
