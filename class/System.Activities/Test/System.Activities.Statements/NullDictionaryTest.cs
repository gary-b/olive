//
// 
// Code Adapted from MonoTests.System.Collections.Generic.DictionaryTest
//
// Authors:
//	Sureshkumar T (tsureshkumar@novell.com)
//	Ankit Jain (radical@corewars.org)
//	David Waite (mass@akuma.org)
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// Copyright (C) 2005 David Waite (mass@akuma.org)
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
using NUnit.Framework;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;

namespace MonoTests.System.Activities.Statements {
	[TestFixture]
	public class NullDictionaryTest {
		class MyClass {
			int a;
			int b;
			public MyClass (int a, int b)
			{
				this.a = a;
				this.b = b;
			}
			public override int GetHashCode ()
			{
				return a + b;
			}

			public override bool Equals (object obj)
			{
				if (!(obj is MyClass))
					return false;
				return ((MyClass)obj).Value == a;
			}


			public int Value {
				get { return a; }
			}

		}

		NullDictionary<string, object> _dictionary = null;
		NullDictionary<MyClass, MyClass> _dictionary2 = null;
		NullDictionary<int, int> _dictionary3 = null;

		[SetUp]
		public void SetUp ()
		{
			_dictionary = new NullDictionary<string, object> ();
			_dictionary2 = new NullDictionary<MyClass, MyClass> ();
			_dictionary3 = new NullDictionary<int, int>();
		}

		[Test]
		public void AddKeyValue ()
		{
			_dictionary.Add ("key1", "value");
			Assert.AreEqual ("value", _dictionary ["key1"].ToString (), "Add failed!");
		}
		[Test]
		public void AddKeyValue2 ()
		{
			MyClass m1 = new MyClass (10,5);
			MyClass m2 = new MyClass (20,5);
			MyClass m3 = new MyClass (12,3);
			MyClass m4 = new MyClass (1,1);
			_dictionary2.Add (m1,m1);
			_dictionary2.Add (m2, m2);
			_dictionary2.Add (m3, m3);
			_dictionary2.Add (null, m4);
			Assert.AreEqual (20, _dictionary2 [m2].Value, "#1");
			Assert.AreEqual (10, _dictionary2 [m1].Value, "#2");
			Assert.AreEqual (12, _dictionary2 [m3].Value, "#3");
			Assert.AreEqual (1, _dictionary2 [null].Value, "#4");
		}

		[Test]
		public void AddKeyValueNull ()
		{
			_dictionary.Add (null, "value2");
			Assert.AreEqual ("value2", _dictionary [null], "Add failed!");
		}

		[Test, ExpectedException(typeof (ArgumentException))]
		public void AddKeyValueDuplicate ()
		{
			_dictionary.Add("foo", "bar");
			_dictionary.Add("foo", "bar");
		}

		[Test, ExpectedException(typeof (ArgumentException))]
		public void AddKeyValueDuplicateNull ()
		{
			_dictionary.Add(null, "bar");
			_dictionary.Add(null, "bar");
		}

		[Test]
		public void AddKvp ()
		{
			_dictionary.Add (new KeyValuePair<string, object> ("foo", "bar"));
			Assert.AreEqual ("bar", _dictionary ["foo"]);
		}

		[Test]
		public void IndexerGet ()
		{
			_dictionary.Add ("key1", "value");
			Assert.AreEqual ("value", _dictionary ["key1"].ToString (), "Add failed!");
		}

		[Test]
		public void IndexerGetNull ()
		{
			_dictionary.Add (null, "value");
			Assert.AreEqual ("value", _dictionary [null].ToString (), "Add failed!");
		}

		[Test, ExpectedException(typeof(KeyNotFoundException))]
		public void IndexerGetNonExisting ()
		{
			object foo = _dictionary ["foo"];
		}

		[Test, ExpectedException(typeof(KeyNotFoundException))]
		public void IndexerGetNonExistingNull ()
		{
			object foo = _dictionary [null];
		}

		[Test]
		public void IndexerSet ()
		{
			_dictionary.Add ("key1", "value1");
			_dictionary ["key1"] =  "value2";
			Assert.AreEqual (1, _dictionary.Count);
			Assert.AreEqual ("value2", _dictionary ["key1"]);
		}

		[Test]
		public void IndexerSetNull ()
		{
			_dictionary.Add (null, "value1");
			_dictionary [null] =  "value2";
			Assert.AreEqual (1, _dictionary.Count);
			Assert.AreEqual ("value2", _dictionary [null]);
		}

