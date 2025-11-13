using System;
using System.Collections.Generic;
using System.Linq;

namespace Fap.Domain.Helpers
{
    /// <summary>
    /// Helper class for grade-related calculations and utilities
    /// </summary>
    public static class GradeHelper
    {
        /// <summary>
        /// Calculate letter grade based on numerical score (0-10 scale)
        /// </summary>
        /// <param name="score">Numerical score between 0 and 10</param>
        /// <returns>Letter grade (A+, A, B+, B, C+, C, D+, D, F)</returns>
        public static string CalculateLetterGrade(decimal score)
        {
            return score switch
            {
                >= 9.0m => "A+",
                >= 8.5m => "A",
                >= 8.0m => "B+",
                >= 7.0m => "B",
                >= 6.5m => "C+",
                >= 5.5m => "C",
                >= 5.0m => "D+",
                >= 4.0m => "D",
                _ => "F"
            };
        }

        /// <summary>
        /// Calculate weighted average score from multiple grade components
        /// </summary>
        /// <param name="grades">List of grades with scores and weights</param>
        /// <returns>Weighted average score, or null if no valid grades</returns>
        public static decimal? CalculateWeightedAverage(IEnumerable<(decimal score, int weight)> grades)
        {
            var gradesList = grades.ToList();
            if (!gradesList.Any())
                return null;

            decimal totalWeightedScore = 0;
            int totalWeight = 0;

            foreach (var (score, weight) in gradesList)
            {
                totalWeightedScore += score * weight;
                totalWeight += weight;
            }

            if (totalWeight == 0)
                return null;

            return totalWeightedScore / totalWeight;
        }

        /// <summary>
        /// Get grade point value for GPA calculation
        /// </summary>
        /// <param name="letterGrade">Letter grade</param>
        /// <returns>Grade point value (4.0 scale)</returns>
        public static decimal GetGradePoint(string letterGrade)
        {
            return letterGrade switch
            {
                "A+" => 4.0m,
                "A" => 4.0m,
                "B+" => 3.5m,
                "B" => 3.0m,
                "C+" => 2.5m,
                "C" => 2.0m,
                "D+" => 1.5m,
                "D" => 1.0m,
                _ => 0.0m  // F
            };
        }

        /// <summary>
        /// Check if score is passing
        /// </summary>
        /// <param name="score">Numerical score</param>
        /// <returns>True if score is passing (>= 4.0)</returns>
        public static bool IsPassing(decimal score)
        {
            return score >= 4.0m;
        }

        /// <summary>
        /// Validate score range
        /// </summary>
        /// <param name="score">Score to validate</param>
        /// <returns>True if score is in valid range (0-10)</returns>
        public static bool IsValidScore(decimal score)
        {
            return score >= 0 && score <= 10;
        }
    }
}
