// Aaron Skonnard <http://staff.develop.com/aarons>

namespace Developmentor.Xml
{
  using System;
  using System.IO;
  using System.Xml;	
  using System.Xml.XPath;
  using System.Collections;

  /// <summary>
  ///   
  /// FileSystemNavigator exposes the file system as one big XML
  /// document. Here is an example:
  ///
  /// <code><![CDATA[
  /// <mycomputer>
  ///   <c type='dir' creationTime='2001-03-03T16:48:43' 
  ///      lastAccessTime='2001-03-06T17:02:02' 
  ///      lastWriteTime='2001-03-06T13:15:48' 
  ///      fullName='C:\'>
  ///     <temp type='dir' creationTime='2001-03-03T16:48:43' 
  ///        lastAccessTime='2001-03-06T17:02:02' 
  ///        lastWriteTime='2001-03-06T13:15:48' 
  ///        fullName='C:\temp'>
  ///       <invoice.xml type='file' 
  ///          creationTime='2000-12-08T18:03:26' 
  ///          lastAccessTime='2001-03-06T17:03:44' 
  ///          lastWriteTime='2001-03-06T09:34:03' 
  ///          fullName='C:\temp\invoice.xml' length='525'>
  ///         <Invoice>
  ///           <InvoiceID>1000</InvoiceID>
  ///           <LineItems>
  ///             <LineItem id='li1'>
  ///               <Sku>134</Sku>
  ///               <Description>Dons Boxers</Description>
  ///               <Price>9.95</Price>
  ///             </LineItem>
  ///           </LineItems>
  ///         </Invoice>
  ///       </invoice.xml>
  ///     </temp>
  ///   </c>
  /// </mycomputer>
  /// ]]></code>
  /// 
  /// <p>Every file/directory is exposed as an element while the 
  /// file/directory properties are exposed as attributes of the 
  /// element. The lower-cased file/directory name becomes the 
  /// name of the element. If the file/directory name is an 
  /// invalid XML name (as per XML 1.0), it's encoded first using 
  /// System.Xml.XmlConvert.EncodeLocalName. For example, a name 
  /// like "Program Files" becomes "program_x0020_files".</p>
  /// 
  /// <p>The root of the file system is exposed through the 
  /// "mycomputer" element. Each available logical drive is 
  /// exposed as a child element of mycomputer, whose name is the 
  /// lower-case drive letter.</p>
  ///    
  /// </summary>
  public class FileSystemNavigator : XPathNavigator
  {	  
	// maintains mapping to underlying file system
	private FSEntryState state;
	// stores all registered custom file navigators
	private Hashtable fileHandlers = new Hashtable();
	// holds atomized names for prefix, local name, & namespace
	private XmlNameTable nameTable = new NameTable();

	public FileSystemNavigator()
	{
	  this.state = new FSEntryState(null, null, -1, "#document", this);
	}

	private FileSystemNavigator(FSEntryState s)
	{
	  this.state = s;
	}

	public override XPathNavigator Clone()
	{
	  FileSystemNavigator fsn = 
		new FileSystemNavigator(this.state.Clone());
	  fsn.fileHandlers = this.fileHandlers;
	  fsn.nameTable = this.nameTable;
	  return fsn;
	}

	public void WriteDebugMsg(string msg)
	{
#if false
	  Console.WriteLine(msg);
#endif
	}
				
	public override string BaseURI
	{
	  get
	  {
		WriteDebugMsg("BaseURI");
		return String.Empty;
	  }
	}

	public override string XmlLang
	{
	  get
	  {
		WriteDebugMsg("XmlLang");
		return String.Empty;
	  }
	}

	public override XPathNodeType NodeType
	{ 
	  get 
	  {
		WriteDebugMsg("NodeType");
		if (state.InNestedNavigator)
		  return state.nestedNavigator.NodeType;
		else if (state.IsDocument)
		  return XPathNodeType.Root;
		else if (state.IsAttribute)
		  return XPathNodeType.Attribute;
		else if (state.IsTextNode)
		  return XPathNodeType.Text;
		else 
		  return XPathNodeType.Element;
	  }
	}
   
	public override string LocalName 
	{ 
	  get 
	  {
		WriteDebugMsg("LocalName: " + Name);
		if (state.InNestedNavigator)
		  return nameTable.Add(state.nestedNavigator.LocalName);
		return nameTable.Add(state.Name);
	  }
	}

