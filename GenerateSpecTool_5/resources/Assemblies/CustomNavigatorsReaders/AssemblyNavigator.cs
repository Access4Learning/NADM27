// Aaron Skonnard <http://staff.develop.com/aaron>

namespace Developmentor.Xml
{
  using System;
  using System.IO;
  using System.Xml;	
  using System.Xml.XPath;
  using System.Collections;
  using System.Globalization;
  using System.Reflection;

  /// <summary>
  ///   
  /// AssemblyNavigator exposes a .NET assembly as an XML 
  /// document as follows:
  ///
  /// <code><![CDATA[
  /// <dm.xml ...>
  ///   <dm.xml.dll ...>
  ///     <FileSystemNavigator ...>
  ///       <!-- constructors and nested parameters -->
  ///       <ctor ...>
  ///         <file .../>
  ///       </ctor>
  ///       <!-- fields -->
  ///       <nestedNavigator isa='field' .../>
  ///       <!-- properties -->
  ///       <IsAttribute isa='property' .../>
  ///       <!-- methods -->
  ///       <MoveToFirstSelected isa='method' .../>
  ///       <MoveToNextSelected isa='method' .../>
  ///       <!-- events -->
  ///       <SelectionChanged isa='event' .../>
  ///       ...
  ///     </FileSystemNavigator>
  ///     
  ///     <FileHandler ...>
  ///       ...
  ///     </FileHandler>
  ///   </dm.xml.dll>
  /// </dm.xml>
  /// ]]></code>
  /// 
  ///    
  /// </summary>
  public class AssemblyNavigator : XPathNavigator
  {
	private AssemblyState state;
	private XmlNameTable nt = new NameTable();
	public Assembly assembly;
	public string assemblyFile;

	public AssemblyNavigator(string assemblyName)
	{
		this.assemblyFile = assemblyName;
		this.assembly = Assembly.LoadFrom(assemblyName);
		this.state = new AssemblyState(null, null, -1, "#document", this);
	}

	private AssemblyNavigator(AssemblyState s, string assemblyName)
	{
	  this.assemblyFile = assemblyName;
	  this.assembly = Assembly.LoadFrom(assemblyName);
	  this.state = s;
	}

	public override XPathNavigator Clone()
	{
	  AssemblyNavigator nav = 
		new AssemblyNavigator(this.state.Clone(), this.assemblyFile);
	  return nav;
	}
		
	public override XPathNodeType NodeType
	{ 
	  get 
	  {
		if (state.IsDocument)
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
		return nt.Add(state.Name);
	  }
	}

	public override string NamespaceURI 
	{ 
	  get 
	  { 
		return nt.Add(string.Empty); 
	  } 
	}

	public override string Name 
	{ 
	  get 
	  { 
		return nt.Add(state.Name);
	  }
	}

	public override string Prefix 
	{ 
	  get 
	  { 
		return nt.Add(string.Empty); 
	  }
	}

	public override string Value 
	{ 
	  get 
	  {
		if (state.IsAttribute || state.IsTextNode)
		  return GetAttributeValue();
		else
		  return ""; 
	  } 
	}

	public override string BaseURI
	{
	  get
	  {
		return String.Empty;
	  }
	}

	public override string XmlLang
	{
	  get
	  {
		return String.Empty;
	  }
	}

	public override bool IsEmptyElement 
	{ 
	  get
	  { 
		if (state.IsAttribute || state.IsTextNode)
		  return false;
		return !HasChildren;
	  }
	}

	public override XmlNameTable NameTable
	{
	  get 
	  {
		return nt; 
	  }
	}

	private int AttributeCount 
	{ 
	  get 
	  {
		if (state.AttributeNames == null)
		  return 0;
		else
		  return state.AttributeNames.Length;
	  }
	}

	public override bool HasAttributes
	{
	  get
	  {
		return AttributeCount > 0;
	  }
	}		