		[Test]
		public void IndexerAdd ()
		{
			_dictionary ["key1"] =  "value1";
			Assert.AreEqual (1, _dictionary.Count);
			Assert.AreEqual ("value1", _dictionary ["key1"]);
		}

		[Test]
		public void IndexerAddNull ()
		{
			_dictionary [null] =  "value1";
			Assert.AreEqual (1, _dictionary.Count);
			Assert.AreEqual ("value1", _dictionary [null]);
		}

		[Test]
		public void IsReadOnly ()
		{
			Assert.IsFalse (_dictionary.IsReadOnly);
		}

		[Test]
		public void RemoveKey ()
		{
			Assert.IsFalse (_dictionary.Remove (null));
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			_dictionary.Add ("key3", "value3");
			_dictionary.Add ("key4", "value4");
			_dictionary.Add (null, "value5");
			Assert.IsTrue (_dictionary.Remove ("key3"));
			Assert.IsFalse (_dictionary.Remove ("foo"));
			Assert.IsFalse (_dictionary.ContainsKey ("key3"));
			Assert.IsTrue (_dictionary.Remove (null));
			Assert.IsFalse (_dictionary.ContainsKey (null));
			Assert.AreEqual (3,_dictionary.Count);
		}

		[Test]
		public void Clear ()
		{
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			_dictionary.Add ("key3", "value3");
			_dictionary.Add ("key4", "value4");
			_dictionary.Add (null, "value5");
			_dictionary.Clear ();
			Assert.AreEqual (0, _dictionary.Count, "Clear method failed!");
			Assert.IsFalse (_dictionary.ContainsKey ("key2"));
			Assert.IsFalse (_dictionary.ContainsKey (null));
		}

		[Test]
		public void Count ()
		{
			_dictionary.Add ("key1", "value1");
			Assert.AreEqual (1, _dictionary.Count, "Count method failed!");
			_dictionary.Add (null, "value5");
			Assert.AreEqual (2, _dictionary.Count, "Count method failed!");
			_dictionary.Remove (null);
			Assert.AreEqual (1, _dictionary.Count, "Count method failed!");
			_dictionary.Remove ("key1");
			Assert.AreEqual (0, _dictionary.Count, "Count method failed!");
		}

		[Test]
		public void ContainsKey ()
		{
			Assert.IsFalse (_dictionary.ContainsKey (null));
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			_dictionary.Add ("key3", "value3");
			_dictionary.Add ("key4", "value4");
			_dictionary.Add (null, "value5");
			bool contains = _dictionary.ContainsKey ("key4");
			Assert.IsTrue (contains, "ContainsKey does not return correct value!");
			contains = _dictionary.ContainsKey ("key5");
			Assert.IsFalse (contains, "ContainsKey for non existant does not return correct value!");
			contains = _dictionary.ContainsKey (null);
			Assert.IsTrue (contains, "ContainsKey does not return correct value!");
		}

		[Test]
		public void ContainsKvp ()
		{
			Assert.IsFalse (_dictionary.Contains (new KeyValuePair<string, object> (null, null)));
			_dictionary.Add ("key1", "value1");
			_dictionary.Add (null, "value2");
			_dictionary.Add ("key3", "value3");
			_dictionary.Add ("key4", "value4");
			bool contains = _dictionary.Contains (new KeyValuePair<string, object> (null, "value2"));
			Assert.IsTrue (contains, "Contains does not return correct value!");
			contains = _dictionary.Contains (new KeyValuePair<string, object> ("key4", "value4"));
			Assert.IsTrue (contains, "Contains does not return correct value!");
			contains = _dictionary.Contains (new KeyValuePair<string, object> (null, null));
			Assert.IsFalse (contains, "Contains for non existant does not return correct value!");
		}

		[Test]
		public void TryGetValue ()
		{
			object value = "";
			bool retrieved = _dictionary.TryGetValue (null, out value);
			Assert.IsFalse (retrieved);
			Assert.IsNull (value);
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			_dictionary.Add (null, "value5");
			_dictionary.Add ("key3", "value3");
			_dictionary.Add ("key4", "value4");

			retrieved = _dictionary.TryGetValue ("key4", out value);
			Assert.IsTrue (retrieved);
			Assert.AreEqual ("value4", value, "TryGetValue does not return value!");

			retrieved = _dictionary.TryGetValue ("key7", out value);
			Assert.IsFalse (retrieved);
			Assert.IsNull (value, "value for non existant value should be null!");

			retrieved = _dictionary.TryGetValue (null, out value);
			Assert.IsTrue (retrieved);
			Assert.AreEqual ("value5", value, "TryGetValue does not return value!");
			int i;
			retrieved = _dictionary3.TryGetValue (2, out i);
			Assert.IsFalse (retrieved);
			Assert.AreEqual (0, i);
		}

