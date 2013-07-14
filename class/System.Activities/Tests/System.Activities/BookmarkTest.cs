using System;
using NUnit.Framework;
using System.Activities;

namespace Tests.System.Activities {
	[TestFixture]
	public class BookmarkTest : WFTest {
		[Test, ExpectedException (typeof (ArgumentException))]
		public void Bookmark_NameNullEx ()
		{
			new Bookmark (null);
		}
		[Test, ExpectedException (typeof (ArgumentException))]
		public void Bookmark_NameEmptyEx ()
		{
			//System.ArgumentException : The argument name is null or empty.
			//Parameter name: name
			new Bookmark (String.Empty);
		}
		[Test]
		public void Bookmark_EqualsOverride ()
		{
			var b1 = new Bookmark ("b");
			var b2 = new Bookmark ("b");
			Assert.IsTrue (b1.Equals (b2));
			Assert.IsFalse (b1 == b2);
			bool noNameNamesAreEqual = false, noNameBookmarksEquals = false, noNameBookmarkEqualsSelf = false;
			var wf = new NativeActivityRunner (null, (context) => {
				var bNoName1 = context.CreateBookmark ();
				var bNoName2 = context.CreateBookmark ();

				noNameNamesAreEqual = (bNoName1.Name == bNoName2.Name);
				noNameBookmarksEquals = bNoName1.Equals (bNoName2);
				noNameBookmarkEqualsSelf = bNoName1.Equals (bNoName1);

			});
			wf.InduceIdle = true;
			GetWFAppWrapperAndRun (wf, WFAppStatus.Idle);
			Assert.IsTrue (noNameNamesAreEqual);
			Assert.IsFalse (noNameBookmarksEquals);
			Assert.IsTrue (noNameBookmarkEqualsSelf);
		}
	}
}

