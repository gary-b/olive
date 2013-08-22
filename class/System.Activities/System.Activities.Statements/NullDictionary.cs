using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace System.Activities.Statements {
	//NOTE: Adding / updating / removing null keyed item does not invalidate Enumerator
	public class NullDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
		object nullValue;
		bool HasNullValue = false;
		void AddNullValue (TValue value)
		{
			if (HasNullValue)
				throw new ArgumentException ("duplicate TKey");
			HasNullValue = true;
			nullValue = value;
		}
		TValue GetNullValue ()
		{
			if (!HasNullValue)
				throw new KeyNotFoundException ();
			return (TValue) nullValue;
		}
		void SetNullValue (TValue value)
		{
			if (!HasNullValue)
				throw new InvalidOperationException ("no value to set");
			nullValue = value;
		}
		bool RemoveNullValue ()
		{
			bool wasRemoved = HasNullValue;
			HasNullValue = false;
			nullValue = null;
			return wasRemoved;
		}
		Dictionary<TKey, TValue> StandardEntries { get; set; }
		List<KeyValuePair<TKey, TValue>> AllEntries { 
			get {
				var list = new List<KeyValuePair<TKey, TValue>> (StandardEntries);
				if (HasNullValue)
					list.Add (new KeyValuePair<TKey, TValue> (default (TKey), GetNullValue ()));
				return list;
			}
		}
		public NullDictionary ()
		{
			StandardEntries = new Dictionary<TKey, TValue> ();
		}
		#region IDictionary implementation
		public void Add (TKey key, TValue value)
		{
			if (key == null) 
				AddNullValue (value);
			else
				StandardEntries.Add (key, value);
		}
		public bool ContainsKey (TKey key)
		{
			return (key == null) ? HasNullValue : StandardEntries.ContainsKey (key);
		}
		public bool Remove (TKey key)
		{
			bool wasRemoved;
			if (key == null)
				wasRemoved = RemoveNullValue ();
			else
				wasRemoved = StandardEntries.Remove (key);

			return wasRemoved;
		}
		public bool TryGetValue (TKey key, out TValue value)
		{
			if (key == null) {
				if (HasNullValue)
					value = GetNullValue ();
				else
					value = default (TValue);
				return HasNullValue;
			}
			return StandardEntries.TryGetValue (key, out value);
		}
		public TValue this [TKey index] {
			get {
				if (index == null)
					return GetNullValue ();
				else
					return StandardEntries [index];
			}
			set {
				if (index == null) {
					if (HasNullValue)
						SetNullValue (value);
					else
						AddNullValue (value);
				} else {
					StandardEntries [index] = value;
				}
			}
		}
		public ICollection<TKey> Keys {
			get {
				return new KeyCollection (this);
			}
		}
		public ICollection<TValue> Values {
			get {
				return new ValueCollection (this);
			}
		}
		#endregion
		#region ICollection implementation
		public void Add (KeyValuePair<TKey, TValue> item)
		{
			Add (item.Key, item.Value);
		}
		public void Clear ()
		{
			RemoveNullValue ();
			StandardEntries.Clear ();
		}
		public bool Contains (KeyValuePair<TKey, TValue> item)
		{
			return AllEntries.Contains (item);
		}
		public void CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			AllEntries.CopyTo (array, arrayIndex);
		}
		public bool Remove (KeyValuePair<TKey, TValue> item)
		{
			bool wasRemoved;
			if (item.Key == null) {
				if (HasNullValue && EqualityComparer<TValue>.Default.Equals (item.Value, GetNullValue ()))
					wasRemoved = RemoveNullValue ();
				else
					wasRemoved = false;
			} else {
				wasRemoved = ((IDictionary<TKey, TValue>) StandardEntries).Remove (item);
			}

			return wasRemoved;
		}
		public int Count {
			get { return AllEntries.Count; }
		}
		public bool IsReadOnly { get { return false; } }
		#endregion
		#region IEnumerable implementation
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
		{
			return new Enumerator (this);
		}
		#endregion
		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return new Enumerator (this);
		}
		#endregion
		public struct Enumerator : IEnumerator<KeyValuePair<TKey,TValue>>,
			IDisposable, IEnumerator
		{
			NullDictionary<TKey, TValue> dictionary;
			IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
			KeyValuePair<TKey, TValue> current;
			bool nullReturned;
			bool currentIsInvalid;

			internal Enumerator (NullDictionary<TKey, TValue> dictionary)
			: this ()
			{
				this.dictionary = dictionary;
				enumerator = ((IDictionary<TKey,TValue>) dictionary.StandardEntries).GetEnumerator ();
			}

			public bool MoveNext ()
			{
				VerifyState ();
				bool result = enumerator.MoveNext ();
				if (result) {
					current = enumerator.Current;
					return true;
				} else if (dictionary.HasNullValue && !nullReturned) {
					current = new KeyValuePair<TKey, TValue> (default(TKey), dictionary.GetNullValue ());
					nullReturned = true;
					return true;
				} else {
					currentIsInvalid = true;
					return false;
				}
			}

			public KeyValuePair<TKey, TValue> Current {
				get { 
					if (currentIsInvalid)
						throw new Exception ();
					return current; 
				}
			}

			object IEnumerator.Current {
				get { 
					return Current;
				}
			}

			void IEnumerator.Reset ()
			{
				VerifyState ();
				((IEnumerator) enumerator).Reset();
				current = new KeyValuePair<TKey, TValue> (default (TKey), default (TValue));
				nullReturned = false;
				currentIsInvalid = false;
			}

			void VerifyState ()
			{
				if (dictionary == null)
					throw new ObjectDisposedException (null);
			}

			public void Dispose ()
			{
				dictionary = null;
				current = new KeyValuePair<TKey, TValue> (default (TKey), default (TValue));
				enumerator.Dispose ();
			}
		}
		public sealed class KeyCollection : ICollection<TKey> {
			NullDictionary<TKey, TValue> dictionary;

			public KeyCollection (NullDictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
					throw new ArgumentNullException ("dictionary");
				this.dictionary = dictionary;
			}

			public void CopyTo (TKey [] array, int index)
			{
				dictionary.AllEntries.Select (e=>e.Key).ToList ().CopyTo (array, index);
			}

			public Enumerator GetEnumerator ()
			{
				return new Enumerator (dictionary);
			}

			void ICollection<TKey>.Add (TKey item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}

			void ICollection<TKey>.Clear ()
			{
				throw new NotSupportedException ("this is a read-only collection");
			}

			bool ICollection<TKey>.Contains (TKey item)
			{
				return dictionary.ContainsKey (item);
			}

			bool ICollection<TKey>.Remove (TKey item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}

			public int Count {
				get { return dictionary.Count; }
			}

			bool ICollection<TKey>.IsReadOnly {
				get { return true; }
			}

			public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator {
				IEnumerator<KeyValuePair<TKey, TValue>> host_enumerator;

				internal Enumerator (NullDictionary<TKey, TValue> host)
				{
					host_enumerator = host.GetEnumerator ();
				}

				public void Dispose ()
				{
					host_enumerator.Dispose ();
				}

				public bool MoveNext ()
				{
					return host_enumerator.MoveNext ();
				}

				public TKey Current {
					get { return host_enumerator.Current.Key; }
				}

				object IEnumerator.Current {
					get { return host_enumerator.Current.Key; }
				}

				void IEnumerator.Reset ()
				{
					host_enumerator.Reset ();
				}
			}
		}
		public sealed class ValueCollection : ICollection<TValue> {
			NullDictionary<TKey, TValue> dictionary;

			public ValueCollection (NullDictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
					throw new ArgumentNullException ("dictionary");
				this.dictionary = dictionary;
			}

			public void CopyTo (TValue [] array, int index)
			{
				dictionary.AllEntries.Select (e => e.Value).ToList ().CopyTo (array, index);
			}

			public Enumerator GetEnumerator ()
			{
				return new Enumerator (dictionary);
			}

			void ICollection<TValue>.Add (TValue item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}

			void ICollection<TValue>.Clear ()
			{
				throw new NotSupportedException ("this is a read-only collection");
			}

			bool ICollection<TValue>.Contains (TValue item)
			{
				return dictionary.AllEntries.Any (kvp => EqualityComparer<TValue>.Default.Equals (kvp.Value, item));
			}

			bool ICollection<TValue>.Remove (TValue item)
			{
				throw new NotSupportedException ("this is a read-only collection");
			}

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return this.GetEnumerator ();
			}

			public int Count {
				get { return dictionary.Count; }
			}

			bool ICollection<TValue>.IsReadOnly {
				get { return true; }
			}

			public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator {
				IEnumerator<KeyValuePair<TKey, TValue>> host_enumerator;

				internal Enumerator (NullDictionary<TKey, TValue> host)
				{
					host_enumerator = host.GetEnumerator ();
				}

				public void Dispose ()
				{
					host_enumerator.Dispose ();
				}

				public bool MoveNext ()
				{
					return host_enumerator.MoveNext ();
				}

				public TValue Current {
					get { return host_enumerator.Current.Value; }
				}

				object IEnumerator.Current {
					get { return host_enumerator.Current.Value; }
				}

				void IEnumerator.Reset ()
				{
					host_enumerator.Reset ();
				}
			}
		}
	}
}