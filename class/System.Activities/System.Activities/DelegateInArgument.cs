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
		public DelegateInArgument ()
		{
		}

		public DelegateInArgument (string name)
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
