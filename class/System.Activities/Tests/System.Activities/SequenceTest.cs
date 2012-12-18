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
	class SequenceTest {
		void RunAndCompare (Activity workflow, string expectedOnConsole)
		{
			var sw = new StringWriter ();
			Console.SetOut (sw);
			WorkflowInvoker.Invoke (workflow);
			Assert.AreEqual (expectedOnConsole, sw.ToString ());
		}

		[Test]
		public void Activities ()
		{
			var seq = new Sequence ();
			Assert.IsNotNull (seq.Activities);
			Assert.AreEqual (0, seq.Activities.Count);
		}

		[Test]
		public void Variables ()
		{ 
			var seq = new Sequence ();
			Assert.IsNotNull (seq.Variables);
			Assert.AreEqual (0, seq.Variables.Count);
		}

		[Test]
		public void ActivitiesExecute ()
		{ 
			var wf = new Sequence {
				Activities = {
					new WriteLine {
						Text = "Act1"
					},
					new WriteLine {
						Text = "Act2"
					},
					new WriteLine {
						Text = "Act3"
					},
					new WriteLine {
						Text = "Act4"
					}
				}
			};
			RunAndCompare (wf, String.Format ("Act1{0}Act2{0}Act3{0}Act4{0}", Environment.NewLine));
		}

		[Test]
		public void VariablesExecute ()
		{ 
			var v1 = new Variable<string> ("a","v1");
			var v2 = new Variable<string> ("b","v2");

			var wf = new Sequence {
				Variables = {
					v1,v2
				},
				Activities = {
					new WriteLine {
						Text = v1
					},
					new WriteLine {
						Text = v2
					}
				}
			};
			RunAndCompare (wf, String.Format ("v1{0}v2{0}", Environment.NewLine));
		}

		/* already tested
		[Test]
		public void Execute () //protected
		{
			throw new NotImplementedException ();
		}
		*/
	}
}
