using System;
using System.Runtime.Serialization;

namespace System.Activities
{
	[DataContract]
	public abstract class Location
	{
		public Location ()
		{
			IsConst = false;
		}
		public abstract Type LocationType { get; }
		public object Value { 
			get { return ValueCore; } 
			set { this.ValueCore = value; }
		}
		protected abstract object ValueCore { get; set; }
		internal bool IsConst { get; set; }
		// these members arn't internal abstract as that would break public interface compatibility with .NET
		internal virtual void MakeDefault ()
		{
			throw new InvalidOperationException ();
		}
		internal virtual void SetConstValue (object value)
		{
			throw new InvalidOperationException ();
		}
	}

	[DataContract]
	public class Location<T> : Location
	{
		private T value;

		public Location () : base ()
		{
			Value = default (T);
		}
		public override Type LocationType {
			get { return typeof (T); }
		}
		public new virtual T Value { 
			get { return (T) ValueCore; }
			set { ValueCore = value; }
		}
		protected override object ValueCore { 
			get { return value; } 
			set {
				if (IsConst)
					throw new InvalidOperationException ("This location is marked as const, so " +
						"its value cannot be modified.");
				else
					this.value = (T) value;
			}
		}
		internal override void MakeDefault ()
		{
			ValueCore = default (T);
		}
		internal override void SetConstValue (object value)
		{
			this.value = (T) value;
		}
	}
}
