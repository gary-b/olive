using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	class VariableT_Test {

		#region From Variable
		#region Properties
		//[IgnoreDataMemberAttribute]
		[Test]
		public void NewDefault ()
		{
			var vStr = new Variable<string> ();
			var strLit = new Literal<string> ("Hello\nWorld");
			vStr.Default = strLit;
			Assert.AreSame (strLit, vStr.Default);
			vStr.Default = null;
			Assert.IsNull (vStr.Default);
		}
		[Test]
		public void Modifiers ()
		{
			throw new NotImplementedException (); //FIXME: test affect
		}
		[Test]
		public void Name ()
		{
			var vStr = new Variable<string> ();
			vStr.Name = "test";
			Assert.AreEqual ("test", vStr.Name);
			vStr.Name = String.Empty;
			Assert.AreEqual (String.Empty, vStr.Name);
			vStr.Name = null;
			Assert.IsNull (vStr.Name);
		}
		/* protected
		[Test]
		public void NameCore () 
		{
			throw new NotImplementedException ();
		}
		*/
		#endregion
		#region Static Methods
		[Test]
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
		public void CreateEx ()
		{
			var v = Variable.Create ("aname", null, VariableModifiers.None);
		}
		#endregion
		#endregion

		#region Properties
		[Test]
		public void Default ()
		{
			var vStr = new Variable<string> ("","Hello\nWorld");
			Assert.AreEqual ("Hello\nWorld", vStr.Default.ToString ());
			Assert.AreEqual ("Hello\nWorld", ((Variable) vStr).Default.ToString ());//OldDefault
			/*
			var intLit = new Literal<int> (42);
			((Variable) vStr).Default = intLit;
			Assert.AreEqual ("Activity", vStr.Default.ToString ()); // comes back with ActivityWithResultWrapper<string>
			*/
		}
		[Test]
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
			Assert.IsNull (vStr.Default);
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			Assert.AreEqual (typeof (string), vStr.Type);
		}
		[Test]
		public void Ctor_DefaultExpression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Ctor_Name ()
		{
			var vStr = new Variable<string> ("aname");
			Assert.AreEqual ("aname", vStr.Name);
			Assert.IsNull (vStr.Default);
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			Assert.AreEqual (typeof (string), vStr.Type);
			// .NET doesnt raise error when null passed
			var v = new Variable<string> ((string) null);
		}
		[Test]
		public void Ctor_Name_DefaultExpression ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		public void Ctor_Name_DefaultValue ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			Assert.AreEqual ("aname", vStr.Name);
			Assert.AreEqual ("avalue", vStr.Default.ToString ());
			Assert.AreEqual (VariableModifiers.None, vStr.Modifiers);
			Assert.AreEqual (typeof (string), vStr.Type);
			// .NET doesnt raise error when null passed
			var v = new Variable<string> ((string) null, (string) null);
		}
		#endregion

		#region Methods
		[Test]
		public void TGet ()
		{
			// FIXME: monodevelops autocomplete isnt picking up T Get(..), its reporting object Get(..)
			var vStr = new Variable<string> ("", "avalue");
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddImplementationVariable (vStr);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				string value = vStr.Get (context);
				Assert.AreEqual ("avalue", value);
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (metadataAction, executeAction));
		}
		[Test]
		public void OGet ()
		{
			var vStr = new Variable<string> ("", "avalue");
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddImplementationVariable (vStr);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				string value = (string)((Variable) vStr).Get (context);
				Assert.AreEqual ("avalue", value);
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (metadataAction, executeAction));
		}
		[Test]
		public void GetLocation ()
		{
			var vStr = new Variable<string> ("", "avalue");
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddImplementationVariable (vStr);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				var location = vStr.GetLocation (context);
				Assert.AreEqual ("avalue", location.Value);
				Assert.AreEqual (typeof (string), location.LocationType);
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (metadataAction, executeAction));
		}
		[Test]
		public void Set ()
		{
			var vStr = new Variable<string> ("", "avalue");
			Action<NativeActivityMetadata> metadataAction = (metadata) => {
				metadata.AddImplementationVariable (vStr);
			};
			Action<NativeActivityContext> executeAction = (context) => {
				string value = vStr.Get (context);
				Assert.AreEqual ("avalue", value);
				vStr.Set (context, "newVal");
				Assert.AreEqual ("newVal", vStr.Get (context));
			};
			WorkflowInvoker.Invoke (new NativeRunnerMock (metadataAction, executeAction));
		}
		#endregion
	}
}
