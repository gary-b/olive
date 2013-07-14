using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Transactions;
using System.Windows.Markup;
using System.Xaml;
using System.Xml.Linq;
using System.Activities.Debugger;
using System.Activities.Expressions;
using System.Activities.Hosting;
using System.Activities.Statements;
using System.Activities.Tracking;
using System.Activities.Validation;

namespace System.Activities
{
	[DataContract]
	public class Bookmark : IEquatable<Bookmark>
	{
		internal Bookmark ()
		{
			Name = String.Empty;
		}
		public Bookmark (string name)
		{
			if (String.IsNullOrEmpty (name))
				throw new ArgumentException ("The argument name is null or empty.", "name");
			Name = name;
		}
		
		public string Name { get; private set; }

		public bool Equals (Bookmark other)
		{
			return other != null && (other == this || (Name != String.Empty && other.Name == Name));
		}
		
		public override bool Equals (object obj)
		{
			return Equals (obj as Bookmark);
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}
		
		public override string ToString ()
		{
			return Name;
		}
	}
}
