namespace Developmentor.Xml 
{
	using System;
	using System.Xml;
	using System.Xml.XPath;
	using System.Reflection;
	using System.Collections;
	using System.IO;
	using System.Diagnostics;

	public class XPathReader: XPathNavigator 
	{
		// TODO - Use XmlValidatingreader with ExpandEntities set
		public XmlTextReader Node;
		//private XmlTextReader SaveNode;

		public XPathReader( Stream stream ) 
		{
			Node = new XmlTextReader(stream);
		}

		public XPathReader( TextReader reader ) 
		{
			Node = new XmlTextReader(reader);
		}

		public XPathReader( String url ) 
		{
			Node = new XmlTextReader(url);
		}

		public XPathReader( XPathReader a ) 
		{
			this.Node = a.Node;
		}

		public override XPathNavigator Clone() 
		{
			return new XPathReader(this);
		}

		public override XPathNodeType NodeType 
		{ 
			get 
			{
				if (Node.ReadState == ReadState.Initial)
					return XPathNodeType.Root;

				switch (Node.NodeType) 
				{
					case XmlNodeType.Document : return XPathNodeType.Root;
					case XmlNodeType.XmlDeclaration: Debug.Assert(false,"Should not be here"); throw new Exception("Invalid node type");
					case XmlNodeType.CDATA: return XPathNodeType.Text;				
					case XmlNodeType.Element: return XPathNodeType.Element;
					case XmlNodeType.EndElement: Debug.Assert(false,"Should not be here"); throw new Exception("Invalid node type");
					case XmlNodeType.EntityReference: Debug.Assert(false,"Should not be here");throw new Exception("Invalid node type");
					case XmlNodeType.Attribute : return XPathNodeType.Attribute;
					case XmlNodeType.Text : return XPathNodeType.Text;
					case XmlNodeType.Whitespace : return XPathNodeType.Whitespace;
					case XmlNodeType.SignificantWhitespace : return XPathNodeType.SignificantWhitespace;
					case XmlNodeType.Comment : return XPathNodeType.Comment;
					case XmlNodeType.ProcessingInstruction : return XPathNodeType.ProcessingInstruction;
					case XmlNodeType.None : return XPathNodeType.Text;
						// TODO add others
					default : Debug.Assert(false,Node.NodeType.ToString()); throw new Exception("Invalid node type");
				}
			}
		}

		public override string LocalName 
		{ 
			get { return Node.LocalName; }
		}

		public override string Name 
		{ 
			get { return Node.Name; }
		}

		public override string NamespaceURI 
		{ 
			get { return Node.NamespaceURI; }
		}

		public override string Prefix 
		{ 
			get { return Node.Prefix; }
		}

		public override string Value 
		{ 
			get { return Node.Value; }
		}

		public override String BaseURI 
		{ 
			get { return Node.BaseURI; } 
		}

		public override String XmlLang 
		{ 
			get { return Node.XmlLang; }
		}

		public override bool IsEmptyElement 
		{ 
			get { return Node.IsEmptyElement; } 
		}

		public override XmlNameTable NameTable 
		{ 
			get { return Node.NameTable; }
		}

		public override bool HasAttributes 
		{ 
			get { return Node.HasAttributes; }
		}

		public override string GetAttribute( string localName, string namespaceURI ) 
		{
			return (Node.GetAttribute(localName,namespaceURI));
		}

		private string GetAttribute( string name ) 
		{
			return (Node.GetAttribute(name));
		}

		public override bool MoveToAttribute( string localName, string namespaceURI ) 
		{
			return (Node.MoveToAttribute(localName,namespaceURI));
		}

		public override bool MoveToFirstAttribute() 
		{
			return (Node.MoveToFirstAttribute());
		}

		public override bool MoveToNextAttribute() 
		{
			return (Node.MoveToNextAttribute());
		}

		public override string GetNamespace( string localName) 
		{
			return (GetAttribute(localName));
		}

		public override bool MoveToNamespace( string localName ) 
		{
			return MoveToAttribute(localName, @"http://www.w3.org/2000/xmlns/");
		}

		public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope) 
		{
			while (Node.MoveToNextAttribute())
			{
				if (Node.LocalName == "xmlns")
					return true;
			}
			return false;
		}

		public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope) 
		{
			while (Node.MoveToNextAttribute())
			{
				if (Node.LocalName == "xmlns")
					return true;
			}
			return false;
		}
    
		public override bool HasChildren 
		{ 
		// TODO
			get 
			{
				return false;
			}
		}
	
		public override bool MoveToNext() 
		{
			if (!Node.EOF)
			{
				Node.Skip();
				return (Node.NodeType != XmlNodeType.EndElement && !Node.EOF);
			}
			return false;
		}

		public override bool MoveToPrevious() 
		{
			throw (new Exception("Invalid XPath Expression for forward only stream"));

		}

		public override bool MoveToFirst() 
		{
			throw (new Exception("Invalid XPath Expression for forward only stream"));
		}

		public override bool MoveToFirstChild() 
		{
			if (Node.ReadState == ReadState.Initial)
			{
				Node.MoveToContent();
				Debug.Assert (Node.NodeType == XmlNodeType.Element);
				return true;
			}
			if (Node.NodeType == XmlNodeType.Element)
			{

				Node.Read();
				if (Node.NodeType != XmlNodeType.EndElement)
					return true;
			}

			return false;
		}

		public bool MoveToChild( int i ) 
		{
			throw (new Exception("Invalid XPath Expression for forward only stream"));
		}

		public override bool MoveToParent() 
		{
 			switch (Node.NodeType) 
			{
				case XmlNodeType.Document :	return false;
				case XmlNodeType.Element :	return false;
				case XmlNodeType.Text : return false;
				case XmlNodeType.Attribute :
					Node.MoveToElement();
					return true;
				default:
					return false;

			}			
		}

		public override void MoveToRoot() 
		{
			Debug.Assert(Node != null);
			if (Node.ReadState == ReadState.Initial)
				return;
			else
				throw (new Exception("Reader in not in initial state for Root node"));
		}

		public override bool MoveTo( XPathNavigator other ) 
		{
			((XPathReader)other).Node = this.Node;
			return true;
		}

		public override bool MoveToId( string id ) 
		{
			return false;
		}

		public override bool IsSamePosition( XPathNavigator other )
		{
			return true;
		}
	}
}
