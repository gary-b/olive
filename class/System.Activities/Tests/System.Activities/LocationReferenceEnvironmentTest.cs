using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Activities;

namespace Tests.System.Activities {
	class LocationReferenceEnvironmentTest {
		class LocRefEnvMock : LocationReferenceEnvironment {
			public override IEnumerable<LocationReference> GetLocationReferences ()
			{
				throw new NotImplementedException ();
			}
			public override bool IsVisible (LocationReference locationReference)
			{
				throw new NotImplementedException ();
			}
			public override Activity Root
			{
				get { throw new NotImplementedException (); }
			}
			public override bool TryGetLocationReference (string name, out LocationReference result)
			{
				throw new NotImplementedException ();
			}
			public void SetParent (LocationReferenceEnvironment parent)
			{
				this.Parent = parent;
			}
		}
		[Test]
		public void Parent ()
		{
			var env1 = new LocRefEnvMock ();
			var env2 = new LocRefEnvMock ();

			Assert.IsNull(env1.Parent);
			env2.SetParent (null);
			env2.SetParent (env1);
			Assert.AreSame (env1, env2.Parent);
		}
	}
}

