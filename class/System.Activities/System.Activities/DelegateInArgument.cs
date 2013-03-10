namespace System.Activities
{
	public abstract class DelegateInArgument : DelegateArgument
	{
		internal DelegateInArgument ()
		{
			Direction = ArgumentDirection.In;
		}
	}

	public sealed class DelegateInArgument<T> : DelegateInArgument
	{
		Type argType;

		public DelegateInArgument () : base ()
		{
			argType = typeof (T);
		}

		public DelegateInArgument (string name) : this ()
		{
			Name = name;
		}

		protected override Type TypeCore {
			get { return argType; }
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
