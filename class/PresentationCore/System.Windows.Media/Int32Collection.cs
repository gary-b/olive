
/* this file is generated by gen-collection-types.cs.  do not modify */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media.Animation;

namespace System.Windows.Media {


	public class Int32Collection : Freezable, ICollection<Int32>, IList<Int32>, ICollection, IList, IFormattable
	{
		List<Int32> list;

		public struct Enumerator : IEnumerator<Int32>, IEnumerator
		{
			public void Reset()
			{
				throw new NotImplementedException (); 
			}

			public bool MoveNext()
			{
				throw new NotImplementedException (); 
			}

			public Int32 Current {
				get { throw new NotImplementedException (); }
			}

			object IEnumerator.Current {
				get { return Current; }
			}

			void IDisposable.Dispose()
			{
			}
		}

		public Int32Collection ()
		{
			list = new List<Int32>();
		}

		public Int32Collection (IEnumerable<Int32> values)
		{
			list = new List<Int32> (values);
		}

		public Int32Collection (int length)
		{
			list = new List<Int32> (length);
		}

		public new Int32Collection Clone ()
		{
			throw new NotImplementedException ();
		}

		public new Int32Collection CloneCurrentValue ()
		{
			throw new NotImplementedException ();
		}

		public bool Contains (int value)
		{
			return list.Contains (value);
		}

		public bool Remove (int value)
		{
			return list.Remove (value);
		}

		public int IndexOf (int value)
		{
			return list.IndexOf (value);
		}

		public void Add (int value)
		{
			list.Add (value);
		}

		public void Clear ()
		{
			list.Clear ();
		}

		public void CopyTo (int[] array, int arrayIndex)
		{
			list.CopyTo (array, arrayIndex);
		}

		public void Insert (int index, int value)
		{
			list.Insert (index, value);
		}

		public void RemoveAt (int index)
		{
			list.RemoveAt (index);
		}

		public int Count {
			get { return list.Count; }
		}

		public int this[int index] {
			get { return list[index]; }
			set { list[index] = value; }
		}

		public static Int32Collection Parse (string str)
		{
			throw new NotImplementedException ();
		}

		bool ICollection<Int32>.IsReadOnly {
			get { return false; }
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator();
		}

		IEnumerator<Int32> IEnumerable<Int32>.GetEnumerator()
		{
			return GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator();
		}

		bool ICollection.IsSynchronized {
			get { return false; }
		}

		object ICollection.SyncRoot {
			get { return this; }
		}

		void ICollection.CopyTo(Array array, int offset)
		{
			CopyTo ((int[]) array, offset);
		}

		bool IList.IsFixedSize {
			get { return false; }
		}

		bool IList.IsReadOnly {
			get { return false; }
		}

		object IList.this[int index] {
			get { return this[index]; }
			set { this[index] = (int)value; }
		}

		int IList.Add (object value)
		{
			Add ((int)value);
			return Count;
		}

		bool IList.Contains (object value)
		{
			return Contains ((int)value);
		}

		int IList.IndexOf (object value)
		{
			return IndexOf ((int)value);
		}

		void IList.Insert (int index, object value)
		{
			Insert (index, (int)value);
		}

		void IList.Remove (object value)
		{
			Remove ((int)value);
		}

		public override string ToString ()
		{
			throw new NotImplementedException ();
		}

		public string ToString (IFormatProvider provider)
		{
			throw new NotImplementedException ();
		}

		string IFormattable.ToString (string format, IFormatProvider provider)
		{
			throw new NotImplementedException ();
		}


		protected override Freezable CreateInstanceCore ()
		{
			return new Int32Collection();
		}

		protected override void GetAsFrozenCore (Freezable sourceFreezable)
		{
			throw new NotImplementedException ();
		}

		protected override void GetCurrentValueAsFrozenCore (Freezable sourceFreezable)
		{
			throw new NotImplementedException ();
		}

		protected override void CloneCurrentValueCore (Freezable sourceFreezable)
		{
			throw new NotImplementedException ();
		}

		protected override void CloneCore (Freezable sourceFreezable)
		{
			throw new NotImplementedException ();
		}
	}
}