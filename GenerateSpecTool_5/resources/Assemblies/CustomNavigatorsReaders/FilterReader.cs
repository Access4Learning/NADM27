// Aaron Skonnard <http://staff.develop.com/aarons>

namespace Developmentor.Xml
{
  using System;
  using System.Xml;
  using System.Xml.XPath;
  using System.IO;

  public class FilterReader : NavigatorReader
  {
	string localName;
	string namespaceURI;
	int inFilterElement;
	bool rootElement;
	bool movedToRoot;

	public FilterReader(XPathNavigator nav, string localName, string namespaceURI) : base(nav) 
	{
	  this.localName = localName;
	  this.namespaceURI = namespaceURI;
	  this.inFilterElement = 0;
	  this.rootElement = false;	
	  this.movedToRoot = false;
	}

	public override bool Read()
	{
	  if (!movedToRoot)
	  {
		rootElement = true;
		movedToRoot = true;
		return true;
	  }
	  if (rootElement)
		rootElement = false;
	  if (inFilterElement > 0)
	  {
		bool more = base.Read();
		if (this.NodeType == XmlNodeType.EndElement && 
			this.LocalName.Equals(this.localName) && 
			this.NamespaceURI.Equals(this.namespaceURI))
		  inFilterElement--;
		return more;
	  }
	  else
	  {
		while (base.Read())
		{
		  if (this.LocalName.Equals(this.localName) && 
			  this.NamespaceURI.Equals(this.namespaceURI))
		  {
			inFilterElement++;
			return true;
		  }
		}				
	  }
	  return false;
	}
		
	public override string LocalName 
	{
	  get
	  {
		if (rootElement)
		  return "root";
		else
		  return base.LocalName;
	  }
	}

	public override string Name
	{
	  get
	  {
		if (rootElement)
		  return "root";
		else
		  return base.Name;
	  }
	}

	public override string NamespaceURI
	{
	  get
	  {
		if (rootElement)
		  return "";
		else
		  return base.NamespaceURI;
	  }
	}

	public override XmlNodeType NodeType
	{
	  get
	  {
		if (rootElement)
		  return XmlNodeType.Element;
		else
		  return base.NodeType;
	  }
	}
  }
}



