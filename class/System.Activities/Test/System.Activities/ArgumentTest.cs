using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;

namespace MonoTests.System.Activities {
	// Argument cant be inherited on .NET by users
	[TestFixture]
	public class ArgumentTest {
		#region Static Fields
		[Test]
		public void ResultValue ()
		{
			Assert.AreEqual ("Result", Argument.ResultValue);
		}
		[Test]
		public void UnspecifiedEvaluationOrder ()
		{
			Assert.AreEqual (-1, Argument.UnspecifiedEvaluationOrder);
		}
		#endregion

		#region Static Methods
		[Test]
		[Ignore ("Not Implemented")]
		public void Create ()
		{
			throw new NotImplementedException ();
		}
		[Test]
		[Ignore ("Not Implemented")]
		public void CreateReference ()
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
	
}
