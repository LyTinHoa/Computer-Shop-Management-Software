using ComputerShopManagement.Controllers;
using ComputerShopManagement.Models;

namespace ComputerShopManagement.Tests
{
    [TestClass]
    public sealed class ControllerTests
    {
        [TestMethod]
        public void CalculateFinalTotal_MatchesSumOfItems()
        {
            // Arrange
            SalesController salesCtrl = new SalesController();

            Product item1 = new Product { ProductID = 1, Price = 50.00m };
            Product item2 = new Product { ProductID = 2, Price = 25.00m };

            // Act: Add 2 of item1 ($100), and 1 of item2 ($25). Total should be 125.00.
            salesCtrl.AddItemsToInvoice(item1, 2);
            salesCtrl.AddItemsToInvoice(item2, 1);
            decimal result = salesCtrl.CalculateFinalTotal();

            // Assert
            Assert.AreEqual(125.00m, result);
        }

        [TestMethod]
        public void Authenticate_InvalidUser_ReturnsErrorCode()
        {
            // Arrange
            AccountController accountCtrl = new AccountController();

            // Act: Pass a fake username that definitely isn't in the database
            int result = accountCtrl.Authenticate("FakeGhostUser999", "Password123");

            // Assert: -1 is the specific error code your controller uses for "User Not Found"
            Assert.AreEqual(-1, result);
        }
    }
}