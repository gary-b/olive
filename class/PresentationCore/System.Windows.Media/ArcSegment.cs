// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2008 Novell, Inc. (http://www.novell.com)
//
// Author:
//	Chris Toshok (toshok@ximian.com)
//

using System.Windows;
using System.Windows.Media.Animation;

namespace System.Windows.Media {

	public class ArcSegment : PathSegment {
		public ArcSegment (Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked)
		{
			Point = point;
			Size = size;
			IsLargeArc = isLargeArc;
			RotationAngle = rotationAngle;
			SweepDirection = sweepDirection;
		}

		public ArcSegment ()
		{
		}

		public new ArcSegment Clone ()
		{
			throw new NotImplementedException ();
		}

		public new ArcSegment CloneCurrentValue ()
		{
			throw new NotImplementedException ();
		}

		protected override Freezable CreateInstanceCore ()
		{
			return new ArcSegment ();
		}

		public static readonly DependencyProperty PointProperty;
		public Point Point {
		    get { return (Point)GetValue (PointProperty); }
		    set { SetValue (PointProperty, value); }
		}

		public static readonly DependencyProperty IsLargeArcProperty;
		public bool IsLargeArc {
			get { return (bool)GetValue (IsLargeArcProperty); }
			set { SetValue (IsLargeArcProperty, value); }
		}

		public static readonly DependencyProperty SweepDirectionProperty;
		public SweepDirection SweepDirection {
			get { return (SweepDirection)GetValue (SweepDirectionProperty); }
			set { SetValue (SweepDirectionProperty, value); }
		}

		public static readonly DependencyProperty SizeProperty;
		public Size Size {
			get { return (Size)GetValue (SizeProperty); }
			set { SetValue (SizeProperty, value); }
		}

		public static readonly DependencyProperty RotationAngleProperty;
		public double RotationAngle {
			get { return (double)GetValue (RotationAngleProperty); }
			set { SetValue (RotationAngleProperty, value); }
		}
	}

}