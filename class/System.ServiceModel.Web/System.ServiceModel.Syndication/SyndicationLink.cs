//
// SyndicationLink.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace System.ServiceModel.Syndication
{
	public class SyndicationLink
	{
		[MonoTODO]
		public static SyndicationLink CreateAlternateLink (Uri uri)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public static SyndicationLink CreateAlternateLink (Uri uri, string mediaType)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public static SyndicationLink CreateMediaEnclosureLink (Uri uri, string mediaType, long length)

		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public static SyndicationLink CreateSelfLink (Uri uri)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public static SyndicationLink CreateSelfLink (Uri uri, string mediaType)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public SyndicationLink ()
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public SyndicationLink (Uri uri)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public SyndicationLink (Uri uri, string relationshipType, string title, string mediaType, long length)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		protected SyndicationLink (SyndicationLink source)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public Dictionary<XmlQualifiedName, string> AttributeExtensions {
			get { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public SyndicationElementExtensionCollection ElementExtensions {
			get { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public Uri BaseUri {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public long Length {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public string MediaType {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public string RelationshipType {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public string Title {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public Uri Uri {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		[MonoTODO]
		public virtual SyndicationLink Clone ()
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		protected internal virtual bool TryParseAttribute (string name, string ns, string value, string version)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		protected internal virtual bool TryParseElement (XmlReader reader, string version)
		{
			throw new NotImplementedException ();
		}


		[MonoTODO]
		protected internal virtual void WriteAttributeExtensions (XmlWriter writer, string version)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		protected internal virtual void WriteElementExtensions (XmlWriter writer, string version)
		{
			throw new NotImplementedException ();
		}

		[MonoTODO]
		public Uri GetAbsoluteUri ()
		{
			throw new NotImplementedException ();
		}
	}
}