	public override string NamespaceURI 
	{ 
	  get 
	  { 
		WriteDebugMsg("NamespaceURI");
		if (state.InNestedNavigator)
		  return nameTable.Add(state.nestedNavigator.NamespaceURI);
		return nameTable.Add(string.Empty); 
	  } 
	}

	public override string Name 
	{ 
	  get 
	  { 
		WriteDebugMsg("Name: " + state.Name);
		return nameTable.Add(state.Name);
	  }
	}

	public override string Prefix 
	{ 
	  get 
	  { 
		WriteDebugMsg("Prefix");
		if (state.InNestedNavigator)
		  return nameTable.Add(state.nestedNavigator.Prefix);
		return nameTable.Add(string.Empty); 
	  }
	}

	public override string Value 
	{ 
	  get 
	  {
		WriteDebugMsg("Value");
		if (state.InNestedNavigator)
		  return state.nestedNavigator.Value;
		if (state.IsAttribute || state.IsTextNode)
		  return GetAttributeValue();
		return ""; 
	  } 
	}

	public override bool IsEmptyElement
	{ 
	  get
	  { 
		WriteDebugMsg("IsEmptyElement");
		if (state.InNestedNavigator)
		  return state.nestedNavigator.IsEmptyElement;
		if (state.IsAttribute || state.IsTextNode)
		  return false;
		return !HasChildren;
	  }
	}

	public override XmlNameTable NameTable
	{
	  get 
	  {
		WriteDebugMsg("NameTable");
		if (state.InNestedNavigator)
		  return state.nestedNavigator.NameTable;
		return nameTable; 
	  }
	}

	public override bool HasAttributes
	{
	  get
	  {
		WriteDebugMsg("HasAttributes");
		if (state.InNestedNavigator)
		  return state.nestedNavigator.HasAttributes;
		return state.IsFileOrDirectory; 
	  }
	}

	private string GetAttributeValue()
	{
	  if (state.IsTextNode && state.parent.IsAttribute)  // this is an attribute #text node
		return state.parent.GetAttribute(state.parent.Name, ""); 
	  else if (state.indexOfAttribute != -1)
		return state.GetAttribute(state.Name, "");
	  return "";
	}

	public override string GetAttribute( string localName, string namespaceURI )
	{
	  WriteDebugMsg("GetAttribute(" + localName + "," + namespaceURI + ")");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.GetAttribute(localName, namespaceURI);
	  else 
		return state.GetAttribute(localName, namespaceURI);
	}

	public override bool MoveToAttribute( string localName, string namespaceURI )
	{
	  WriteDebugMsg("MoveToAttribute");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToAttribute(localName, namespaceURI);
	  if (namespaceURI.Equals(""))
	  {
		if (state.IsFileOrDirectory)
		{
		  for (int i=0; i<state.AttributeCount; i++)
		  {
			if (FSEntryState.attributeNames[i].Equals(localName))
			{
			  state.indexOfAttribute = i;
			  return true;
			}
		  }
		}
		return false;
	  }
	  return false;
	}
		
	public override bool MoveToFirstAttribute()
	{
	  WriteDebugMsg("MoveToFirstAttribute");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToFirstAttribute();
	  else
	  {
		if (state.IsFileOrDirectory)
		{		
		  state.indexOfAttribute = 0;
		  return true;
		}
		return false;
	  }
	}

	public override bool MoveToNextAttribute()
	{
	  WriteDebugMsg("MoveToNextAttribute");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToNextAttribute();
	  else
	  {
		if (state.IsTextNode)
		  MoveToParent();
		if (state.IsFileOrDirectory)
		{				
		  if (state.indexOfAttribute < state.AttributeCount-1)
		  {
			state.indexOfAttribute++;
			return true;
		  }
		}
		return false;
	  }
	}

	public override string GetNamespace(string prefix)
	{
	  WriteDebugMsg("GetNamespace");
	  return String.Empty;
	}

	public override bool MoveToNamespace(string prefix)
	{
	  WriteDebugMsg("MoveToNamespace");
	  return false;
	}

	public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
	{
	  WriteDebugMsg("MoveToFirstNamespace");
	  return false;
	}

	public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
	{
	  WriteDebugMsg("MoveToNextNamespace");
	  return false;
	}
		
	public override bool HasChildren 
	{ 
	  get 
	  {
		WriteDebugMsg("HasChildren");
		if (state.InNestedNavigator)
		  return state.nestedNavigator.HasChildren;
		else
		{
		  if (state.IsFile)
		  {
			XPathNavigator nav = LookupFileHandler(state.currentEntry);
			if (nav != null)
			{
			  nav.MoveToRoot();
			  return nav.HasChildren;
			}
			else return false;
		  }
		}
		return (state.ChildCount > 0);
	  }
	}
				
