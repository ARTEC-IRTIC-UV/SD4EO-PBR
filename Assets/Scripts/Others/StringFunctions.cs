using System;
using System.Collections.Generic;
using UnityEngine;

public class StringFunctions : MonoBehaviour
{
    public static string GetMonthNumberAsString(string monthName)
    {
        Dictionary<string, int> monthDictionary;
        monthDictionary = new Dictionary<string, int>
        {
            { "January", 1 },
            { "February", 2 },
            { "March", 3 },
            { "April", 4 },
            { "May", 5 },
            { "June", 6 },
            { "July", 7 },
            { "August", 8 },
            { "September", 9 },
            { "October", 10 },
            { "November", 11 },
            { "December", 12 }
        };

        if (monthDictionary.TryGetValue(monthName, out int monthNumber))
        {
            return monthNumber.ToString("D2");
        }
        else
        {
            return null;
        }
    }
    
    public static string GetMonthNameFromNumber(string monthNumber)
    {
        Dictionary<int, string> monthDictionary = new Dictionary<int, string>
        {
            { 1, "January" },
            { 2, "February" },
            { 3, "March" },
            { 4, "April" },
            { 5, "May" },
            { 6, "June" },
            { 7, "July" },
            { 8, "August" },
            { 9, "September" },
            { 10, "October" },
            { 11, "November" },
            { 12, "December" }
        };

        if (int.TryParse(monthNumber, out int monthInt) && monthDictionary.TryGetValue(monthInt, out string monthName))
        {
            return monthName;
        }
        else
        {
            return null;
        }
    }
    
    public static string SubstringBetweenChars(string input, char targetChar)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input string cannot be null or empty");
        }

        int firstIndex = input.IndexOf(targetChar);
        if (firstIndex == -1)
        {
            throw new ArgumentException("Character not found in the string");
        }

        int secondIndex = input.IndexOf(targetChar, firstIndex + 1);
        if (secondIndex == -1)
        {
            throw new ArgumentException("Character does not appear twice in the string");
        }

        return input.Substring(firstIndex + 1, secondIndex - firstIndex - 1);
    }
}