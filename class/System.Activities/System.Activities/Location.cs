using System;
using System.Runtime.Serialization;

namespace System.Activities
{
	[DataContract]
	public abstract class Location
	{
		public abstract Type LocationType { get; }
		public object Value { get; set; }
		protected abstract object ValueCore { get; set; }
	}

	[DataContract]
	public class Location<T> : Location
	{
		public Location () : base ()
		{
			Value = default (T);
		}

		public override Type LocationType {
			get { return typeof (T); }
		}
		public new virtual T Value { 
			get {
				return (T) base.Value; //FIXME: test how .NET handles this
			}
			set {
				base.Value = value;
			}
		}
		protected override object ValueCore { get; set; }
	}
}
