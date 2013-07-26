namespace System.Activities
{
	public abstract class DelegateOutArgument : DelegateArgument
	{
		internal DelegateOutArgument ()
		{
			Direction = ArgumentDirection.Out;
		}
	}
	
	public sealed class DelegateOutArgument<T> : DelegateOutArgument
	{
		public DelegateOutArgument ()
		{
		}

		public DelegateOutArgument (string name)
		{
			Name = name;
		}

		protected override Type TypeCore {
			get { return typeof (T); }
		}

		public new T Get (ActivityContext context)
		{
			throw new NotImplementedException ();
		}
		public void Set (ActivityContext context, T value)
		{
			throw new NotImplementedException ();
		}
	}
}
