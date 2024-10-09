using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RafaelHomeCore.Controllers
{
    public class ProcessController : Controller
    {
        private const string ScriptPath = "/home/rafael/start.sh";

        public IActionResult Index()
        {
            var processes = Process.GetProcessesByName("dotnet").ToList();
            var dotnetProjects = GetDotnetProjectsFromScript(ScriptPath);

            ViewBag.Processes = processes;
            ViewBag.DotnetProjects = dotnetProjects;

            return View();
        }

        [HttpPost]
        public IActionResult Start(string directory, string dllPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"{dllPath}",
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                var process = new Process { StartInfo = startInfo };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                TempData["Success"] = $"Successfully started {dllPath} in {directory}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to start {dllPath} in {directory}: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Stop(int id)
        {
            try
            {
                var process = Process.GetProcessById(id);
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    TempData["Success"] = $"Successfully stopped process with ID {id}.";
                }
                else
                {
                    TempData["Error"] = $"Process with ID {id} is not running.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to stop process with ID {id}: {ex.Message}";
            }

            return RedirectToAction("Index");
        }


        private List<(string Directory, string DllPath)> GetDotnetProjectsFromScript(string scriptPath)
        {
            var projects = new List<(string Directory, string DllPath)>();

            var lines = System.IO.File.ReadAllLines(scriptPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("cd "))
                {
                    var directory = lines[i].Substring(3).Trim();
                    var dllLine = lines[i + 1];
                    if (dllLine.StartsWith("dotnet "))
                    {
                        var dllPath = dllLine.Substring(7).Trim().TrimEnd('&');
                        projects.Add((directory, dllPath));
                    }
                }
            }

            return projects;
        }
    }
}
