using Bookworm_Bot_Automation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class AutomationConfigValidationTests
	{
		[TestMethod]
		public void AutomationConfig_rejects_board_outside_client()
		{
			AutomationConfig config = AutomationConfig.FromBoardBounds(1113, 627, 2133, 403, 2334, 607);

			Assert.IsFalse(config.FitsWithinClient());
			Assert.IsFalse(config.IsValid);
			Assert.IsTrue(config.DescribeValidationIssue().Contains("outside"));
		}
	}
}
