
/* this file is generated by gen-animation-types.cs.  do not modify */

using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;

namespace System.Windows.Media.Animation {


public class LinearSizeKeyFrame : SizeKeyFrame
{
	Size value;
	KeyTime keyTime;

	public LinearSizeKeyFrame ()
	{
	}

	public LinearSizeKeyFrame (Size value)
	{
		this.value = value;
		// XXX keytime?
	}

	public LinearSizeKeyFrame (Size value, KeyTime keyTime)
	{
		this.value = value;
		this.keyTime = keyTime;
	}

	protected override Freezable CreateInstanceCore ()
	{
		throw new NotImplementedException ();
	}

	protected override Size InterpolateValueCore (Size baseValue, double keyFrameProgress)
	{
		// standard linear interpolation
		return new Size (baseValue.Width + (value.Width - baseValue.Width) * keyFrameProgress, baseValue.Height + (value.Height - baseValue.Height) * keyFrameProgress);
	}
}


}