	public override bool HasChildren 
	{ 
	  get 
	  {
		return (ChildCount > 0);
	  }
	}
		
	private int ChildCount 
	{ 
	  get 
	  {
		return state.ChildCount;
	  }
	}
		
	private int IndexInParent 
	{ 
	  get 
	  {
		return state.indexOfCurrentInParent;
	  }
	}

	private string GetAttributeValue()
	{
	  // this is an attribute #text node
	  if (state.IsTextNode && state.parent.IsAttribute)  
		return state.parent.GetAttribute(state.parent.Name); 
	  else if (state.indexOfAttribute != -1)
		return state.GetAttribute(state.Name);
	  return "";
	}

	public override string GetAttribute( string localName, string namespaceURI )
	{
	  if (namespaceURI.Equals(""))
		return state.GetAttribute(localName);
	  return "";
	}

	public override bool MoveToAttribute( string localName, string namespaceURI )
	{
	  if (!namespaceURI.Equals(""))
		return false;
	  if (HasAttributes)
	  {
		for (int i=0; i<AttributeCount; i++)
		{
		  if (state.AttributeNames[i].Equals(localName))
		  {
			state.indexOfAttribute = i;
			return true;
		  }
		}
	  }
	  return false;
	}

	private bool MoveToAttribute( int i )
	{
	  if (HasAttributes)
	  {				
		if (i >= 0 && i < AttributeCount)
		{
		  state.indexOfAttribute = i;
		  return true;
		}
	  }
	  return false;
	}
		
	public override bool MoveToFirstAttribute()
	{
	  return MoveToAttribute(0);
	}

	public override bool MoveToNextAttribute()
	{
	  if (state.IsTextNode)
		MoveToParent();
	  if (HasAttributes)
	  {				
		if (state.indexOfAttribute < AttributeCount-1)
		{
		  state.indexOfAttribute++;
		  return true;
		}
	  }
	  return false;
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
   		
	private bool UpdateState(AssemblyState s)
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
	  if (state.IsAttribute)
		return false;
	  AssemblyState p = state.parent;
	  if (p!=null && (IndexInParent+1 < p.ChildCount))
	  {
		AssemblyState newState = p.OpenChild(IndexInParent+1);
		return UpdateState(newState);
	  }
	  return false;
	}
		
	public override bool MoveToPrevious()
	{
	  if (state.IsAttribute)
		return false;
	  AssemblyState p = state.parent;
	  if (p!=null && (IndexInParent-1 >= 0))
	  {
		AssemblyState newState = p.OpenChild(IndexInParent-1);
		return UpdateState(newState);
	  }
	  return false;
	}
   
	public override bool MoveToFirst()
	{
	  if (state.IsAttribute)
		return false;
	  AssemblyState p = state.parent;
	  if (p!=null)
	  {
		AssemblyState newState = p.OpenChild(0);
		return UpdateState(newState);
	  }
	  return false;
	}

	public override bool MoveToFirstChild()
	{
	  AssemblyState newState = state.OpenChild(0);
	  return UpdateState(newState);
	}

	private bool MoveToChild( int i )
	{
	  if (i < ChildCount && i >= 0)
	  {
		AssemblyState newState = state.OpenChild(i);
		return UpdateState(newState);
	  }
	  return false;
	}

	public override bool MoveToParent()
	{
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
	  state = new AssemblyState(null, null, -1, "#document", this);
	}

	public override bool MoveTo( XPathNavigator other )
	{
	  if (other is AssemblyNavigator)
	  {
		AssemblyNavigator asn = (AssemblyNavigator)other;
		state = asn.state.Clone();
		return true;
	  }
	  return false;
	}

	public override bool MoveToId( string id )
	{        
	  return false;
	}

