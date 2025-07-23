using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

public class EnergyUsageParser
{
    // Data structure to hold processed energy usage information.
    private class EnergyUsageData
    {
        public decimal DiscountEnergy { get; set; }
        public decimal OffPeakEnergy { get; set; }
        public decimal OnPeakEnergy { get; set; }
        public decimal TotalEnergy { get; set; }
        public decimal HighestOnPeakValue { get; set; } = decimal.MinValue;
        public DateTime HighestOnPeakTimestamp { get; set; } = DateTime.MinValue;
        public decimal HighestOffPeakAndDiscountValue { get; set; } = decimal.MinValue;
        public DateTime HighestOffPeakAndDiscountTimestamp { get; set; } = DateTime.MinValue;
        public decimal HighestDiscountValue { get; set; } = decimal.MinValue;
        public DateTime HighestDiscountTimestamp { get; set; } = DateTime.MinValue;
    }

    public static void Main(string[] args)
    {
        string filePath = "Energy Usage (4).xml";
        try
        {
            var intervalReadings = LoadIntervalReadings(filePath);
            if (intervalReadings == null) return;

            var usageData = ProcessUsageData(intervalReadings);
            CalculateAndDisplayReport(usageData);
            CalculateAndDisplayTopUsageDays(intervalReadings);
            CalculateAndDisplayAverageHourlyUsage(intervalReadings);
        }
        catch (TimeZoneNotFoundException)
        {
            Console.WriteLine("Error: The Eastern Standard Time zone could not be found on this system.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static IEnumerable<XElement> LoadIntervalReadings(string filePath)
    {
        XDocument doc = XDocument.Load(filePath);
        XNamespace espi = "http://naesb.org/espi";
        var intervalReadings = doc.Descendants(espi + "IntervalReading");

        if (!intervalReadings.Any())
        {
            Console.WriteLine("No <espi:IntervalReading> elements found in the XML file.");
            return null;
        }
        return intervalReadings;
    }

    private static EnergyUsageData ProcessUsageData(IEnumerable<XElement> intervalReadings)
    {
        var usageData = new EnergyUsageData();
        XNamespace espi = "http://naesb.org/espi";
        TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        foreach (XElement reading in intervalReadings)
        {
            XElement startElement = reading.Element(espi + "timePeriod")?.Element(espi + "start");
            XElement valueElement = reading.Element(espi + "value");

            if (startElement != null && valueElement != null &&
                long.TryParse(startElement.Value, out long unixTimestamp) &&
                decimal.TryParse(valueElement.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            {
                DateTime estDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime, estTimeZone);
                int hour = estDateTime.Hour;

                if (hour >= 1 && hour < 6) // Discount
                {
                    usageData.DiscountEnergy += value;
                    if (value > usageData.HighestDiscountValue)
                    {
                        usageData.HighestDiscountValue = value;
                        usageData.HighestDiscountTimestamp = estDateTime;
                    }
                }
                else if ((hour >= 6 && hour < 18) || (hour >= 21 && hour < 24) || hour == 0) // Off-Peak
                {
                    usageData.OffPeakEnergy += value;
                    if (value > usageData.HighestOffPeakAndDiscountValue)
                    {
                        usageData.HighestOffPeakAndDiscountValue = value;
                        usageData.HighestOffPeakAndDiscountTimestamp = estDateTime;
                    }
                }
                else if (hour >= 18 && hour < 21) // On-Peak
                {
                    usageData.OnPeakEnergy += value;
                    if (value > usageData.HighestOnPeakValue)
                    {
                        usageData.HighestOnPeakValue = value;
                        usageData.HighestOnPeakTimestamp = estDateTime;
                    }
                }
                usageData.TotalEnergy += value;
            }
            else
            {
                Console.WriteLine("Warning: Could not parse timestamp or value for an interval reading. Skipping.");
            }
        }
        return usageData;
    }

    private static void CalculateAndDisplayReport(EnergyUsageData usageData)
    {
        // --- Cost Calculation ---
        const decimal discountRate = 0.04156m;
        const decimal offPeakRate = 0.06342m;
        const decimal onPeakRate = 0.14954m;
        const decimal offPeakDemandRate = 3.82m;
        const decimal onPeakDemandRate = 1.95m;

        decimal totalEnergyCost = (usageData.DiscountEnergy * discountRate) + (usageData.OffPeakEnergy * offPeakRate) + (usageData.OnPeakEnergy * onPeakRate);
        decimal offPeakDemandCharge = (usageData.HighestOffPeakAndDiscountValue * 4) * offPeakDemandRate;
        decimal onPeakDemandCharge = (usageData.HighestOnPeakValue * 4) * onPeakDemandRate;
        decimal totalDemandCharge = offPeakDemandCharge + onPeakDemandCharge;
        decimal totalPrice = totalEnergyCost + totalDemandCharge;

        // --- Display Results ---
        Console.WriteLine("--- Energy Usage Summary ---");
        Console.WriteLine($"Total Discount Energy (1am-6am): {usageData.DiscountEnergy:F2} kWh");
        Console.WriteLine($"Total Off-Peak Energy (6am-6pm, 9pm-1am): {usageData.OffPeakEnergy:F2} kWh");
        Console.WriteLine($"Total On-Peak Energy (6pm-9pm): {usageData.OnPeakEnergy:F2} kWh");
        Console.WriteLine($"Total energy consumption: {usageData.TotalEnergy:F2} kWh");
        Console.WriteLine();
        Console.WriteLine("--- Peak Power Summary ---");
        Console.WriteLine($"Highest On-Peak average power: {usageData.HighestOnPeakValue * 4:F2} kW on {usageData.HighestOnPeakTimestamp:yyyy-MM-dd HH:mm:ss} EST");
        Console.WriteLine($"Highest average power outside of On-Peak times: {usageData.HighestOffPeakAndDiscountValue * 4:F2} kW on {usageData.HighestOffPeakAndDiscountTimestamp:yyyy-MM-dd HH:mm:ss} EST");
        Console.WriteLine($"Highest average power during discount times: {usageData.HighestDiscountValue * 4:F2} kW on {usageData.HighestDiscountTimestamp:yyyy-MM-dd HH:mm:ss} EST");
        Console.WriteLine();
        Console.WriteLine("--- Cost Breakdown ---");
        Console.WriteLine($"Total Energy Cost: {totalEnergyCost:C}");
        Console.WriteLine($"On-Peak Demand Charge: {onPeakDemandCharge:C}");
        Console.WriteLine($"Off-Peak Demand Charge: {offPeakDemandCharge:C}");
        Console.WriteLine($"Total Estimated Price: {totalPrice:C}");

        // --- Flat Rate Comparison ---
        const decimal flatRate = 0.12119m;
        decimal flatRateTotalCost = usageData.TotalEnergy * flatRate;
        decimal savings = totalPrice - flatRateTotalCost;

        Console.WriteLine();
        Console.WriteLine("--- Flat Rate Comparison ---");
        Console.WriteLine($"Total cost at a flat rate of {flatRate:C5}/kWh would be: {flatRateTotalCost:C}");

        if (savings < 0)
        {
            Console.WriteLine($"You saved {Math.Abs(savings):C} with the time-of-use plan compared to the flat rate.");
        }
        else
        {
            Console.WriteLine($"You would have spent an extra {savings:C} with the time-of-use plan compared to the flat rate.");
        }
    }

    private static void CalculateAndDisplayTopUsageDays(IEnumerable<XElement> intervalReadings)
    {
        XNamespace espi = "http://naesb.org/espi";
        TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var dailyUsage = new Dictionary<DateTime, decimal>();

        foreach (XElement reading in intervalReadings)
        {
            XElement startElement = reading.Element(espi + "timePeriod")?.Element(espi + "start");
            XElement valueElement = reading.Element(espi + "value");

            if (startElement != null && valueElement != null &&
                long.TryParse(startElement.Value, out long unixTimestamp) &&
                decimal.TryParse(valueElement.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            {
                DateTime estDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime, estTimeZone);
                DateTime date = estDateTime.Date;

                if (dailyUsage.ContainsKey(date))
                {
                    dailyUsage[date] += value;
                }
                else
                {
                    dailyUsage[date] = value;
                }
            }
        }

        var top5Days = dailyUsage.OrderByDescending(kvp => kvp.Value).Take(5);

        Console.WriteLine();
        Console.WriteLine("--- Top 5 Days of kWh Usage ---");
        foreach (var day in top5Days)
        {
            Console.WriteLine($"{day.Key:yyyy-MM-dd}: {day.Value:F2} kWh");
        }
    }

    private static void CalculateAndDisplayAverageHourlyUsage(IEnumerable<XElement> intervalReadings)
    {
        XNamespace espi = "http://naesb.org/espi";
        TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var hourlyUsage = new Dictionary<int, decimal>();
        var hourlyCounts = new Dictionary<int, int>();

        foreach (XElement reading in intervalReadings)
        {
            XElement startElement = reading.Element(espi + "timePeriod")?.Element(espi + "start");
            XElement valueElement = reading.Element(espi + "value");

            if (startElement != null && valueElement != null &&
                long.TryParse(startElement.Value, out long unixTimestamp) &&
                decimal.TryParse(valueElement.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            {
                DateTime estDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime, estTimeZone);
                int hour = estDateTime.Hour;

                if (hourlyUsage.ContainsKey(hour))
                {
                    hourlyUsage[hour] += value;
                    hourlyCounts[hour]++;
                }
                else
                {
                    hourlyUsage[hour] = value;
                    hourlyCounts[hour] = 1;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("--- Average Power Usage per Hour ---");
        for (int hour = 0; hour < 24; hour++)
        {
            if (hourlyUsage.ContainsKey(hour))
            {
                decimal averageUsage = (hourlyUsage[hour] / hourlyCounts[hour]) * 4; // Multiply by 4 to get kW from kWh per 15 min
                Console.WriteLine($"Hour {hour:00}:00 - {hour:00}:59: {averageUsage:F2} kW, Total: {hourlyUsage[hour]:F2} kWh");
            }
            else
            {
                Console.WriteLine($"Hour {hour:00}:00 - {hour:00}:59: No data");
            }
        }
    }
}
