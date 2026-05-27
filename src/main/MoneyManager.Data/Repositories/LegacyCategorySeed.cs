using MoneyManager.Core.Models;

namespace MoneyManager.Data.Repositories
{
	/// <summary>
	/// Authoritative legacy category set (Categories.txt). ParentCategory_I is null for top-level rows.
	/// </summary>
	public static class LegacyCategorySeed
	{
		public static IReadOnlyList<Category> Categories { get; } = new List<Category>
		{
			C(3, "Tithe", 90, true),
			C(4, "Housing", null, false),
			C(5, "Transportation", null, false),
			C(6, "Groceries", null, true),
			C(7, "Entertainment", null, false),
			C(8, "Insurance", null, false),
			C(9, "PhoneCards", null, true),
			C(10, "Cell Phone", null, true),
			C(12, "Education", null, true),
			C(13, "Investments", null, true),
			C(14, "Doctor Visit", 75, true),
			C(15, "Clothing", null, false),
			C(16, "Other Giving", 90, true),
			C(17, "Miscellaneous", null, false),
			C(18, "Gas - Car", 5, true),
			C(19, "Split", null, false),
			C(20, "Savings", null, false),
			C(21, "Dining/Eating Out", 7, false),
			C(42, "Vacation", null, false),
			C(43, "Health", 75, false),
			C(44, "Wedding", 46, false),
			C(45, "Baby", null, true),
			C(46, "Special Occasions", null, false),
			C(48, "Prescriptions", 75, true),
			C(49, "Medicine/Drugs", 75, true),
			C(50, "Dental", 75, true),
			C(52, "Electricity", 4, true),
			C(53, "Cable/Internet", 4, true),
			C(54, "Auto Maintenance", 5, true),
			C(55, "Auto Insurance", 5, true),
			C(56, "Other Income", null, false),
			C(57, "Mortgage/Rent", 4, true),
			C(58, "House Insurance", 4, true),
			C(60, "Natural Gas", 4, false),
			C(61, "Water", 4, true),
			C(64, "Auto Payment", 5, true),
			C(66, "Auto Taxes", 5, false),
			C(68, "Life Insurance", 8, true),
			C(69, "Health Insurance", 8, false),
			C(70, "Baby-sitters", 7, false),
			C(71, "Activities/Trips", null, false),
			C(75, "Medical Expenses", null, false),
			C(78, "Personal Care", null, false),
			C(80, "School Tuition", 12, true),
			C(81, "Other Expenses", null, false),
			C(82, "Other Housing", 4, false),
			C(83, "Streaming providers", 7, true),
			C(84, "Business", null, true),
			C(86, "Bert Cash", null, true),
			C(87, "Debts", null, false),
			C(88, "Tools", null, false),
			C(89, "Children", null, false),
			C(90, "Giving", null, false),
			C(91, "Offering", 90, true),
			C(92, "Uber/Taxi/Bus", 5, false),
			C(94, "School bus", 5, false),
			C(95, "Cleaning Supplies", 4, true),
			C(96, "Gifts", null, false),
			C(97, "Music Lessons", 12, false),
			C(98, "Sports", 12, false),
			C(99, "School Supplies", 12, false),
			C(100, "Toys/Gadgets", null, false),
			C(101, "Furnishings/Appliances", 4, false),
			C(102, "Pet Supplies", null, false),
			C(103, "Parking", 5, false),
			C(104, "Taxes", null, true),
			C(105, "Rocio Cash", null, false),
			C(106, "Vacation Food", 42, false),
			C(107, "Vacation Gnd Transport", 42, false),
			C(108, "Vacation Activity", 42, false),
			C(109, "Vacation Lodging", 42, false),
			C(110, "Vacation Restaurant", 42, false),
			C(111, "Outdoors", null, false),
		};

		private static Category C(int id, string name, int? parent, bool required) => new()
		{
			Category_I = id,
			Name = name,
			ParentCategory_I = parent,
			Required = required,
			Archived = false
		};
	}
}
