namespace Developmentor.Xml
{
  using System;
  using System.Xml;
  using System.Xml.XPath;
  using System.IO;
  using System.Collections;

  public class AssemblyReader : NavigatorReader
  {
	public AssemblyReader(string assemblyName) : base(new AssemblyNavigator(assemblyName)) {}
  }

  /*
  public class ZipReader : NavigatorReader
  {
	public ZipReader(string zipFileName) : base(new ZipNavigator(zipFileName)) {}
  }
  */

  public class RegistryReader : NavigatorReader
  {
	public RegistryReader() : base(new RegistryNavigator()) {}
  }

  public class FileSystemReader : NavigatorReader
  {
	FileSystemNavigator nav;
	
	public FileSystemReader(FileSystemNavigator nav) : base(nav) {this.nav = nav;}
	
	public bool RegisterFileHandler(string extension, string type, string assembly)
	{
	  return nav.RegisterFileHandler(extension, type, assembly);
	}
  }

  public class NavigatorReader : XmlReader 
  {
	private XPathNavigator cursor;
	private bool firstReadCall = false;
	private bool closeCall = false;
	private bool endElement = false;
    private bool secondAttributeReadCall = false;
	private XPathNavigator eof;

	private bool IsEndElement
	{
	  get 
	  {
		return endElement && 
		  cursor.NodeType == XPathNodeType.Element;
	  }
	}

	public NavigatorReader(XPathNavigator nav) 
	{
	  this.cursor = nav;
	  //this.cursor.MoveToRoot();
	  this.eof = nav.Clone();
	  this.eof.MoveToParent();
	}

	public override XmlNodeType NodeType 
	{
	  get 
	  {
		// map the XPathNodeType values to the
		// XmlNodeType values
		switch(cursor.NodeType)
		{
		case XPathNodeType.Root:
		  return XmlNodeType.Document;
		case XPathNodeType.Element:
		  if (IsEndElement)
			return XmlNodeType.EndElement;
		  else
			return XmlNodeType.Element;
		case XPathNodeType.Attribute:
		  return XmlNodeType.Attribute;
		case XPathNodeType.Text:
		  return XmlNodeType.Text;
		case XPathNodeType.Comment:
		  return XmlNodeType.Comment;
		case XPathNodeType.ProcessingInstruction:
		  return XmlNodeType.ProcessingInstruction;
		case XPathNodeType.Whitespace:
		  return XmlNodeType.Whitespace;
		case XPathNodeType.SignificantWhitespace:
		  return XmlNodeType.SignificantWhitespace;
		default:
		  return XmlNodeType.None;
		}
	  }
	}

	public override string Name 
	{
	  get 
	  {
		return cursor.Name;
	  }
	}

	public override string LocalName 
	{
	  get 
	  {
		return cursor.LocalName;
	  }
	}

	public override string NamespaceURI 
	{
	  get 
	  {
		return cursor.NamespaceURI;
	  }
	}

	public override string Prefix 
	{
	  get 
	  {
		return cursor.Prefix;
	  }
	}

	public override bool HasValue 
	{
	  get 
	  {
		if (cursor.NodeType == XPathNodeType.Attribute || 
			cursor.NodeType == XPathNodeType.Comment ||
			cursor.NodeType == XPathNodeType.ProcessingInstruction ||
			cursor.NodeType == XPathNodeType.Text ||
			cursor.NodeType == XPathNodeType.Whitespace ||
			cursor.NodeType == XPathNodeType.SignificantWhitespace)
		  return true;
		else
		  return false;
	  }
	}

	public override string Value 
	{
	  get 
	  {
		return cursor.Value;
	  }
	}

	public override string BaseURI 
	{
	  get 
	  {
		return cursor.BaseURI; 
	  }
	}

	public override string XmlLang 
	{
	  get 
	  {
		return cursor.XmlLang;
	  }
	}

	public override int Depth 
	{
	  get 
	  {
		XPathNavigator clone = cursor.Clone();
		if (cursor.NodeType == XPathNodeType.Attribute)
		  clone.MoveToParent(); //moves to element node
		// now start counting levels for depth
		int depth = 0;
		while (clone.MoveToParent())
		  depth++;
		return depth;  
	  }
	}

	public override bool IsEmptyElement 
	{
	  get 
	  {
		return cursor.NodeType == XPathNodeType.Element && 
		  !cursor.HasChildren;
	  }
	}

	public override bool IsDefault 
	{
	  get 
	  {
		return false;
	  }
	}

	public override int AttributeCount 
	{
	  get 
	  {
		if (cursor.NodeType != XPathNodeType.Element)
		  return 0;
		else
		{
		  XPathNavigator clone = cursor.Clone();
		  int count=0;
		  if (clone.MoveToFirstAttribute())
			count++;
		  while (clone.MoveToNextAttribute())
			count++;
		  return count;
		}
	  }
	}

