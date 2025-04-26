using System;
using System.Globalization;

public class MedicalSchoolFinanceSimulator
{
    public static void Main(string[] args)
    {
        Console.WriteLine("--- Medical School Finance Simulator ---");
        Console.WriteLine("This program simulates your finances over a 48-month (4-year) program, starting June 2025.");
        Console.WriteLine("Includes semi-annual tuition and semi-annual bulk income (ESPP/RSU).");
        Console.WriteLine("----------------------------------------");

        // --- Get User Input ---
        decimal initialBalance = GetDecimalInput("Enter your initial starting account balance: $");
        decimal monthlyIncome = GetDecimalInput("Enter your estimated average monthly income (e.g., part-time work, stipends): $");
        decimal monthlyExpenses = GetDecimalInput("Enter your estimated average monthly living expenses (rent, food, etc., excluding tuition): $");
        decimal tuitionCost = GetDecimalInput("Enter the semi-annual tuition payment amount (paid every June and December): $");
        decimal esppPayout = GetDecimalInput("Enter the semi-annual ESPP payout amount (paid every March and September): $");
        decimal rsuPayout = GetDecimalInput("Enter the semi-annual RSU vesting value (paid every March and September): $");


        // --- Simulation Setup ---
        int totalMonths = 48;
        decimal currentBalance = initialBalance;
        // Start date: June 1, 2025
        DateTime currentDate = new DateTime(2025, 6, 1);
        CultureInfo culture = CultureInfo.CurrentCulture; // For currency formatting

        // --- Print Header ---
        Console.WriteLine("\n--- Monthly Financial Simulation ---");
        Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------");
        // Adjust padding for the new columns
        Console.WriteLine("{0,-10} | {1,18} | {2,15} | {3,15} | {4,15} | {5,15} | {6,18} | {7,18}",
                          "Month/Year", "Start Balance", "Monthly Income", "ESPP Income", "RSU Income", "Expenses", "Tuition Paid", "End Balance");
        Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------");

        // --- Run Simulation ---
        for (int i = 0; i < totalMonths; i++) // Loop 48 times
        {
            decimal startingBalanceOfMonth = currentBalance;
            decimal tuitionPaidThisMonth = 0;
            decimal esppIncomeThisMonth = 0;
            decimal rsuIncomeThisMonth = 0;

            // Add regular monthly income
            currentBalance += monthlyIncome;

            // Check for ESPP/RSU payouts (March or September)
            if (currentDate.Month == 3 || currentDate.Month == 9) // 3 = March, 9 = September
            {
                esppIncomeThisMonth = esppPayout;
                rsuIncomeThisMonth = rsuPayout;
                currentBalance += esppIncomeThisMonth;
                currentBalance += rsuIncomeThisMonth;
            }

            // Subtract expenses
            currentBalance -= monthlyExpenses;

            // Check for tuition payment (June or December)
            if (currentDate.Month == 6 || currentDate.Month == 12) // 6 = June, 12 = December
            {
                tuitionPaidThisMonth = tuitionCost;
                currentBalance -= tuitionCost;
            }

            // --- Print Monthly Details ---
            // Format date as "MMM yyyy" (e.g., "Jun 2025")
            string monthYearString = currentDate.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            Console.WriteLine("{0,-10} | {1,18:C} | {2,15:C} | {3,15:C} | {4,15:C} | {5,15:C} | {6,18:C} | {7,18:C}",
                              monthYearString,
                              startingBalanceOfMonth,
                              monthlyIncome, // Regular monthly income
                              esppIncomeThisMonth, // ESPP income for this month
                              rsuIncomeThisMonth,  // RSU income for this month
                              monthlyExpenses,
                              tuitionPaidThisMonth,
                              currentBalance);

            // Optional: Add a check for negative balance
            if (currentBalance < 0)
            {
                Console.WriteLine($"   *** WARNING: Account balance is negative in {monthYearString}! ***");
            }

            // Move to the next month
            currentDate = currentDate.AddMonths(1);
        }

        // --- Print Final Summary ---
        Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------");
        // Display the final month/year correctly
        Console.WriteLine($"\nSimulation Complete. Final balance after {currentDate.AddMonths(-1).ToString("MMM yyyy", CultureInfo.InvariantCulture)}: {currentBalance:C}");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    // --- Helper function to get valid decimal input ---
    public static decimal GetDecimalInput(string prompt)
    {
        decimal value;
        bool validInput = false;
        do
        {
            Console.Write(prompt);
            string input = Console.ReadLine();

            // Try parsing using the current culture's currency settings
            if (decimal.TryParse(input, NumberStyles.Currency, CultureInfo.CurrentCulture, out value))
            {
                if (value >= 0)
                {
                    validInput = true;
                }
                else
                {
                     Console.WriteLine("Invalid input. Please enter a non-negative number.");
                }
            }
            // Fallback: Try parsing as a plain number
            else if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            {
                 if (value >= 0)
                {
                    validInput = true;
                }
                else
                {
                     Console.WriteLine("Invalid input. Please enter a non-negative number.");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid number (e.g., 5000.50 or $5,000.50).");
            }

        } while (!validInput);

        return value;
    }
}