		private class MyTest
		{
			public string Name;
			public int RollNo;

			public MyTest (string name, int number)
			{
				Name = name;
				RollNo = number;
			}

			public override int GetHashCode ()
			{
				return Name.GetHashCode () ^ RollNo;
			}

			public override bool Equals (object obj)
			{
				MyTest myt = obj as MyTest;
				return myt.Name.Equals (this.Name) &&
					myt.RollNo.Equals (this.RollNo);
			}
		}

		[Test]
		public void IEnumerator ()
		{
			_dictionary.Add ("1", "1");
			_dictionary.Add (null, "n");
			_dictionary.Add ("3", "3");

			IEnumerator itr1 = ((IEnumerable)_dictionary).GetEnumerator ();
			IEnumerator <KeyValuePair <string, object>> itr2 = ((IEnumerable <KeyValuePair <string, object>>)_dictionary).GetEnumerator ();
			Assert.AreEqual (itr1, itr2);
		}

		[Test]
		public void IEnumeratorGeneric ()
		{
			_dictionary.Add ("1", "1");
			_dictionary.Add (null, "n");
			_dictionary.Add ("3", "3");
			IEnumerator <KeyValuePair <string, object>> itr = ((IEnumerable <KeyValuePair <string, object>>)_dictionary).GetEnumerator ();
			var list = new List<KeyValuePair<string, object>> ();
			while (itr.MoveNext ())	{
				object o = itr.Current;
				Assert.AreEqual (typeof (KeyValuePair <string, object>), o.GetType (), "Current should return a type of KeyValuePair<object,string>");
				KeyValuePair <string, object> entry = (KeyValuePair <string, object>)itr.Current;
				list.Add (entry);
			}

			Assert.Contains (new KeyValuePair<string, object> ("1", "1"), list);
			Assert.Contains (new KeyValuePair<string, object> (null, "n"), list);
			Assert.Contains (new KeyValuePair<string, object> ("3", "3"), list);
		}

		[Test]
		public void ForEachTest_LOOK ()
		{
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			_dictionary.Add ("key3", "value3");
			_dictionary.Add ("key4", "value4");

			int i = 0;
			foreach (KeyValuePair <string, object> entry in _dictionary)
				i++;
			Assert.AreEqual(4, i, "fail1: foreach entry failed!");

			i = 0;
			foreach (KeyValuePair <string, object> entry in ((IEnumerable)_dictionary))
				i++;
			Assert.AreEqual(4, i, "fail2: foreach entry failed!");
		}

