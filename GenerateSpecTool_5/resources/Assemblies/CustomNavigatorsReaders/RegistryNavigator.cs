// originally written by Chris Lovett from Microsoft
// ported to beta2 by Aaron Skonnard <http://staff.develop.com/aarons>

namespace Developmentor.Xml
{
  using System;
  using System.Text;
  using System.IO;
  using System.Xml;
  using System.Xml.XPath;
  using System.Collections;
  using Microsoft.Win32;

  //**********************************************************************
  // This class creates a read-only XML data model over the Registry.
  // The data model looks like this:
  //  Document: #document (fabricated node)
  //      DocumentElement: "registry" (fabricated node)
  //          Element: HKEY_CLASSES_ROOT
  //          Element: HKEY_CURRENT_USER
  //          Element: HKEY_LOCAL_MACHINE
  //          Element: HKEY_USERS
  //          Element: HKEY_CURRENT_CONFIG
  //          Element: HKEY_DYN_DATA
  //          Element: HKEY_PERFORMANCE_DATA
  // Then from there each subkey is a child Element, if the element has named
  // values, then those are returned as attributes.  If the element has
  // an un-named (default) value, then this is returned in the first child text node.
  //
  // All element and attribute names are encoded using XmlConvert.Encode
  // to make sure they are valid XML names.  For example, the clsid's like
  // "{7FC0B86E-5FA7-11d1-BC7C-00C04FD929DB}" would be returned as the
  // element named "_x007b_7FC0B86E-5FA7-11d1-BC7C-00C04FD929DB_x007d_"
  // 
  // REG_BINARY values are returned as a base64 encoded string.
  //
  // For example the key "/registry/HKEY_CLASSES_ROOT/xmlfile" returns the following:
  //<result>
  //    <xmlfile EditFlags="AAABAA==">XML Document
  //        <BrowseInPlace/>
  //        <CLSID>{48123bc4-99d9-11d1-a6b3-00c04fd91555}</CLSID>
  //        <DefaultIcon>C:\WINNT\system32\msxml.dll,0</DefaultIcon>
  //        <shell>Open
  //            <Open>&amp;Open
  //                <command>"C:\Program Files\Internet Explorer\iexplore.exe" -nohome</command>
  //                <ddeexec>"file:%1",,-1,,,,,
  //                    <application>IExplore</application>
  //                    <topic>WWW_OpenURL</topic>
  //                </ddeexec>
  //            </Open>
  //        </shell>
  //  </xmlfile>
  //</result>
  //
  // The cool thing is that you can then do XPath queries to find stuff and
  // Xsl Transformations to generate HTML user interfaces to all this.
  // For example:
  //      Select("/*/HKEY_CLASSES_ROOT/CLSID/*[InProcServer32/@ThreadingModel='Both']")
  // Returns a list of all free-threaded components registered in the system.
  //
  // 
  //**********************************************************************

  public class RegistryNavigator : XPathNavigator
  {
	RegNavState state;

	public RegistryNavigator()
	{
	  this.state = new RegNavState();
	}
	    
	internal RegistryNavigator(RegNavState state)
	{
	  // used by Clone.
	  this.state = state;
	}      
	    
	public override XPathNavigator Clone()
	{
	  return new RegistryNavigator(state);
	}
	    
	// Node properties -- adaptor to the XPathNodeType
	public override XPathNodeType NodeType
	{ 
	  get { 
		// BUGBUG - moving to text value nodes...
		if (state.IsAttribute) {
		  return XPathNodeType.Attribute;
		} else if (state.IsDocument) {
		  return XPathNodeType.Root;
		} else if (state.IsTextNode) {
		  return XPathNodeType.Text;
		} else {
		  return XPathNodeType.Element;
		}
	  }
	}
	   
	public override string LocalName 
	{ 
	  get { return state.Name; }
	}

	public override string NamespaceURI 
	{ 
	  get { return string.Empty; } 
	}

	public override string Name 
	{ 
	  get { return LocalName; } 
	}

	public override string Prefix
	{ 
	  get { return string.Empty; }
	}

	public override string Value 
	{ 
	  get { return state.Value; } 
	}

	public override bool IsEmptyElement 
	{ 
	  get { return (!state.HasChildren); }
	}

	public override XmlNameTable NameTable
	{
	  get { return state.NameTable; }
	}

	private int AttributeCount 
	{ 
	  get { return state.AttributeCount; }
	}

	public override bool HasAttributes
	{
	  get { return (AttributeCount > 0); }
	}

	public override bool HasChildren 
	{ 
	  get { return (state.HasChildren); }
	}
	    
