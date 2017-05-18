// Aaron Skonnard <http://staff.develop.com/aarons>

namespace Developmentor.Xml
{
  using System;
  using System.Xml;	
  using System.Xml.XPath;
  using System.Reflection;
  
  // factory interface for instantiating a file-based navigator
  public interface IFileNavigatorFactory
  {
	// instantiates an XPathNavigator for the specified file
	XPathNavigator CreateNavigator(string file);	
  }
  
  // factory class for XmlDocument navigators (.xml files)
  public class XmlDocumentNavigatorFactory : IFileNavigatorFactory
  {
	public XPathNavigator CreateNavigator(string file)
	{
	  XmlDocument doc = new XmlDocument();
	  try 
	  {
		doc.Load(file);
		return ((IXPathNavigable)doc).CreateNavigator();
	  }
	  catch
	  {
		// ignore exception - we might run across ill-formed XML 
		// documents while evaluating XPath expressions
	  }
	  return null;
	}
  }

  /*
  public class ZipNavigatorFactory : IFileNavigatorFactory
  {
	public XPathNavigator CreateNavigator(string file)
	{
	  try 
	  {
		return new ZipNavigator(file);
	  }
	  catch(Exception e)
	  {
		// ignore exception - we might run across ill-formed XML 
		// documents while evaluating XPath expressions
	  }
	  return null;
	}
  }
  */

  public class AssemblyNavigatorFactory : IFileNavigatorFactory
  {
	public XPathNavigator CreateNavigator(string file)
	{
	  try 
	  {
		return new AssemblyNavigator(file);
	  }
	  catch(Exception e)
	  {
		Console.WriteLine(e.Message);
		// ignore exception - we might run across ill-formed XML 
		// documents while evaluating XPath expressions
	  }
	  return null;
	}
  }

  // maps a file extension to a class that implements 
  // IFileNavigatorFactory whenever a file is encountered 
  // with the given extension, an instance of the specified 
  // class will be created for navigating *into* the file
  internal class FileHandler
  {
	public string extension;
	// name of assembly containing type
	public string assembly; 
	// name of a class that implements IFileNavigatorFactory
	public string type;     
	
	internal FileHandler(string ext, string t, string a)
	{
	  this.extension = ext;
	  this.type = t;
	  this.assembly = a;
	  // make sure we can load the assembly and create the class
	  if (null == this.CreateNavigatorFactory())
		throw new Exception("###error instantiating type: " + t);
	}

	// loads the assembly and instantiates the navigator factory
	// class for this file extension
	internal IFileNavigatorFactory CreateNavigatorFactory()
	{
	  Assembly ass = Assembly.Load(this.assembly);
	  if (ass == null)
		throw new Exception("###error loading assembly: " + 
							this.assembly);
	  return (IFileNavigatorFactory)ass.CreateInstance(
		 this.type, false, 0, null, null, null, null);
	}

	internal XPathNavigator CreateNavigator(string file)
	{
	  IFileNavigatorFactory f = this.CreateNavigatorFactory();
	  return f.CreateNavigator(file);	
	}
  }
}






