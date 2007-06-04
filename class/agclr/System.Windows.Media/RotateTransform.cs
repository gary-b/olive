//
// System.Windows.Media.RotateTransform class
//
// Authors:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
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

namespace System.Windows.Media {

	public class RotateTransform : Transform {

		public static readonly DependencyProperty AngleProperty = DependencyProperty.Register ("Angle", typeof (double), typeof (RotateTransform));
		public static readonly DependencyProperty CenterXProperty = DependencyProperty.Register ("CenterX", typeof (double), typeof (RotateTransform));
		public static readonly DependencyProperty CenterYProperty = DependencyProperty.Register ("CenterY", typeof (double), typeof (RotateTransform));


		public RotateTransform ()
		{
		}


		public double Angle {
			get { return (double) GetValue (AngleProperty); }
			set { SetValue (AngleProperty, value); }
		}

		public double CenterX {
			get { return (double) GetValue (CenterXProperty); }
			set { SetValue (CenterXProperty, value); }
		}

		public double CenterY {
			get { return (double) GetValue (CenterYProperty); }
			set { SetValue (CenterYProperty, value); }
		}
	}
}