	public override string this[int index] 
	{
	  get 
	  {
		if (cursor.NodeType != XPathNodeType.Element)
		  return String.Empty;
		else
		{
		  XPathNavigator clone = cursor.Clone();
		  if (clone.MoveToFirstAttribute())
		  {
			if (index == 0)
			  return clone.Value;
			else
			{
			  for (int i=1; i<=index; i++)
			  {
				if (!clone.MoveToNextAttribute())
				  return String.Empty;
			  }
			  return clone.Value;
			}
		  }
		  return String.Empty;
		}
	  }
	}

	public override string this[string name] 
	{
	  get 
	  {
		return cursor.GetAttribute(name, "");
	  }
	}

	public override string this[string localName, string nsURI] 
	{
	  get 
	  {
		return cursor.GetAttribute(localName, nsURI);
	  }
	}

	public override bool EOF 
	{
	  get 
	  {
		XPathNavigator temp = cursor.Clone();
		temp.MoveToParent(); 

	    XmlNodeOrder order = temp.ComparePosition(this.eof);

		return this.endElement && order == XmlNodeOrder.Same;
		  //temp.NodeType == XPathNodeType.Root;
	  }
	}

	public override ReadState ReadState 
	{
	  get 
	  {
		if (!firstReadCall)
		  return ReadState.Initial;
		else if (closeCall)
		  return ReadState.Closed;
		else if (EOF)
		  return ReadState.EndOfFile;
		else
		  return ReadState.Interactive;
	  }
	}

	public override bool HasAttributes 
	{
	  get 
	  {
		return cursor.HasAttributes;
	  }
	}

	public override XmlNameTable NameTable 
	{
	  get 
	  {
		return cursor.NameTable;
	  }
	}

	public override Char QuoteChar 
	{
	  get 
	  {
		return '\'';
	  }
	}

	public override XmlSpace XmlSpace 
	{
	  get 
	  {
		return XmlSpace.Default;
	  }
	}

	public override bool ReadAttributeValue() 
	{
		if (cursor.NodeType == XPathNodeType.Attribute)
		{
			if (secondAttributeReadCall)
				return (secondAttributeReadCall = false);

			secondAttributeReadCall = true;
			bool result = cursor.MoveToFirstChild();
			return true;
		}
			//else if (cursor.NodeType == XPathNodeType.Text)
			//	return cursor.MoveToNext();
		else
		{
			secondAttributeReadCall = false;
			XPathNavigator parent = cursor.Clone();
			parent.MoveToParent();
			if (parent.NodeType == XPathNodeType.Attribute)
				return cursor.MoveToNext();
		}
	  return false;
	}

	public override string LookupNamespace(string prefix) 
	{
	  return cursor.GetNamespace(prefix); 
	}

	public override void Close() 
	{
	  closeCall = true;
	  cursor = null;
	}

	public override bool Read() 
	{
	  firstReadCall = true;

	  // if on attribute, move to parent before 
	  // processing Read operation
      MoveToElement();

	  // if on an EndElement, move to next sibling (if any)
	  // or move up to parent
	  if (IsEndElement)
	  {
		if (cursor.MoveToNext())
		{
		  this.endElement = false;
		  return true;
		}
		else
		{
		  if (EOF)
			return false;
		  else
		  {
			cursor.MoveToParent();
			return this.endElement = true;
		  }
		}
	  }
	  else if (cursor.HasChildren)
		return cursor.MoveToFirstChild();
	  else
	  {
		if (cursor.MoveToNext())
		  return true;
		else
		{
		  if (EOF)
			return false;
		  else
		  {
			cursor.MoveToParent();
			return this.endElement = true;
		  }
		}
	  }
	}

	public override bool MoveToElement() 
	{
	  if (cursor.NodeType == XPathNodeType.Text)
	  {
	    XPathNavigator temp = cursor.Clone();
		temp.MoveToParent();
		if (temp.NodeType == XPathNodeType.Attribute)
		  cursor.MoveToParent();
	  }
	  if (cursor.NodeType == XPathNodeType.Attribute)
		cursor.MoveToParent();
	  return cursor.NodeType == XPathNodeType.Element;
	}

	public override bool MoveToFirstAttribute() 
	{
	  return cursor.MoveToFirstAttribute();
	}

	public override bool MoveToNextAttribute() 
	{
	  return cursor.MoveToNextAttribute();
	}