	public override bool IsSamePosition( XPathNavigator other )
	{
	  if (other is AssemblyNavigator)
	  {
		AssemblyNavigator asn = (AssemblyNavigator)other;
		return ((state.currentObject == asn.state.currentObject) &&
				(state.indexOfCurrentInParent == asn.state.indexOfCurrentInParent) &&
				(state.indexOfAttribute == asn.state.indexOfAttribute) &&
				(state.nonEntryName.Equals(asn.state.nonEntryName)) );
	  }
	  return false;
	}
  }

  internal class AssemblyState
  {
	public Object currentObject;
	public Object[] childObjects;
	public AssemblyState parent;
	public int indexOfCurrentInParent;
	public int indexOfAttribute;
	public AssemblyNavigator owner;
	// for other types of nodes
	// #document, #text, assembly
	public string nonEntryName; 
	
	// attribute names
	public static string[] assemblyAtts = 
	{ 
	  "isa", 
	  "codebase", 
	  "location" 
	};
	public static string[] moduleAtts = 
	{ 
	  "isa", 
	  "qname", 
	  "scopename" 
	};  
	public static string[] typeAtts = 
	{ 
	  "isa", 
	  "qname", 
	  "typetype", 
	  "abstract", 
	  "visibility", 
	  "sealed", 
	  "serializable", 
	  "basetype", 
	  "underlying", 
	  "guid" 
	};
	public static string[] constructorAtts = 
	{ 
	  "isa", 
	  "abstract", 
	  "visibility", 
	  "final", 
	  "static", 
	  "virtual", 
	  "callconv" 
	};
	public static string[] parameterAtts = 
	{ 
	  "isa", 
	  "type", 
	  "default", 
	  "optional", 
	  "in", 
	  "out", 
	  "retval" 
	};
	public static string[] fieldAtts = 
	{ 
	  "isa", 
	  "type", 
	  "visibility", 
	  "literal", 
	  "initonly", 
	  "notserialized", 
	  "static"
	};
	public static string[] propertyAtts = 
	{ 
	  "isa", 
	  "type", 
	  "read", 
	  "write" 
	};
	public static string[] methodAtts = 
	{ 
	  "isa", 
	  "rettype", 
	  "abstract", 
	  "visibility", 
	  "final", 
	  "static", 
	  "virtual", 
	  "callconv"
	};
	public static string[] eventAtts = 
	{ 
	  "isa", 
	  "type", 
	  "multicast" 
	};

	internal AssemblyState()
	{
	  indexOfCurrentInParent = -1;
	  indexOfAttribute = -1;
	  nonEntryName = "";
	}

	internal AssemblyState(Object current, AssemblyState p, 
	   int index, string neName, AssemblyNavigator nav)
	{
	  currentObject = current;
	  parent = p;	
	  nonEntryName = neName;
	  indexOfCurrentInParent = index;
	  indexOfAttribute = -1;
	  owner = nav;

	  if (current != null)
	  {
		try 
		{
		  if (IsAssembly)
			childObjects = ((Assembly)currentObject).GetModules();
		  else if (IsModule)
			childObjects = ((Module)currentObject).GetTypes();
		  else if (IsConstructor)
			childObjects = ((ConstructorInfo)currentObject).GetParameters();
		  else if (IsMethod)
			childObjects = ((MethodInfo)currentObject).GetParameters();
		  else if (IsType)
		  {
			Type cType = (Type)currentObject;
			Object[] ctors = cType.GetConstructors();
			Object[] fields = cType.GetFields();
			Object[] props = cType.GetProperties();
			Object[] methods = cType.GetMethods();
			Object[] events = cType.GetEvents();

			int i = 0;
			childObjects = new Object[ctors.Length + fields.Length + props.Length + methods.Length + events.Length];
			foreach(Object o in ctors)
			  childObjects[i++]=o;
			foreach(Object o in fields)
			  childObjects[i++]=o;
			foreach(Object o in props)
			  childObjects[i++]=o;
			foreach(Object o in methods)
			  childObjects[i++]=o;
			foreach(Object o in events)
			  childObjects[i++]=o;
		  }
		  else
			childObjects = null;
		}
		catch (Exception e)
		{
		  System.Console.WriteLine(e.Message);
		  childObjects = null;
		}
	  }
	  else
		childObjects = null;				
	}

