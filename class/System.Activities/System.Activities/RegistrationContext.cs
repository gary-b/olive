using System;

namespace System.Activities
{
	public sealed class RegistrationContext
	{
		ExecutionProperties Properties { get; set; }
		internal RegistrationContext (ExecutionProperties properties)
		{
			if (properties == null)
				throw new ArgumentNullException ("properties");

			Properties = properties;
		}

		public object FindProperty (string name)
		{
			return Properties.Find (name);
		}
	}
}
