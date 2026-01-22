using System;

// ReSharper disable CheckNamespace
namespace CrowRx.Editor
{
	public class EditorWindowCrowRxSerializeFieldAttribute : Attribute
	{
		public string _displayContents;

		public EditorWindowCrowRxSerializeFieldAttribute(string display_contents)
		{
			_displayContents = display_contents;
		}
	}
}