	public AssemblyState Clone()
	{
	  AssemblyState astate = new AssemblyState();
	  astate.currentObject = this.currentObject;
	  astate.childObjects = this.childObjects;
	  astate.nonEntryName = this.nonEntryName;
	  astate.parent = this.parent;
	  astate.indexOfCurrentInParent = this.indexOfCurrentInParent;
	  astate.indexOfAttribute = this.indexOfAttribute;
	  astate.owner = this.owner;
	  return astate;
	}

	public AssemblyState OpenChild(int childIndex)
	{
	  AssemblyState ast = null;

	  if (IsDocument)
	  {
		if (childIndex > 0)
		  return null;
		// open the root assembly element
		ast = new AssemblyState(owner.assembly, this, 0, "", owner);				
	  }
	  else if (IsAttribute)
	  {
		if (childIndex > 0)
		  return null;
		// attributes will only have a single #text node in this case
		ast = new AssemblyState(null, this, 0, "#text", owner);
	  }
	  else if (childIndex >= 0 && childIndex < ChildCount)
	  {
		ast = new AssemblyState(childObjects[childIndex], this, childIndex, "", owner);
	  }
	  else 
	  {
		return null;
	  }
	  return ast;
	}			

	public string Name 
	{ 
	  get 
	  { 
		if (IsAttribute)
		  return AttributeNames[indexOfAttribute];
		else if (IsAssembly)
		{
		  string name = ((Assembly)currentObject).FullName;
		  return XmlConvert.EncodeLocalName(name.Substring(0, name.IndexOf(",")).ToLower());
		}
		else if (IsModule)
		  return XmlConvert.EncodeLocalName(((Module)currentObject).Name.ToLower());
		else if (IsType)
		  return XmlConvert.EncodeLocalName(((Type)currentObject).Name);
		else if (IsConstructor)
		  return XmlConvert.EncodeLocalName(((ConstructorInfo)currentObject).Name.Substring(1));
		else if (IsMethod)
		  return ((MethodInfo)currentObject).Name;
		else if (IsParameter)
		  return ((ParameterInfo)currentObject).Name;
		else if (IsField)
		  return ((FieldInfo)currentObject).Name;
		else if (IsProperty)
		  return ((PropertyInfo)currentObject).Name;
		else if (IsEvent)
		  return ((EventInfo)currentObject).Name;
		else
		  return nonEntryName;
	  }
	}

	public int ChildCount 
	{ 
	  get 
	  {
		if (IsDocument)
		  return 1;
		else if (IsAttribute)
		  return 1; // file system attributes always have 1 child text node
		else if (IsTextNode)
		  return 0;
		else if (childObjects == null)
		  return 0;
		else
		  return childObjects.Length;
	  }
	}

	public string[] GetAttributeNames(AssemblyState s)
	{
	  if (s.IsAssembly)
		return assemblyAtts;
	  else if (s.IsModule)
		return moduleAtts;
	  else if (s.IsType)
		return typeAtts;
	  else if (s.IsConstructor)
		return constructorAtts;
	  else if (s.IsMethod)
		return methodAtts;
	  else if (s.IsParameter)
		return parameterAtts;
	  else if (s.IsField)
		return fieldAtts;
	  else if (s.IsProperty)
		return propertyAtts;
	  else if (s.IsEvent)
		return eventAtts;
	  else
		return null;
	}				

	public string[] AttributeNames
	{
	  get
	  {
		if (IsAssembly)
		  return assemblyAtts;
		else if (IsModule)
		  return moduleAtts;
		else if (IsType)
		  return typeAtts;
		else if (IsConstructor)
		  return constructorAtts;
		else if (IsMethod)
		  return methodAtts;
		else if (IsParameter)
		  return parameterAtts;
		else if (IsField)
		  return fieldAtts;
		else if (IsProperty)
		  return propertyAtts;
		else if (IsEvent)
		  return eventAtts;
		else
		  return null;
	  }				
	}