	private bool UpdateState(FSEntryState s)
	{
	  if (s == null)
		return false;
	  else
	  {
		state = s;
		return true;
	  }
	}

	public override bool MoveToNext()
	{
	  WriteDebugMsg("MoveToNext");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToNext();
	  if (state.IsTextNode || state.IsAttribute)
		return false;

	  FSEntryState p = state.parent;
	  if (p!=null && (state.IndexInParent+1 < p.ChildCount))
	  {
		FSEntryState newState = p.OpenChildEntry(state.IndexInParent+1);
		return UpdateState(newState);
	  }
	  return false;
	}
		
	public override bool MoveToPrevious()
	{
	  WriteDebugMsg("MoveToPrevious");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToPrevious();
	  if (state.IsTextNode || state.IsAttribute)
		return false;

	  FSEntryState p = state.parent;
	  if (p!=null && (state.IndexInParent-1 >= 0))
	  {
		FSEntryState newState = p.OpenChildEntry(state.IndexInParent-1);
		return UpdateState(newState);
	  }
	  return false;
	}
   
	public override bool MoveToFirst()
	{
	  WriteDebugMsg("MoveToFirst");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToFirst();
	  if (state.IsTextNode || state.IsAttribute)
		return false;

	  FSEntryState p = state.parent;
	  if (p!=null)
	  {
		FSEntryState newState = p.OpenChildEntry(0);
		return UpdateState(newState);
	  }
	  return false;
	}

	public override bool MoveToFirstChild()
	{
	  WriteDebugMsg("MoveToFirstChild");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToFirstChild();
	  if (state.IsTextNode)
		return false;

	  FSEntryState newState = state.OpenChildEntry(0);
	  return UpdateState(newState);
	}

	public override bool MoveToParent()
	{
	  WriteDebugMsg("MoveToParent");
	  if (state.InNestedNavigator)
	  {
		bool bParent = state.nestedNavigator.MoveToParent();
		if (bParent)
		{
		  if (state.nestedNavigator.LocalName.Equals("#document"))
		  {
			// state.InNestedNavigator == false from now on
			state.nestedNavigator = null;
		  }
		  return true;				
		}
		else
		  return false;
	  }
	  if (state.IsAttribute)
	  {
		state.indexOfAttribute = -1;
		return true;
	  }

	  state = state.parent;
	  return (state != null);
	}

	public override void MoveToRoot()
	{
	  WriteDebugMsg("MoveToRoot");
	  state = new FSEntryState(null, null, -1, "#document", this);
	}

	public bool MoveToDocumentElement()
	{
	  FSEntryState fse = new FSEntryState(null, null, -1, "#document", this);
	  state = new FSEntryState(null, fse, 0, "mycomputer", this);
	  return true;
	}

	public override bool MoveTo( XPathNavigator other )
	{
	  WriteDebugMsg("MoveTo");
	  if (state.InNestedNavigator)
	  {
		FileSystemNavigator fsn = (FileSystemNavigator)other;
		return state.nestedNavigator.MoveTo(fsn.state.nestedNavigator);
	  }
	  else if (other is FileSystemNavigator)
	  {
		FileSystemNavigator fsn = (FileSystemNavigator)other;
		state = fsn.state.Clone();
		return true;
	  }
	  return false;
	}

	public override bool MoveToId( string id )
	{        
	  WriteDebugMsg("MoveToId");
	  if (state.InNestedNavigator)
		return state.nestedNavigator.MoveToId(id);
	  return false;
	}

	public override bool IsSamePosition( XPathNavigator other )
	{
	  WriteDebugMsg("IsSamePosition");
	  if (other is FileSystemNavigator)
	  {
		FileSystemNavigator fsn = (FileSystemNavigator)other;
		if ((state.currentEntry == fsn.state.currentEntry) &&
			(state.indexOfCurrentInParent == fsn.state.indexOfCurrentInParent) &&
			(state.indexOfAttribute == fsn.state.indexOfAttribute) &&
			(state.nonEntryName.Equals(fsn.state.nonEntryName)))
		{
		  if (state.InNestedNavigator)
			return state.nestedNavigator.IsSamePosition(fsn.state.nestedNavigator);
		  else
			return true;
		}
	  }
	  return false;
	}

