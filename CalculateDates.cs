using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DateOverlapCalc
{
    public static class CalculateDates
    {
        [FunctionName("CalculateDates")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Parse the request body to get the projects array
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            List<dynamic> projects = new List<dynamic>(data.projects);
            int maxMonths = (int?)data.maxMonths ?? int.MaxValue;

            List<dynamic> results = new List<dynamic>();
            List<dynamic> warnings = new List<dynamic>();
            List<dynamic> errors = new List<dynamic>();

            // Sort the projects by start date
            projects = projects.OrderBy(p => DateTime.Parse((string)p.start)).ToList();

            // Loop through each project to calculate the total months and check for overlaps
            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                var start = DateTime.Parse(project.start.ToString());
                var end = DateTime.Parse(project.end.ToString() == "present" ? DateTime.Now.ToString() : project.end.ToString());
                var totalMonths = (end.Year - start.Year) * 12 + end.Month - start.Month + 1;

                // Calculate the original total months for the project before any overlaps
                var originalTotalMonths = (end.Year - start.Year) * 12 + end.Month - start.Month + 1;

                // Calculate the number of months since today's date
                var currentDate = DateTime.Now;
                var monthsSinceCurrentDate = (currentDate.Year - start.Year) * 12 + currentDate.Month - start.Month;

                int outOfRangeMonths = 0;

                // Calculate the number of months outside of the valid range
                if (Math.Min(monthsSinceCurrentDate, maxMonths) == maxMonths)
                {
                    outOfRangeMonths = Math.Max(0, monthsSinceCurrentDate - maxMonths);

                    errors.Add(new
                    {
                        project = project.name,
                        message = $"{project.name} has {outOfRangeMonths} months outside of the provided max range"
                    });
                }

                // If there no months out of range, process the rest
                if (outOfRangeMonths == 0)
                {
                    // Add the name of the overlapping project to a list
                    List<dynamic> overlappingProjects = new List<dynamic>();

                    if (project.overlappingProjects != null)
                    {
                        overlappingProjects = new List<dynamic>();
                    }


                    // Check for overlaps with previous projects
                    for (int j = 0; j < i; j++)
                    {
                        var previousProject = projects[j];
                        var previousStart = DateTime.Parse(previousProject.start.ToString());
                        var previousEnd = DateTime.Parse(previousProject.end.ToString() == "present" ? DateTime.Now.ToString() : previousProject.end.ToString());

                        if ((start >= previousStart && start <= previousEnd) || (end >= previousStart && end <= previousEnd))
                        {
                            // Calculate the overlapping months
                            var overlappingStart = start > previousStart ? start : previousStart;
                            var overlappingEnd = end < previousEnd ? end : previousEnd;
                            var overlappingMonths = (overlappingEnd.Year - overlappingStart.Year) * 12 + overlappingEnd.Month - overlappingStart.Month + 1;

                            // Adjust the total months for the current project
                            totalMonths -= overlappingMonths;

                            // Add the project to the list
                            overlappingProjects.Add(new { previousProject.name, overlappingMonths, start = previousStart });

                            // Add the overlapping project to the warnings list
                            warnings.Add(new
                            {
                                project = project.name,
                                message = $"{previousProject.name} overlaps with this project ({overlappingMonths} months)"
                            });
                        }
                    }


                    // Add the project with total months and index to the results list
                    results.Add(new
                    {
                        index = i,
                        project = project.name,
                        totalMonths,
                        monthsSinceCurrentDate,
                        originalTotalMonths,
                        outOfRangeMonths,
                        overlappingProjects
                    });
                }
            }

            // Create a dictionary to hold the final output with results and warnings (if any)
            var output = new Dictionary<string, object>
            {
                { "results", results },
                { "warnings", warnings },
                { "errors", errors }
            };

            // Return the output as JSON
            return new JsonResult(output);
        }
    }
}