	private int ChildCount 
	{ 
	  get { return state.ChildCount; }
	}

	public override string BaseURI
	{
	  get { return String.Empty; }
	}

	public override string XmlLang
	{
	  get { return String.Empty; }
	}

	public override string GetAttribute( string localName, string namespaceURI )
	{
	  if (MoveToAttribute(localName,""))
	  {
		string value = Value;
		MoveToParent();
		return value;
	  }
	  return String.Empty;
	}

	public override bool MoveToAttribute( string localName, string namespaceURI )
	{
	  return state.MoveToAttribute(localName);
	}

	private bool MoveToAttribute( int i )
	{
	  bool result = state.MoveToAttribute(i);
	  return result;
	}
	    
	public override bool MoveToFirstAttribute()
	{
	  return state.MoveToAttribute(0);
	}

	public override bool MoveToNextAttribute()
	{
	  return state.MoveToNextAttribute();
	}

	public bool MoveToPreviousAttribute()
	{
	  return state.MoveToNextAttribute();
	}
	   	   	    	    
	public override string GetNamespace(string prefix)
	{
	  return String.Empty;
	}

	public override bool MoveToNamespace(string prefix)
	{
	  return false;
	}

	public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
	{
		return false;
	}

	public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
	{
		return false;
	}

	public override bool MoveToNext()
	{
	  RegNavState p = state.Parent;
	  if (p != null) {
		p = p.OpenChild(state.IndexInParent+1);
		if (p != null) {
		  state.Close();
		  state = p;
		  return true;
		}
	  }
	  return false;
	}

	public override bool MoveToPrevious()
	{
	  RegNavState p = state.Parent;
	  if (p != null) {
		p = p.OpenChild(state.IndexInParent-1);
		if (p != null) {
		  state.Close();
		  state = p;
		  return true;
		}
	  }
	  return false;
	}
	   
	public override bool MoveToFirst()
	{
	  RegNavState p = state.Parent;
	  if (p != null) {
		p = p.OpenChild(0);
		if (p != null) {
		  state.Close();
		  state = p;
		  return true;
		}
	  }
	  return false;
	}

	public override bool MoveToFirstChild()
	{
	  return MoveToChild(0);
	}

	private bool MoveToChild( int i )
	{
	  RegNavState p = state.OpenChild(i);
	  if (p != null) {
		state.Close();
		state = p;
		return true;
	  }
	  return false;
	}

	public override bool MoveToParent()
	{
	  if (state.IsAttribute)
	  {
		state.MoveToElement();
		return true;
	  }
	  RegNavState p = state.Parent;
	  if (p != null) {
		state.Close();
		state = p;
		return true;
	  }
	  return false;
	}

	public override void MoveToRoot()
	{
	  state.Close();
	  state = new RegNavState();
	}

	public override bool MoveTo( XPathNavigator other )
	{
	  if (other is RegistryNavigator) {
		RegistryNavigator rn = (RegistryNavigator)other;
		state.Close();
		state = rn.state.Clone();
		return true;
	  }
	  return false;
	}

	public override bool MoveToId( string id )
	{        
	  return false; // no concept of id's in registry.
	}

	public override bool IsSamePosition( XPathNavigator other )
	{
	  if (other is RegistryNavigator) {
		RegistryNavigator rn = (RegistryNavigator)other;
		if (state.IsSame(rn.state)) {
		  return true;
		}
	  }
	  return false;
	}
  }

  internal class RegNavState
  {
	RegistryKey _currentKey;
	int userCount; // number of RegNavState objects sharing this RegistryKey object
	int currentAttr; 
	string[] valueNames;
	string[] keyNames;
	public RegNavState prev;
	public int index;
	string name; 
	string xmlname; // the atomized xml-encoded name
	int count; // child count.
	NameTable nt; // all names returned are atomized in this name table.

	object textnodename;
	object docnodename;
	object regnodename;

	static string[] rootNames = {
	  Registry.ClassesRoot.Name,
	  Registry.CurrentConfig.Name,
	  Registry.CurrentUser.Name,
	  Registry.DynData.Name,
	  Registry.LocalMachine.Name,
	  Registry.PerformanceData.Name,
	  Registry.Users.Name
	};

	static RegNavState[] rootStates = null;
	    
