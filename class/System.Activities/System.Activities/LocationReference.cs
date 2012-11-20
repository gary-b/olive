using System;

namespace System.Activities
{
	public abstract class LocationReference
	{
		public string Name { get { 
				return NameCore; 
			} 
		}
		protected abstract string NameCore { get; }
		public Type Type { get { return TypeCore; } }
		protected abstract Type TypeCore { get; }
		
		public abstract Location GetLocation (ActivityContext context);
	}
}
