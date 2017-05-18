// Aaron Skonnard <http://staff.develop.com/aarons>

namespace Developmentor.Xml
{
  using System;
  using System.Xml;
  using System.Xml.XPath;

  public class Serializer
  {
	public static void SerializeNode(XmlWriter w, XmlReader reader, bool descendants, bool attributes)
	{
	  switch (reader.NodeType)
	  {
	  case XmlNodeType.Element:
		w.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
		if (attributes)
		{
		  while (reader.MoveToNextAttribute())
		  {
			w.WriteStartAttribute(reader.Prefix, reader.LocalName,
								  reader.NamespaceURI);
			w.WriteString(reader.Value);
			w.WriteEndAttribute();
		  }
		}
		if (descendants)
		{
		  while (reader.Read())
			SerializeNode(w, reader, descendants, attributes);
		}
		break;
	  case XmlNodeType.Text:
		w.WriteString(reader.Value);
		break;
	  case XmlNodeType.CDATA:
		w.WriteCData(reader.Value);
		break;
	  case XmlNodeType.ProcessingInstruction:
		w.WriteProcessingInstruction(reader.Name, reader.Value);
		break;
	  case XmlNodeType.Comment:
		w.WriteComment(reader.Value);
		break;
	  case XmlNodeType.Whitespace:
	  case XmlNodeType.SignificantWhitespace:
		// ignore whitespace
		break;
	  case XmlNodeType.EndElement:
		w.WriteEndElement();
		break;
	  }
	}

	public static void SerializeNode(XmlWriter w, XPathNavigator nav, bool descendants, bool attributes)
	{
	  switch (nav.NodeType)
	  {
	  case XPathNodeType.Element:
		w.WriteStartElement(nav.Prefix, nav.LocalName, nav.NamespaceURI);
		if (attributes)
		{
		  if (nav.MoveToFirstAttribute())
		  {
			w.WriteStartAttribute(nav.Prefix, nav.LocalName, nav.NamespaceURI);
			w.WriteString(nav.Value);
			w.WriteEndAttribute();

			while (nav.MoveToNextAttribute())
			{
			  w.WriteStartAttribute(nav.Prefix, nav.LocalName, nav.NamespaceURI);
			  w.WriteString(nav.Value);
			  w.WriteEndAttribute();
			}
			nav.MoveToParent();
		  }
		}
		if (descendants)
		{
		  if (nav.HasChildren)
		  {
			bool more = nav.MoveToFirstChild();
			while (more)
			{
			  SerializeNode(w, nav, descendants, attributes);
			  more = nav.MoveToNext();
			}
			nav.MoveToParent();
		  }
		}
		w.WriteEndElement();
		break;
	  case XPathNodeType.Text:
		w.WriteString(nav.Value);
		break;
	  case XPathNodeType.ProcessingInstruction:
		w.WriteProcessingInstruction(nav.Name, nav.Value);
		break;
	  case XPathNodeType.Comment:
		w.WriteComment(nav.Value);
		break;
	  case XPathNodeType.Whitespace:
	  case XPathNodeType.SignificantWhitespace:
		// ignore whitespace
		break;
	  }			
	}	
  }
}