	public string GetAttribute(string name) 
	{
	  if (IsAssembly)
	  {
		Assembly assem = (Assembly)currentObject;
		switch(name)
		{
		case "isa":
		  return "assembly";
		case "codebase":
		  return assem.CodeBase;
		case "location":
		  return assem.Location;
		default:
		  break;
		}
	  }
	  else if (IsModule)
	  {
		Module mod = (Module)currentObject;
		switch(name)
		{
		case "isa":
		  return "module";
		case "qname":
		  return mod.FullyQualifiedName;
		case "scopename":
		  return mod.ScopeName;
		default:
		  break;
		}
	  }
	  else if (IsType)
	  {
		Type t = (Type)currentObject;
		switch(name)
		{
		case "isa":
		  return "type";
		case "qname":
		  return t.FullName;
		case "typetype":
		  if (t.IsArray)
			return "array";
		  else if (t.IsClass)
			return "class";
		  else if (t.IsCOMObject)
			return "comobject";
		  else if (t.IsEnum)
			return "enum";
		  else if (t.IsInterface)
			return "interface";
		  else if (t.IsPointer)
			return "pointer";
		  else if (t.IsPrimitive)
			return "primitive";
		  else if (t.IsValueType)
			return "value";
		  break;
		case "abstract":
		  return t.IsAbstract.ToString();
		case "visibility":
		  if (t.IsPublic)
			return "public";
		  else 
			return "notpublic";
		case "sealed":
		  return t.IsSealed.ToString();
		case "serializable":
		  return t.IsSerializable.ToString();
		case "basetype":
		  if (t.BaseType != null)
			return t.BaseType.Name;
		  else 
			return "";
		case "underlying":
		  if (t.UnderlyingSystemType != null)
			return t.UnderlyingSystemType.Name;
		  else
			return "";
		case "guid":
		  return t.GUID.ToString();
		default:
		  break;
		}
	  }
	  else if (IsConstructor)
	  {
		ConstructorInfo ctor = (ConstructorInfo)currentObject;
		switch(name)
		{
		case "isa":
		  return "constructor";
		case "abstract":
		  return ctor.IsAbstract.ToString();
		case "visibility":
		  if (ctor.IsPublic)
			return "public";
		  else if (ctor.IsPrivate)
			return "private";
		  else if (ctor.IsFamily)
			return "family";
		  else if (ctor.IsFamilyAndAssembly)
			return "family-and-assembly";
		  else if (ctor.IsFamilyOrAssembly)
			return "family-or-assembly";
		  break;
		case "final":
		  return ctor.IsFinal.ToString();
		case "static":
		  return ctor.IsStatic.ToString();
		case "virtual":
		  return ctor.IsVirtual.ToString();
		case "callconv":
		  return ctor.CallingConvention.ToString();
		default:
		  break;
		}
	  }
	  else if (IsMethod)
	  {
		MethodInfo me = (MethodInfo)currentObject;
		switch(name)
		{
		case "isa":
		  return "method";
		case "rettype":
		  if (me.ReturnType != null)
			return me.ReturnType.Name;
		  else
			return "";
		case "abstract":
		  return me.IsAbstract.ToString();
		case "visibility":
		  if (me.IsPublic)
			return "public";
		  else if (me.IsPrivate)
			return "private";
		  else if (me.IsFamily)
			return "family";
		  else if (me.IsFamilyAndAssembly)
			return "family-and-assembly";
		  else if (me.IsFamilyOrAssembly)
			return "family-or-assembly";
		  break;
		case "final":
		  return me.IsFinal.ToString();
		case "static":
		  return me.IsStatic.ToString();
		case "virtual":
		  return me.IsVirtual.ToString();
		case "callconv":
		  return me.CallingConvention.ToString();
		default:
		  break;
		}				
	  }
	  else if (IsParameter)
	  {
		ParameterInfo pinfo = (ParameterInfo)currentObject;
		switch (name)
		{
		case "isa":
		  return "parameter";
		case "type":
		  if (pinfo.ParameterType != null)
			return pinfo.ParameterType.Name;
		  else
			return "";
		case "default":
		  return pinfo.DefaultValue.ToString();
		case "optional":
		  return pinfo.IsOptional.ToString();
		case "in":
		  return pinfo.IsIn.ToString();
		case "out":
		  return pinfo.IsOut.ToString();
		case "retval":
		  return pinfo.IsRetval.ToString();
		default:
		  break;
		}
	  }
	  else if (IsField)
	  {
		FieldInfo finfo = (FieldInfo)currentObject;
		switch(name)
		{
		case "isa":
		  return "field";
		case "type":
		  if (finfo.FieldType != null)
			return finfo.FieldType.Name;
		  else 
			return "";
		case "visibility":
		  if (finfo.IsPublic)
			return "public";
		  else if (finfo.IsPrivate)
			return "private";
		  else if (finfo.IsFamily)
			return "family";
		  else if (finfo.IsFamilyAndAssembly)
			return "family-and-assembly";
		  else if (finfo.IsFamilyOrAssembly)
			return "family-or-assembly";
		  break;
		case "literal":
		  return finfo.IsLiteral.ToString();
		case "initonly":
		  return finfo.IsInitOnly.ToString();
		case "notserialized":
		  return finfo.IsNotSerialized.ToString();
		case "static":
		  return finfo.IsStatic.ToString();
		default:
		  break;
		}
	  }
	  else if (IsProperty)
	  {
		PropertyInfo pinfo = (PropertyInfo)currentObject;
		switch(name)
		{
		case "isa":
		  return "property";
		case "type":
		  if (pinfo.PropertyType != null)
			return pinfo.PropertyType.Name;
		  else
			return "";
		case "read":
		  return pinfo.CanRead.ToString();
		case "write":
		  return pinfo.CanWrite.ToString();
		default:
		  break;
		}
	  }
	  else if (IsEvent)
	  {
		EventInfo einfo = (EventInfo)currentObject;
		switch(name)
		{
		case "isa":
		  return "event";
		case "type":
		  if (einfo.EventHandlerType != null)
			return einfo.EventHandlerType.Name;
		  else 
			return "";
		case "multicast":
		  return einfo.IsMulticast.ToString();
		default:
		  break;
		}
	  }
	  return "";
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
		return IsAssembly;
	  }
	}

	public bool IsTextNode
	{
	  get
	  {
		return Name.Equals("#text");
	  }
	}

	public bool IsAttribute
	{
	  get
	  {
		return (indexOfAttribute != -1);
	  }
	}

	public bool IsAssembly
	{
	  get
	  {
		return currentObject is Assembly;
	  }
	}

	public bool IsModule
	{
	  get
	  {
		return currentObject is Module;
	  }
	}

	public bool IsType
	{
	  get
	  {
		return currentObject is Type;
	  }
	}

	public bool IsConstructor
	{
	  get
	  {
		return currentObject is ConstructorInfo;
	  }
	}

	public bool IsParameter
	{
	  get
	  {
		return currentObject is ParameterInfo;
	  }
	}

	public bool IsField
	{
	  get
	  {
		return currentObject is FieldInfo;
	  }
	}

	public bool IsProperty
	{
	  get
	  {
		return currentObject is PropertyInfo;
	  }
	}

	public bool IsMethod
	{
	  get
	  {
		return currentObject is MethodInfo;
	  }
	}

	public bool IsEvent
	{
	  get
	  {
		return currentObject is EventInfo;
	  }
	}
  }
}