	RegNavState GetRootState(int i, RegNavState root)
	{
	  if (rootStates == null) {
		rootStates = new RegNavState[7];
		rootStates[0] = new RegNavState(Registry.ClassesRoot, rootNames[0], root, 0, -1);
		rootStates[1] = new RegNavState(Registry.CurrentUser, rootNames[2], root, 1, -1);
		rootStates[2] = new RegNavState(Registry.LocalMachine, rootNames[4], root, 2, -1);
		rootStates[3] = new RegNavState(Registry.Users, rootNames[6], root, 3, -1);
		rootStates[4] = new RegNavState(Registry.CurrentConfig, rootNames[1], root, 4, -1);
		rootStates[5] = new RegNavState(Registry.DynData, rootNames[3], root, 5, -1);
		rootStates[6] = new RegNavState(Registry.PerformanceData, rootNames[5], root, 6, -1);
	  }
	  return rootStates[i];
	}

	public RegNavState()
	{
	  this._currentKey = null; // root
	  this.valueNames = rootNames;
	  this.currentAttr = -1;
	  this.index = -1;
	  this.count = -1;
	  this.nt = new NameTable();
	  this.name = "#document";
	  this.xmlname = XmlName(name);
	  this.textnodename = nt.Add("#text");
	  this.docnodename = xmlname;
	  this.regnodename = nt.Add("registry");
	  AddRef();
	}

	internal RegNavState(RegistryKey key, string name, RegNavState parent, int i, int attr)
	{
	  this._currentKey = key;        
	  this.currentAttr = attr;
	  this.index = i;
	  this.count = -1;
	  this.nt = parent.nt;
	  this.textnodename = parent.textnodename;
	  this.docnodename = parent.docnodename;
	  this.regnodename = parent.regnodename;
	  this.name = name;
	  if (name.Equals(textnodename) || name.Equals(docnodename) || name.Equals(regnodename)) 
		this.xmlname = name;
	  else 
		this.xmlname = XmlName(name);
	  this.prev = parent;

	  AddRef();
	}

	public bool IsSame(RegNavState other) 
	{
	  return (_currentKey == other._currentKey &&
			  index == other.index &&
			  currentAttr == other.currentAttr &&
			  name.Equals(other.name));
	}

	public RegNavState Parent {
	  get { 
		prev.AddRef();
		return prev; 
	  }
	}

	public RegNavState Clone()
	{
	  RegNavState s = new RegNavState();
	  s._currentKey = this._currentKey;
	  s.valueNames = this.valueNames;
	  s.currentAttr = this.currentAttr;
	  s.index = this.index;
	  s.name = this.name;
	  s.prev = this.prev;
	  s.count = this.count;
	  s.xmlname = this.xmlname;
	  s.textnodename = this.textnodename;
	  s.docnodename = this.docnodename;
	  s.regnodename = this.regnodename;
	  s.AddRef();
	  return s;
	}

	public int IndexInParent {
	  get {
		return index;
	  }
	}

	public RegNavState FindRoot(string name)
	{
	  for (int i = 0; i < rootNames.Length; i++)
	  {
		if (rootNames[i] == name)
		{
		  return GetRootState(i, this);
		}
	  }
	  return null;
	}

	string[] GetKeyNames()
	{
	  // lazily construct child names
	  if (keyNames == null) {
		keyNames = CurrentKey.GetSubKeyNames();
	  }
	  return keyNames;
	}

	string[] GetValueNames()
	{
	  // lazily construct value names
	  if (valueNames == null && CurrentKey.ValueCount > 1) {
		string[] names = _currentKey.GetValueNames();
		int count = 0;
		int i;
		for (i = 0; i < names.Length; i++) {
		  if (names[i] != "")
			count++;
		}
		i = 0;
		int j = 0;
		valueNames = new string[count];
		for (i = 0; i < names.Length; i++) {
		  if (names[i] != "") {
			//Console.WriteLine("Found attribute " + names[i]);
			valueNames[j++] = names[i];
		  }
		}
	  }
	  return valueNames;
	}


	RegistryKey CurrentKey {
	  get {
		// lazily construct current key based on parent key.
		if (_currentKey == null && prev != null) {
		  // time to open it !
		  _currentKey = prev._currentKey.OpenSubKey(name);
		}
		return _currentKey;
	  }
	}

	public RegNavState OpenChild(int i)
	{
	  if (i < 0 || i >= ChildCount) {
		return null;
	  }
	  RegNavState s;

	  if (IsDocument) {
		s = new RegNavState(CurrentKey, (string)regnodename, this, 0, -1);
	  } else if (i == 0 && (HasTextNode || currentAttr>=0)) {
		// If there is a text node it is always the first child node.
		s = new RegNavState(CurrentKey, (string)textnodename, this, i, -1);
	  } else if (IsDocumentElement) {
		s = GetRootState(i, this);
	  } else {
		string[] names = GetKeyNames();
		int j = i;
		if (HasTextNode) j--;
		//Console.WriteLine("Opening:"+names[j]);
		s = new RegNavState(null, names[j], this, i, currentAttr);
	  }
	  return s;
	}

