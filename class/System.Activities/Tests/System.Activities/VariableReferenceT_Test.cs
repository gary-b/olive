﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using NUnit.Framework;
using System.Activities.Statements;
using System.IO;
using System.Activities.Expressions;

namespace Tests.System.Activities {
	class VariableReferenceT_Test {
		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}

		#region Ctors

		[Test]
		public void VariableReference_Ctor ()
		{
			var vr = new VariableReference<string> ();
			Assert.IsNull (vr.Variable);
		}
		[Test]
		public void VariableReferenceVariable_Ctor ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vr = new VariableReference<string> (vStr);
			Assert.AreSame (vStr, vr.Variable);
			
			// .NET does not throw error when type param of Variable clashes with that if VR
			var vInt = new Variable<int> ("aname", 42);
			var vr2 = new VariableReference<string> (vInt);
			Assert.AreSame (vInt, vr2.Variable);
			// .NET doesnt throw error on null param
			var vr3 = new  VariableReference<string> (null);
		}

		#endregion

		#region Properties
		[Test]
		public void Variable ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vInt = new Variable<int> ("aname", 42);
			var vr = new VariableReference<string> (vStr);
			Assert.AreSame (vStr, vr.Variable);
			
			vr.Variable = vInt;
			Assert.AreSame (vInt, vr.Variable);
			
			vr.Variable = null;
			Assert.IsNull (vr.Variable);
		}
		#endregion

		#region Methods
		[Test]
		public void ToStringTest ()
		{
			var vStr = new Variable<string> ("aname", "avalue");
			var vInt = new Variable<int> ("intName", 42);
			var vr = new VariableReference<string> (vStr);
			Assert.AreEqual ("aname", vr.ToString ());
			
			vr.Variable = vInt;
			Assert.AreEqual ("intName", vr.ToString ());
		}
		//FIXME: convoluted test
		[Test]
		public void Execute () //protected
		{
			var PubVar = new Variable<string> ("", "HelloPublic");
			
			var wf = new Sequence {
				Variables = {
					PubVar
				},
				Activities = {
					new WriteLine {
						Text = PubVar
					},
					new Assign {
						Value = new InArgument<string> ("AnotherValue"),
						To = new OutArgument<string> (PubVar) // OutArg .Expression will be a VariableReference
					},
					new WriteLine {
						Text = PubVar
					}
				}
			};
			RunAndCompare (wf, String.Format ("HelloPublic{0}AnotherValue{0}", Environment.NewLine));
		}
		#endregion
	}
}