	public bool RegisterFileHandler(string extension, string type, 
									string assembly) 
	{
	  try 
	  {
		// throws exception if fails to register
		FileHandler fh = new FileHandler(extension, type, assembly); 
		fileHandlers.Add(extension, fh);
	  }
	  catch
	  {
		return false;
	  }
      return true;
	}

	public XPathNavigator LookupFileHandler(FileSystemInfo file)
	{
	  Object obj = fileHandlers[file.Extension];
	  if (obj!=null && obj is FileHandler)
		return ((FileHandler)obj).CreateNavigator(file.FullName);	
	  return null;
	}
  }	

  internal class FSEntryState
  {
	// file/directory node state
	public FileSystemInfo currentEntry;
	public FileSystemInfo[] childEntries;
	public FSEntryState parent;
	public int indexOfCurrentInParent;
	public int indexOfAttribute;
	public FileSystemNavigator owner;
		
	// for other types of nodes
	// #document, #text, mycomputer
	public string nonEntryName; 

	// for navigating into files
	public XPathNavigator nestedNavigator;

	// logical drives on current machine
	public static string[] driveLetters;
	// attribute names for files/directories
	public static string[] attributeNames = 
	{
	  "type", 
	  "creationTime", 
	  "lastAccessTime", 
	  "lastWriteTime", 
	  "fullName", 
	  "length"
	};

	internal FSEntryState()
	{
	  indexOfCurrentInParent = -1;
	  indexOfAttribute = -1;
	  nonEntryName = "";
	  // cache logical drive letters
	  if (driveLetters == null)
		driveLetters = Directory.GetLogicalDrives();
	}

	internal FSEntryState(FileSystemInfo current, FSEntryState p, 
	   int index, string neName, FileSystemNavigator fsn)
	{
	  currentEntry = current;
	  parent = p;	
	  nonEntryName = neName;
	  indexOfCurrentInParent = index;
	  indexOfAttribute = -1;
	  owner = fsn;

	  // cache logical drive letters
	  if (driveLetters == null)
		driveLetters = Directory.GetLogicalDrives();

	  if (current != null && current is DirectoryInfo)
	  {
		try 
		{
		  // cache child file system entries
		  childEntries = ((DirectoryInfo)current).GetFileSystemInfos();
		}
		catch
		{
		  childEntries = null;
		  // ignore exception (logical drive has no children)
		  // this can happen when logical network drives are
		  // currently disconnected
		}
	  }
	  else
		childEntries = null;				
	}

	public FSEntryState Clone()
	{
	  FSEntryState fse = new FSEntryState();
	  fse.currentEntry = this.currentEntry;
	  fse.childEntries = this.childEntries;
	  fse.nonEntryName = this.nonEntryName;
	  fse.parent = this.parent;
	  fse.indexOfCurrentInParent = this.indexOfCurrentInParent;
	  fse.indexOfAttribute = this.indexOfAttribute;
	  fse.owner = this.owner;
	  // clone any nested navigators as well
	  if (this.nestedNavigator != null)
		fse.nestedNavigator = (XPathNavigator)this.nestedNavigator.Clone();
	  return fse;
	}

	public FSEntryState OpenChildEntry(int childIndex)
	{
	  FSEntryState fse;

	  if (IsDocument)
	  {
		if (childIndex > 0)
		  return null;
		// open the root 'mycomputer' element
		fse = new FSEntryState(null, this, 0, "mycomputer", owner);				
	  }
	  else if (IsAttribute)
	  {
		if (childIndex > 0)
		  return null;
		// attributes will only have a single #text node in this case
		fse = new FSEntryState(null, this, 0, "#text", owner);
	  }
	  else if (IsDocumentElement)
	  {
		// open the specified logical drive
		if (childIndex >=0 && childIndex < driveLetters.Length)
		{
		  // skip over a: drive
		  if (driveLetters[childIndex].ToLower().Equals("a:\\"))
		  {
			childIndex++;
			if (!(childIndex < driveLetters.Length))
			  return null;
		  }
		  DirectoryInfo dir = new DirectoryInfo(driveLetters[childIndex]);
		  fse = new FSEntryState(dir, this, childIndex, "", owner);
		}
		else
		  return null;
	  }
	  else if (IsFile)
	  {
		nestedNavigator = owner.LookupFileHandler(currentEntry);
		if (nestedNavigator != null)
		{
		  nestedNavigator.MoveToRoot(); 
		  if (nestedNavigator.MoveToFirstChild())
		  {
			if (nestedNavigator.NodeType == XPathNodeType.Element)
			  return this;
			else
			{
			  while (nestedNavigator.MoveToNext())
			  {
				if (nestedNavigator.NodeType == XPathNodeType.Element)
				  return this;
			  }
			}
		  }
		}
		return null;
	  }
	  else if (childIndex >= 0 && childIndex < ChildCount)
	  {
		// otherwise open the child file system entry
		fse = new FSEntryState(childEntries[childIndex], this, childIndex, "", owner);				
	  }
	  else 
	  {
		return null;
	  }
	  return fse;
	}			

