//
// AssemblyInfo.cs
//
// Authors:
//	Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//	Sebastien Pouliot  (sebastien@xamarin.com)
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// Copyright (C) 2012 Xamarin Inc. (http://www.xamarin.com)
//

using System;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about the system assembly

#if NET_2_0
[assembly: AssemblyVersion ("2.0.0.0")]
#else
[assembly: AssemblyVersion ("1.1.0.0")]
#endif

[assembly: AssemblyCompany ("Novell, Xamarin and contributors")]
[assembly: AssemblyCopyright ("(C) 2006 Novell, (C) 2012 Xamarin Inc.")]
[assembly: AssemblyDescription ("Crimson MHash Wrapper Assembly")]
[assembly: AssemblyTitle ("Crimson.MHash.dll")]
[assembly: CLSCompliant (true)]
[assembly: ComVisible (false)]
[assembly: NeutralResourcesLanguage ("en-US")]

[assembly:SecurityPermission (SecurityAction.RequestMinimum, UnmanagedCode=true)]

//[assembly: AssemblyDelaySign (false)]
[assembly:AssemblyKeyFile("crimson.snk")] 
