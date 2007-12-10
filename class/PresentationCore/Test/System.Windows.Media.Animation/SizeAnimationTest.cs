
/* this file is generated by gen-animation-types.cs.  do not modify */

using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using NUnit.Framework;

namespace MonoTests.System.Windows.Media.Animation {


[TestFixture]
public class SizeAnimationTest
{
	[Test]
	public void Properties ()
	{
		Assert.AreEqual ("By", SizeAnimation.ByProperty.Name, "1");
		Assert.AreEqual (typeof (SizeAnimation), SizeAnimation.ByProperty.OwnerType, "2");
		Assert.AreEqual (typeof (Size?), SizeAnimation.ByProperty.PropertyType, "3");

		Assert.AreEqual ("From", SizeAnimation.FromProperty.Name, "4");
		Assert.AreEqual (typeof (SizeAnimation), SizeAnimation.FromProperty.OwnerType, "5");
		Assert.AreEqual (typeof (Size?), SizeAnimation.FromProperty.PropertyType, "6");

		Assert.AreEqual ("To", SizeAnimation.ToProperty.Name, "7");
		Assert.AreEqual (typeof (SizeAnimation), SizeAnimation.ToProperty.OwnerType, "8");
		Assert.AreEqual (typeof (Size?), SizeAnimation.ToProperty.PropertyType, "9");
	}
}


}