	public string GetAttribute( string localName, string namespaceURI )
	{
	  if (namespaceURI.Equals(""))
	  {
		string attValue = "";
		if (IsFileOrDirectory)
		{
		  /* file/directory attributes include:
			 - Type {dir|file}
			 - CreationTime
			 - LastAccessTime
			 - LastWriteTime
			 - FullName
			 - Length (file only)
		  */
		  try 
		  {
			switch(localName)
			{
			case "type":
			  if (IsFile)
				attValue = "file";
			  else if (IsDirectory)
				attValue = "dir";							
			  break;
			case "creationTime":
			  attValue = currentEntry.CreationTime.ToString("G");
			  break;
			case "lastAccessTime":
			  attValue = currentEntry.LastAccessTime.ToString("G");
			  break;
			case "lastWriteTime":
			  attValue = currentEntry.LastWriteTime.ToString("G");
			  break;
			case "fullName":
			  attValue = currentEntry.FullName;
			  break;
			case "length":
			  if (IsFile)
				attValue = ((FileInfo)currentEntry).Length.ToString();
			  break;
			default:
			  break;
			}
		  }
		  catch
		  {
			// ignore exception
			return "";
		  }
		}
		return attValue;
	  }
	  return "";
	}

	public string Name 
	{ 
	  get 
	  { 
		if (InNestedNavigator)
		  return nestedNavigator.Name;
		if (IsAttribute)
		  return attributeNames[indexOfAttribute];
		if (IsFileOrDirectory)
		{
		  // convert all drive letters, directory/file names to lower case for simplicity
			if (parent != null && parent.Name.Equals("mycomputer"))
				return currentEntry.Name.Substring(0, 1).ToLower(); // 'c' instead of 'c:\'
			else
			{	
				// convert all file system names to valid XML encoded names
				string elementname = currentEntry.Name;
				elementname = elementname.Replace(" ", "_");
				return XmlConvert.EncodeLocalName(elementname.ToLower());
			}
		}
		else
		  return nonEntryName;
	  }
	}
	
	public int ChildCount 
	{ 
	  get 
	  {
		if (IsAttribute)
		  return 1; // file system attributes always have 1 child text node
		else if (IsTextNode)
		  return 0;
		else if (IsDocument)
		  return 1;
		else if (IsDocumentElement)
		  return driveLetters.Length;
		else if (childEntries == null)
		  return 0;
		else
		  return childEntries.Length;
	  }
	}

	public bool IsDocument
	{
	  get
	  {
		return Name.Equals("#document");
	  }
	}

	public bool IsDocumentElement
	{
	  get
	  {
		return Name.Equals("mycomputer");
	  }
	}

	public bool IsTextNode
	{
	  get
	  {
		return Name.Equals("#text");
	  }
	}

	public bool InNestedNavigator
	{
	  get
	  {
		return (IsFile && (nestedNavigator !=null));
	  }
	}

	public bool IsAttribute
	{
	  get
	  {
		return (indexOfAttribute != -1);
	  }
	}

	public bool IsFileOrDirectory
	{
	  get
	  {
		return IsFile || IsDirectory;
	  }
	}

	public bool IsFile
	{
	  get
	  {
		if (currentEntry != null)
		  return currentEntry is FileInfo;
		else
		  return false;
	  }
	}

	public bool IsDirectory
	{
	  get
	  {
		if (currentEntry != null)
		  return currentEntry is DirectoryInfo;
		else
		  return false;
	  }
	}	

	public int AttributeCount 
	{ 
	  get 
	  {
		if (IsFile) 
		  return FSEntryState.attributeNames.Length;
		if (IsDirectory) 
		  return FSEntryState.attributeNames.Length - 1;
		return 0;
	  }
	}

	public int IndexInParent 
	{ 
	  get 
	  {
		return indexOfCurrentInParent;
	  }
	}

  }

}


