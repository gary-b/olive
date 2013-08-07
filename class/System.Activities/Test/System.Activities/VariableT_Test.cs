using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;
using System.ComponentModel;

namespace MonoTests.System.Activities {
	[TestFixture]
	public class VariableT_Test {
		#region From Variable
		#region Properties
		[Test]
		public void New_Default ()
		{
			var vStr = new Variable<string> ();
			var strLit = new Literal<string> ("Hello\nWorld");
			vStr.Default = strLit;
			Assert.AreSame (strLit, vStr.Default);
			Assert.AreSame (vStr.Default, ((Variable)vStr).Default);
			vStr.Default = null;
			Assert.IsNull (vStr.Default);
			Assert.AreSame (vStr.Default, ((Variable)vStr).Default);
		}
		[Test]
		public void Variable_Default ()
		{
			var vStr = new Variable<string> ();
			var strLit = new Literal<string> ("Hello\nWorld");
			((Variable)vStr).Default = strLit;
			Assert.AreSame (strLit, vStr.Default);
			Assert.AreSame (vStr.Default, ((Variable)vStr).Default);
			((Variable)vStr).Default = null;
			Assert.IsNull (vStr.Default);
			Assert.AreSame (vStr.Default, ((Variable)vStr).Default);
		}
		[Test]
		public void Modifiers ()
		{
			// checking validation on property passes for all valid values
			var vStr = new Variable<string> (); 
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			vStr.Modifiers = VariableModifiers.ReadOnly;
			Assert.AreEqual (VariableModifiers.ReadOnly, vStr.Modifiers);
			vStr.Modifiers = VariableModifiers.Mapped;
			Assert.AreEqual (VariableModifiers.Mapped, vStr.Modifiers);
			vStr.Modifiers = VariableModifiers.None;
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			vStr.Modifiers = VariableModifiers.Mapped | VariableModifiers.ReadOnly;
			Assert.AreEqual (VariableModifiers.Mapped | VariableModifiers.ReadOnly, vStr.Modifiers);

			Assert.AreEqual (VariableModifiers.ReadOnly, (vStr.Modifiers & VariableModifiers.ReadOnly));
			Assert.AreEqual (VariableModifiers.Mapped, (vStr.Modifiers & VariableModifiers.Mapped));
		}
		[Test, ExpectedException (typeof (InvalidEnumArgumentException))]
		public void ModifiersInvalidValueEx ()
		{
			//System.ComponentModel.InvalidEnumArgumentException : The value of argument 'value' (7) is invalid for Enum type 
			//'VariableModifiers'. Parameter name: value
			VariableModifiers m = (VariableModifiers) 7; // no error here
			var vStr = new Variable<string> ("", "Hello\nWorld");
			vStr.Modifiers = m; // exception raised from here
		}
		[Test]
		public void New_Name ()
		{
			var vStr = new Variable<string> ();
			vStr.Name = "test";
			Assert.AreEqual ("test", vStr.Name);
			Assert.AreEqual (((LocationReference)vStr).Name, vStr.Name);
			vStr.Name = null;
			Assert.IsNull (vStr.Name);
			Assert.AreEqual (((LocationReference)vStr).Name, vStr.Name);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void NameCore () 
		{
			throw new NotImplementedException ();
		}
		#endregion

		#region Static Methods
		[Test]
		[Ignore ("Create")]
		public void Create ()
		{
			var vStr = Variable.Create ("aname", typeof (string), VariableModifiers.None);
			Assert.IsInstanceOfType (typeof (Variable<string>), vStr);
			Assert.AreEqual (typeof (string), vStr.Type);
			Assert.AreEqual ("aname", vStr.Name);
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			Assert.IsNull (vStr.Default);

			var vInt = Variable.Create ("aname", typeof (int), VariableModifiers.None);
			Assert.IsInstanceOfType (typeof (Variable<int>), vInt);
			// .NET doesnt raise error on null name
			var v = Variable.Create (null, typeof (int), VariableModifiers.None);
		}
		[Test, ExpectedException (typeof (ArgumentNullException))]
		[Ignore ("Create")]
		public void CreateEx ()
		{
			var v = Variable.Create ("aname", null, VariableModifiers.None);
		}
		#endregion
		#endregion

		#region Properties
		[Test]
		[Ignore ("Not Implemented")]
		public void TypeCore () //Protected
		{
			throw new NotImplementedException ();
		}
		#endregion

		#region Ctors
		[Test]
		public void Ctor ()
		{
			var vStr = new Variable<string> ();
			Assert.IsNull (vStr.Name);
			Assert.AreEqual (vStr.Name, ((Variable) vStr).Name);
			Assert.IsNull (vStr.Default);
			Assert.AreEqual (vStr.Default, ((Variable) vStr).Default);
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			Assert.AreEqual (typeof (string), vStr.Type);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Ctor_DefaultExpression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Ctor_Name ()
		{
			var vStr = new Variable<string> ("aname");
			Assert.AreEqual ("aname", vStr.Name);
			Assert.AreEqual (vStr.Name, ((Variable) vStr).Name);
			Assert.IsNull (vStr.Default);
			Assert.AreEqual (vStr.Default, ((Variable) vStr).Default);
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			Assert.AreEqual (typeof (string), vStr.Type);
			// .NET doesnt raise error when null passed
			var v = new Variable<string> ((string) null);
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void Ctor_Name_DefaultExpression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Ctor_Name_DefaultValue ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			Assert.AreEqual ("aname", vStr.Name);
			Assert.AreEqual (vStr.Name, ((Variable) vStr).Name);
			Assert.IsInstanceOfType (typeof (Literal<string>), vStr.Default);
			Assert.AreEqual ("avalue", vStr.Default.ToString ());
			Assert.AreEqual (vStr.Default, ((Variable) vStr).Default);
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			Assert.AreEqual (typeof (string), vStr.Type);
			// .NET doesnt raise error when null passed
			var v = new Variable<string> ((string) null, (string) null);
		}
		#endregion

		#region Methods
		[Test]
		//FIXME: context param validation tests?
		public void TGet ()
		{
			var vStr = new Variable<string> ("", "avalue");

			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				string value = vStr.Get (context);
				Assert.AreEqual ("avalue", value);
			}));
		}
		[Test]
		public void OGet ()
		{
			var vStr = new Variable<string> ("", "avalue");

			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				string value = (string)((Variable) vStr).Get (context);
				Assert.AreEqual ("avalue", value);
			}));
		}
		[Test]
		public void GetLocation ()
		{
			var vStr = new Variable<string> ("", "avalue");

			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				var location = vStr.GetLocation (context);
				Assert.AreEqual ("avalue", location.Value);
				Assert.AreEqual (typeof (string), location.LocationType);
			}));
		}
		[Test]
		public void Set ()
		{
			var vStr = new Variable<string> ("", "avalue");

			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				string value = vStr.Get (context);
				Assert.AreEqual ("avalue", value);
				vStr.Set (context, "newVal");
				Assert.AreEqual ("newVal", vStr.Get (context));
			}));
		}
		[Test]
		public void OSet ()
		{
			var vStr = new Variable<string> ("", "avalue");
			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				string value = vStr.Get (context);
				Assert.AreEqual ("avalue", value);
				vStr.Set (context, (object) "newVal");
				Assert.AreEqual ("newVal", vStr.Get (context));
			}));
		}
		[Test, ExpectedException (typeof (InvalidOperationException))] 
		public void OSet_InvalidTypeEx ()
		{
			//System.InvalidOperationException: A value of type 'System.Int32' cannot be set 
			// to the location with name '' because it is a location of type 'System.String'.
			var vStr = new Variable<string> ("", "avalue");
			Exception ex = null;
			WorkflowInvoker.Invoke (new NativeActivityRunner ((metadata) => {
				metadata.AddImplementationVariable (vStr);
			}, (context) => {
				try {
					vStr.Set (context, 2);
				} catch (Exception ex2) {
					ex = ex2;
				}
			}));
			throw ex;
		}
		#endregion
	}
}
