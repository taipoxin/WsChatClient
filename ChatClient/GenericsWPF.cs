using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Xml;
using System.IO;

namespace ChatClient
{
	class GenericsWPF<T>
	{
		/// Generic Method to perform Deep-Copy of a WPF element (e.g. UIElement)
		public static T DeepDarkCopy(T element)
		{
			var xaml = XamlWriter.Save(element);
			var xamlString = new StringReader(xaml);
			var xmlTextReader = new XmlTextReader(xamlString);
			var deepCopyObject = (T)XamlReader.Load(xmlTextReader);
			return deepCopyObject;
		}
	}
}