	public bool IsDocument {
	  get { return (xmlname.Equals(docnodename)); }
	}

	public bool IsDocumentElement {
	  get { return (xmlname.Equals(regnodename)); }
	}

	public bool IsTextNode {
	  get { return (xmlname.Equals(textnodename)); }
	}

	public bool HasTextNode {
	  get {
		if (IsDocument || IsDocumentElement || IsTextNode)
		  return false;

		object val = CurrentKey.GetValue("");
		if (val != null && val.ToString() != "") return true;
		return false;
	  }
	}

	public bool HasChildren {
	  get { 
		return (ChildCount > 0);
	  }
	}

	public int ChildCount {
	  get {
		if (IsDocument) { 
		  return 1;
		} else if (IsDocumentElement) {
		  return rootNames.Length;
		} else if (IsTextNode) {
		  return 0;
		} else if (currentAttr>=0) {
		  return 1; // all attributes have one text node child.
		} else { 
		  if (count == -1) {
			count = CurrentKey.SubKeyCount;
			if (HasTextNode) count++;
		  }
		  return count;
		}
	  }
	}

	public int AttributeCount {
	  get {
		if (IsDocument || IsDocumentElement) {
		  return 0;
		} else if (IsTextNode || IsAttribute) {
		  return 0;
		} else {
		  string [] values = GetValueNames();
		  if (values == null) return 0;
		  return values.Length;
		}
	  }
	}

	public bool IsAttribute {
	  get { return currentAttr >= 0; }
	}

	public bool MoveToAttribute(int i)
	{
	  if (IsDocument || IsDocumentElement || IsTextNode)
		return false;

	  string [] values = GetValueNames();
	  if (i >= 0 && values != null && i < values.Length) {
		currentAttr = i;
		return true;
	  }
	  return false;
	}

	public void MoveToElement()
	{
	  currentAttr = -1;
	}

	public bool MoveToNextAttribute()
	{
	  string [] values = GetValueNames();
	  if (values == null) return false;
	  if (currentAttr + 1 < values.Length) {
		currentAttr++;
		return true;
	  }
	  return false;
	}

	public bool MoveToPreviousAttribute()
	{
	  if (currentAttr - 1 >= 0) {
		currentAttr--;
		return true;
	  }
	  return false;
	}

	public string XmlName(string str) {        
	  return nt.Add(XmlConvert.EncodeLocalName(str));
	}

	public string Name {
	  get { 
		if (IsAttribute) {
		  if (valueNames == null) {
			Console.WriteLine("Internal Error!!!");
		  }
		  return XmlName(valueNames[currentAttr]);
		} else {
		  return xmlname;
		}
	  }
	}

	public string Value {
	  get { 
		if (IsAttribute) {
		  return AttributeValue;
		} else if (IsTextNode) {
		  object val = CurrentKey.GetValue("");
		  string result = "";
		  if (val != null) result = val.ToString();
		  return result;
		} else {
		  Console.WriteLine("Null value");
		  return null;
		}
	  }
	}

	public string AttributeValue {
	  get {
		string [] values = GetValueNames();
		string attrname = values[currentAttr];
		//Console.WriteLine("AttrValue(" + Int32.Format(currentAttr,"d") + ") for " + name + " is " + attrname);
		object value = CurrentKey.GetValue(attrname);
		if (value == null) return "";

		if (value.GetType() == typeof(Byte[])) {
		  return Convert.ToBase64String((byte[])value);
		}
		return value.ToString();
	  }
	}

	public bool MoveToAttribute( string name )
	{
	  int i = FindAttrOffset(name);
	  if (i >= 0) {
		currentAttr = i;
		return true;
	  } else {
		return false;
	  }
	}

	internal int FindAttrOffset( string name )
	{
	  string [] values = GetValueNames();
	  for (int i = 0; i < values.Length; i++)
	  {
		if (values[i] == name)
		  return i;
	  }
	  return -1;
	}

	public NameTable NameTable {
	  get { return nt; }
	}

	public void Close()
	{
	  if (_currentKey!=null && !xmlname.Equals(regnodename)) {
		userCount--;
		if (userCount==0) {
		  if (prev != null && _currentKey != prev._currentKey) {
			_currentKey.Close();
		  }
		  prev.userCount--; // reduce use count on parent key
		  _currentKey = null;
		}
	  }
	}

	public void AddRef()
	{
	  userCount++;
	  if (prev != null) {
		prev.userCount++;
	  }
	}

	~RegNavState ( ) 
	{
	  Close();
	}
  }
}



