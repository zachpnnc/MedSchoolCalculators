using System;
using System.Globalization; // Required for NumberStyles and CultureInfo

namespace GraduateLoanInterestCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Graduate Student Loan Interest Calculator ---");
            Console.WriteLine("Calculates interest accrued during study, a grace period, and monthly repayment schedule.");
            Console.WriteLine("-------------------------------------------------");

            // --- Get User Input ---
            decimal costPerYear = GetDecimalInput("Enter the loan amount taken per year: $");
            decimal annualInterestRatePercent = GetDecimalInput("Enter the annual interest rate (e.g., 5 for 5%): ");

            // --- Constants ---
            const int numberOfYearsStudy = 4;
            const int gracePeriodMonths = 6;
            const int repaymentYears = 10;
            const int monthsInYear = 12;

            // --- Calculations ---

            // Convert annual rate percentage to decimal
            decimal annualInterestRateDecimal = annualInterestRatePercent / 100.0m;
            // Calculate the monthly interest rate for compounding during grace/repayment
            decimal monthlyInterestRate = annualInterestRateDecimal / monthsInYear;

            decimal totalPrincipalBorrowed = 0m;
            decimal totalSimpleInterestDuringStudy = 0m;

            Console.WriteLine("\n--- Interest Accrual During Study (Simple Interest) ---");
            // Calculate simple interest accrued during the 4 years of study
            for (int year = 1; year <= numberOfYearsStudy; year++)
            {
                decimal currentLoanPrincipal = costPerYear;
                totalPrincipalBorrowed += currentLoanPrincipal;

                int yearsOfInterestAccrual = numberOfYearsStudy - year + 1;
                decimal interestForThisLoan = currentLoanPrincipal * annualInterestRateDecimal * yearsOfInterestAccrual;
                totalSimpleInterestDuringStudy += interestForThisLoan;
            }

            decimal balanceAfterStudy = totalPrincipalBorrowed + totalSimpleInterestDuringStudy;
            Console.WriteLine($"\nTotal Principal Borrowed: {totalPrincipalBorrowed:C}");
            Console.WriteLine($"Total Simple Interest During Study: {totalSimpleInterestDuringStudy:C}");
            Console.WriteLine($"Balance at End of Study (Year {numberOfYearsStudy}): {balanceAfterStudy:C}");

            // --- Grace Period Calculation (Monthly Compounding) ---
            Console.WriteLine($"\n--- Grace Period ({gracePeriodMonths} months) ---");
            decimal balanceAfterGrace = balanceAfterStudy;

            // Check if there's interest to compound
            if (monthlyInterestRate > 0 && balanceAfterGrace > 0)
            {
                // Formula: P * (1 + r)^n
                double compoundFactor = Math.Pow(1.0 + (double)monthlyInterestRate, gracePeriodMonths);
                balanceAfterGrace = balanceAfterStudy * (decimal)compoundFactor;
            }

            decimal interestAccruedDuringGrace = balanceAfterGrace - balanceAfterStudy;
            Console.WriteLine($"Interest Accrued During Grace Period (Compounded Monthly): {interestAccruedDuringGrace:C}");
            Console.WriteLine($"Balance at End of Grace Period (Start of Repayment): {balanceAfterGrace:C}");


            // --- Monthly Repayment Calculation ---
            Console.WriteLine($"\n--- Repayment Calculation ({repaymentYears} Years) ---");
            decimal monthlyPayment = 0m;
            int totalNumberOfPayments = repaymentYears * monthsInYear;
            decimal currentBalance = balanceAfterGrace; // Initialize balance for repayment simulation

            if (currentBalance <= 0)
            {
                Console.WriteLine("No balance to repay.");
            }
            else if (monthlyInterestRate <= 0)
            {
                Console.WriteLine($"Interest rate is 0% or less.");
                if (totalNumberOfPayments > 0)
                {
                    monthlyPayment = currentBalance / totalNumberOfPayments;
                    Console.WriteLine($"Calculated Monthly Payment (no interest): {monthlyPayment:C}");
                }
                else
                {
                     Console.WriteLine("Cannot calculate payment with zero repayment period.");
                     monthlyPayment = 0;
                }
            }
            else
            {
                // Standard Loan Payment Formula (Annuity Formula)
                double monthlyRateDouble = (double)monthlyInterestRate;
                double powerTerm = Math.Pow(1.0 + monthlyRateDouble, totalNumberOfPayments);
                decimal numerator = currentBalance * monthlyInterestRate * (decimal)powerTerm;
                decimal denominator = (decimal)powerTerm - 1.0m;

                if (denominator != 0)
                {
                    monthlyPayment = numerator / denominator;
                    Console.WriteLine($"Calculated Monthly Payment over {repaymentYears} years ({totalNumberOfPayments} payments): {monthlyPayment:C}");
                }
                else
                {
                     Console.WriteLine("Error: Cannot calculate monthly payment due to zero denominator in formula.");
                     monthlyPayment = 0;
                }
            }

            // --- Amortization Schedule ---
            if (currentBalance > 0 && monthlyPayment > 0)
            {
                Console.WriteLine("\n--- Monthly Amortization Schedule ---");
                // Updated Header
                Console.WriteLine("Month | Start Balance | Payment | Interest Paid | Principal Paid | End Balance   | Total Paid");
                Console.WriteLine("------|---------------|---------|---------------|----------------|---------------|-----------");

                decimal totalPaidSoFar = 0m; // Initialize total paid tracker

                for (int month = 1; month <= totalNumberOfPayments; month++)
                {
                    decimal interestForMonth = currentBalance * monthlyInterestRate;
                    decimal principalPaid;
                    decimal actualPayment = monthlyPayment;
                    decimal startBalanceOfMonth = currentBalance; // Store for printing

                    // Adjust last payment to ensure balance hits exactly zero
                    if (month == totalNumberOfPayments || monthlyPayment >= currentBalance + interestForMonth)
                    {
                         // If it's the last payment OR the standard payment clears the balance + interest
                         actualPayment = currentBalance + interestForMonth;
                         principalPaid = currentBalance; // Pay off remaining principal
                         interestForMonth = actualPayment - principalPaid; // Recalculate interest based on actual final payment
                    }
                    else
                    {
                        principalPaid = monthlyPayment - interestForMonth;
                    }

                    // Defensive coding: ensure principal doesn't exceed balance or go negative
                    if (principalPaid > currentBalance) principalPaid = currentBalance;
                    if (principalPaid < 0) principalPaid = 0; // Interest might be higher than payment briefly if balance is tiny

                    // Update the balance
                    currentBalance -= principalPaid;

                     // Update total paid *after* calculating the actual payment for the month
                    totalPaidSoFar += actualPayment;

                    // Ensure balance is exactly zero at the end, correcting tiny rounding errors
                    if (month == totalNumberOfPayments)
                    {
                        currentBalance = 0m;
                        // Optional: Adjust final principal/interest slightly if needed so they sum perfectly to final actualPayment
                        interestForMonth = actualPayment - principalPaid;
                    }


                    // Print the details for the current month, including the new total paid column
                    Console.WriteLine($"{month,5} | {startBalanceOfMonth,13:C} | {actualPayment,7:C} | {interestForMonth,13:C} | {principalPaid,14:C} | {currentBalance,13:C} | {totalPaidSoFar,11:C}");


                    // Stop if balance is paid off early (less likely now with final payment adjustment)
                    if (currentBalance <= 0 && month < totalNumberOfPayments)
                    {
                        Console.WriteLine($"Balance paid off early in month {month}. Final total paid: {totalPaidSoFar:C}");
                        break;
                    }
                }
                 // Optional: Print final summary totals after the loop
                 Console.WriteLine("------------------------------------------------------------------------------------------");
                 Console.WriteLine($"End of Repayment. Total Paid over {totalNumberOfPayments} months: {totalPaidSoFar:C}");
            }
            else if (currentBalance > 0 && monthlyPayment <= 0)
            {
                Console.WriteLine("\nCannot generate amortization schedule because monthly payment could not be calculated.");
            }


            // Keep the console window open
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Prompts the user for input and attempts to parse it as a non-negative decimal value.
        /// Handles currency symbols and standard number formats based on the system's culture.
        /// Reprompts the user until valid input is received.
        /// </summary>
        /// <param name="prompt">The message to display to the user.</param>
        /// <returns>A non-negative decimal value entered by the user.</returns>
        static decimal GetDecimalInput(string prompt)
        {
            decimal value;
            string? input; // Use nullable string type
            while (true)
            {
                Console.Write(prompt);
                input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Input cannot be empty. Please try again.");
                    continue;
                }

                NumberStyles styles = NumberStyles.Currency | NumberStyles.AllowDecimalPoint |
                                      NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands;

                // Attempt to parse using CurrentCulture, removing the currency symbol first for robustness
                if (decimal.TryParse(input.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol, "").Trim(),
                                     styles & ~NumberStyles.AllowCurrencySymbol,
                                     CultureInfo.CurrentCulture, out value))
                {
                    if (value >= 0)
                    {
                        return value; // Valid, non-negative input
                    }
                    else
                    {
                        Console.WriteLine("Input error: Please enter a non-negative number.");
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid input. Please enter a valid number (e.g., 5000 or 5.5). Issue parsing: '{input}'");
                }
            }
        }
    }
}