	public override void MoveToAttribute(int index) 
	{
	  if (cursor.NodeType != XPathNodeType.Element)
		return;
	  else
	  {
		XPathNavigator clone = cursor.Clone();
		if (clone.MoveToFirstAttribute())
		{
		  if (index == 0)
		  {
			cursor.MoveTo(clone);
			return;
		  }
		  else
		  {
			for (int i=1; i<=index; i++)
			{
			  if (!clone.MoveToNextAttribute())
				return;
			}
			cursor.MoveTo(clone);
			return;
		  }
		}
		return;
	  }
	}

	public override bool MoveToAttribute(string name, string ns) 
	{
	  return cursor.MoveToAttribute(name, ns);
	}

	public override bool MoveToAttribute(string name) 
	{
	  return cursor.MoveToAttribute(name, "");
	}

	public override string GetAttribute(int i) 
	{
	  return this[i];
	}

	public override string GetAttribute(string name, string namespaceURI) 
	{
	  return cursor.GetAttribute(name, namespaceURI);
	}

	public override string GetAttribute(string name) 
	{
	  return cursor.GetAttribute(name, "");
	}	    

	public override void ResolveEntity() 
	{
	  return;
	}		

	public override string ReadInnerXml() 
	{
	  TextWriter tw = new StringWriter();
	  switch(cursor.NodeType)
	  {
	  case XPathNodeType.Element:
		Stack elemScope = new Stack();
		while (Read())
		{
		  if (NodeType == XmlNodeType.Element)
			elemScope.Push(1);
		  else if (NodeType == XmlNodeType.EndElement)
		  {
			if (elemScope.Count == 0)
			  break;
			else 
			  elemScope.Pop();
		  }
		  SerializeNode(tw);
		}
		break;
	  case XPathNodeType.Attribute:
		SerializeNode(tw); 
		break;
	  default:
		return String.Empty;
	  }
	  return tw.ToString();
	}

	public override string ReadOuterXml()
	{
	  TextWriter tw = new StringWriter();
	  switch(cursor.NodeType)
	  {
	  case XPathNodeType.Element:
		SerializeNode(tw);
		tw.Write(ReadInnerXml());
		SerializeNode(tw);
		Read(); // move off end element
		break;
	  case XPathNodeType.Attribute:
		SerializeNode(tw); 
		break;
	  default:
		return String.Empty;
	  }
	  return tw.ToString();
	}

	public override string ReadString() 
	{
	  string value = "";
	  switch (NodeType)
	  {
	  case XmlNodeType.Attribute:
		while (ReadAttributeValue())
		  value += Value;
		break;
	  case XmlNodeType.Text:
	  case XmlNodeType.Element:
		value += Value;
		while (Read())
		{
		  if (NodeType == XmlNodeType.Text ||
			  NodeType == XmlNodeType.Whitespace ||
			  NodeType == XmlNodeType.SignificantWhitespace)
			value += Value;
		  else
			break;
		}
		break;
	  }
	  return value;
	}

	private void WriteAttribute(TextWriter w)
	{
	  if (!Prefix.Equals(String.Empty))
		w.Write(" {0}:{1}='{2}'", Prefix, LocalName, Value);
	  else
		w.Write(" {0}='{1}'", LocalName, Value);
	}

	public void SerializeNode(TextWriter w)
	{
	  switch (NodeType)
	  {
	  case XmlNodeType.Element:
		// write start tag
		if (!Prefix.Equals(String.Empty))
		  w.Write("<{0}:{1}", Prefix, LocalName);
		else
		  w.Write("<{0}", LocalName);
		if (cursor.MoveToFirstAttribute())
		  WriteAttribute(w);
		while (cursor.MoveToNextAttribute())
		  WriteAttribute(w);
		if (cursor.MoveToFirstNamespace())
		  WriteAttribute(w);
		if (cursor.MoveToNextNamespace())
		  WriteAttribute(w);
		if (NodeType != XmlNodeType.Element)
		  cursor.MoveToParent();
		w.Write(">");
		break;
	  case XmlNodeType.EndElement:
		// write end tag
		if (!Prefix.Equals(String.Empty))
		  w.Write("</{0}:{1}>", Prefix, LocalName);
		else
		  w.Write("</{0}>", LocalName);
		break;
	  case XmlNodeType.Attribute:
		WriteAttribute(w);
		break;
	  case XmlNodeType.Text:
		w.Write(Value);
		break;
	  case XmlNodeType.ProcessingInstruction:
		w.Write("<?{0} {1}?>", Name, Value);
		break;
	  case XmlNodeType.Comment:
		w.Write("<!--{0}-->", Value);
		break;
	  case XmlNodeType.Whitespace:
	  case XmlNodeType.SignificantWhitespace:
		w.Write(Value);
		break;
	  }
	}
  }
}