		[Test]
		public void PlainEnumeratorReturn ()
		{
			// Test that we return a KeyValuePair even for non-generic dictionary iteration
			_dictionary["foo"] = "bar";
			IEnumerator<KeyValuePair<string, object>> enumerator = _dictionary.GetEnumerator();
			Assert.IsTrue(enumerator.MoveNext(), "#1");
			Assert.AreEqual (typeof (KeyValuePair<string,object>), ((IEnumerator)enumerator).Current.GetType (), "#2");
			Assert.AreEqual (typeof (KeyValuePair<string,object>), ((object) enumerator.Current).GetType (), "#5");
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Enumerator_FailFast_IndexAdd ()
		{
			NullDictionary<int, int> d = new NullDictionary<int, int> ();
			d [1] = 1;
			int count = 0;
			foreach (KeyValuePair<int, int> kv in d) {
				d [kv.Key + 1] = kv.Value + 1;
				if (count++ != 0)
					Assert.Fail ("Should not be reached");
			}
			Assert.Fail ("Should not be reached");
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Enumerator_FailFast_AddKeyValue ()
		{
			NullDictionary<int, int> d = new NullDictionary<int, int> ();
			d [1] = 1;
			int count = 0;
			foreach (KeyValuePair<int, int> kv in d) {
				d.Add (kv.Key + 1, kv.Key + 1);
				if (count++ != 0)
					Assert.Fail ("Should not be reached");
			}
			Assert.Fail ("Should not be reached");
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Enumerator_FailFast_AddKvp ()
		{
			NullDictionary<string, int> d = new NullDictionary<string, int> ();
			d ["1"] = 1;
			int count = 0;
			foreach (KeyValuePair<string, int> kv in d) {
				d.Add (new KeyValuePair<string, int> ("2", kv.Value));
				if (count++ != 0)
					Assert.Fail ("Should not be reached");
			}
			Assert.Fail ("Should not be reached");
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Enumerator_FailFast_IndexSet ()
		{
			NullDictionary<int, int> d = new NullDictionary<int, int> ();
			d [1] = 1;
			int count = 0;
			foreach (KeyValuePair<int, int> kv in d) {
				d [kv.Key] = kv.Key;
				if (count++ != 0)
					Assert.Fail ("Should not be reached");
			}
			Assert.Fail ("Should not be reached");
		}
		[Test]
		public void Enumerator_NullKeyedItemDoesntInvalidate ()
		{
			NullDictionary<string, int> d = new NullDictionary<string, int> ();
			d ["1"] = 1;
			d ["2"] = 2;
			int loop = 0;
			//Add (Key, Value)
			foreach (KeyValuePair<string, int> kv in d) {
				if (loop++ == 0)
					d.Add (null, 10);
			}
			Assert.AreEqual (3, d.Count);
			//Remove (Key)
			foreach (KeyValuePair<string, int> kv in d) {
				d.Remove (null);
			}
			Assert.AreEqual (2, d.Count);
			// add using index and set using index
			foreach (KeyValuePair<string, int> kv in d) {
				d [null] = 10;
			}
			Assert.AreEqual (3, d.Count);
			d.Remove (null);
			loop = 0;
			//Add (Kvp)
			foreach (KeyValuePair<string, int> kv in d) {
				if (loop++ == 0)
					d.Add (new KeyValuePair<string, int> (null, 10));
			}
			Assert.AreEqual (3, d.Count);
			//Remove (Kvp)
			foreach (KeyValuePair<string, int> kv in d) {
				d.Remove (new KeyValuePair<string, int> (null, 10));
			}
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Enumerator_FailFast_Clear ()
		{
			NullDictionary<int, int> d = new NullDictionary<int, int> ();
			d [1] = 1;
			int count = 0;
			foreach (KeyValuePair<int, int> kv in d) {
				d.Clear ();
				if (count++ != 0)
					Assert.Fail ("Should not be reached");
			}
			Assert.Fail ("Should not be reached");
		}
		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Enumerator_FailFast_RemoveKey ()
		{
			NullDictionary<int, int> d = new NullDictionary<int, int> ();
			d [1] = 1;
			d [2] = 2;
			d [3] = 3;
			int count = 0;
			foreach (KeyValuePair<int, int> kv in d) {
				d.Remove (3);
				if (count++ != 0)
					Assert.Fail ("Should not be reached");
			}
			Assert.Fail ("Should not be reached");
		}

		[Test, ExpectedException (typeof (InvalidOperationException))]
		public void Enumerator_FailFast_RemoveKvp ()
		{
			NullDictionary<int, int> d = new NullDictionary<int, int> ();
			d [1] = 1;
			d [2] = 2;
			d [3] = 3;
			int count = 0;
			foreach (KeyValuePair<int, int> kv in d) {
				d.Remove (new KeyValuePair<int, int> (3,3));
				if (count++ != 0)
					Assert.Fail ("Should not be reached");
			}
			Assert.Fail ("Should not be reached");
		}

		[Test]
		public void RemoveKvp ()
		{
			var dictionary = new NullDictionary<string, int> ();
			dictionary.Add ("foo", 42);
			dictionary.Add ("bar", 12);
			dictionary.Add (null, 6);

			var collection = dictionary as ICollection<KeyValuePair<string, int>>;

			Assert.IsFalse (collection.Remove (new KeyValuePair<string, int> ("bar", 42)));
			Assert.IsFalse (collection.Remove (new KeyValuePair<string, int> ("foo", 12)));
			Assert.IsTrue (collection.Remove (new KeyValuePair<string, int> ("foo", 42)));
			Assert.IsTrue (collection.Remove (new KeyValuePair<string, int> (null, 6)));

			Assert.AreEqual (12, dictionary ["bar"]);
		}

		[Test]
		public void CopyTo ()
		{
			var dictionary = new NullDictionary<string, int> ();
			dictionary.Add (null, 21);
			dictionary.Add ("foo", 42);

			Assert.AreEqual (2, dictionary.Count);

			var pairs = new KeyValuePair<string, int> [2];

			dictionary.CopyTo (pairs, 0);

			Assert.AreEqual ("foo", pairs [0].Key);
			Assert.AreEqual (42, pairs [0].Value);
			Assert.IsNull (pairs [1].Key);
			Assert.AreEqual (21, pairs [1].Value);
		}

		[Test]
		public void CopyTo_Empty ()
		{
			var dictionary = new NullDictionary<string, int> ();
			var pairs = new KeyValuePair<string, int> [2];
			dictionary.CopyTo (pairs, 0);
			Assert.IsNull (pairs [0].Key);
			Assert.AreEqual (0, pairs [0].Value);
			Assert.IsNull (pairs [1].Key);
			Assert.AreEqual (0, pairs [1].Value);
		}
		[Test]
		public void Keys_Enumerator ()
		{
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			_dictionary.Add (null, "value3");
			_dictionary.Add ("key4", "value4");

			var keys = new List<string> (_dictionary.Keys);
			Assert.AreEqual (4, keys.Count);

			foreach (string key in keys) {
				Assert.IsTrue (_dictionary.ContainsKey (key));
				Assert.AreEqual (1, keys.Count (i => i == key));
			}

			_dictionary.Clear ();
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			keys = new List<string> (_dictionary.Keys);
			Assert.AreEqual (2, keys.Count);

			foreach (string key in keys) {
				Assert.IsTrue (_dictionary.ContainsKey (key));
				Assert.AreEqual (1, keys.Count (i => i == key));
			}
		}
		[Test]
		public void Keys_Count_IsReadOnly_Contains ()
		{
			Assert.IsTrue (_dictionary.Keys.IsReadOnly);
			Assert.AreEqual (0, _dictionary.Keys.Count);
			Assert.IsFalse (_dictionary.Keys.Contains ("key"));
			_dictionary.Add ("key", "value");
			Assert.AreEqual (1, _dictionary.Keys.Count);
			Assert.IsTrue (_dictionary.Keys.Contains ("key"));
			_dictionary.Add (null, "value");
			Assert.AreEqual (2, _dictionary.Keys.Count);
			Assert.IsTrue (_dictionary.Keys.Contains (null));
		}
		[Test]
		public void Keys_CopyTo ()
		{
			var arr = new string [2];
			_dictionary.Keys.CopyTo (arr, 0);
			Assert.IsNull (arr [0]);
			Assert.IsNull (arr [1]);

			_dictionary.Add (null, null);
			_dictionary.Add ("foo", "bar");

			_dictionary.Keys.CopyTo (arr, 0);
			Assert.AreEqual ("foo", arr [0]);
			Assert.IsNull (arr [1]);
		}
		[Test]
		public void Values_Enumerator ()
		{
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			_dictionary.Add (null, "value3");
			_dictionary.Add ("key4", "value4");

			var values = new List<object> (_dictionary.Values);
			Assert.AreEqual (4, values.Count);

			foreach (object val in values) {
				Assert.IsTrue (_dictionary.Any (kvp => kvp.Value == val));
				Assert.AreEqual (1, values.Count (i => i == val));
			}

			_dictionary.Clear ();
			_dictionary.Add ("key1", "value1");
			_dictionary.Add ("key2", "value2");
			values = new List<object> (_dictionary.Values);
			Assert.AreEqual (2, values.Count);

			foreach (object val in values) {
				Assert.IsTrue (_dictionary.Any (kvp => kvp.Value == val));
				Assert.AreEqual (1, values.Count (i => i == val));
			}
		}
		[Test]
		public void Values_Count_IsReadOnly_Contains ()
		{
			Assert.IsTrue (_dictionary.Values.IsReadOnly);
			Assert.AreEqual (0, _dictionary.Values.Count);
			Assert.IsFalse (_dictionary.Values.Contains ("value"));
			_dictionary.Add ("key", "value");
			Assert.AreEqual (1, _dictionary.Values.Count);
			Assert.IsTrue (_dictionary.Values.Contains ("value"));
			_dictionary.Add (null, null);
			Assert.AreEqual (2, _dictionary.Values.Count);
			Assert.IsTrue (_dictionary.Values.Contains (null));
		}
		[Test]
		public void Values_CopyTo ()
		{
			var arr = new string [2];
			_dictionary.Values.CopyTo (arr, 0);
			Assert.IsNull (arr [0]);
			Assert.IsNull (arr [1]);

			_dictionary.Add (null, null);
			_dictionary.Add ("foo", "bar");
			_dictionary.Values.CopyTo (arr, 0);
			Assert.AreEqual ("bar", arr [0]);
			Assert.IsNull (arr [1]);
		}
	}
}

