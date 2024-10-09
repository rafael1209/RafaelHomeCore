using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace RafaelHomeCore.Pages
{
    public class ProcessesModel : PageModel
    {
        public List<Process>? Processes { get; set; }

        public List<(string Directory, string DllPath)> DotnetProjects { get; set; }

        private string scriptPath = "/home/rafael/start.sh";
        private string directoryPath = "/home/rafael";

        public void OnGet()
        {
            try
            {
                Processes = Process.GetProcessesByName("dotnet").ToList();

                DotnetProjects = GetDotnetProjectsFromScript(scriptPath);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to retrieve processes: {ex.Message}";
            }
        }


        public IActionResult OnPostStop(int id)
        {
            try
            {
                var process = Process.GetProcessById(id);
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
                else
                {
                    TempData["Error"] = $"Process with ID {id} is not running.";
                }
            }
            catch (ArgumentException)
            {
                TempData["Error"] = $"Process with ID {id} is not running.";
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                TempData["Error"] = $"Failed to stop process with ID {id}: {ex.Message}";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUploadUpdate(string directory, IFormFile updateFile)
        {
            try
            {
                if (updateFile != null && updateFile.Length > 0)
                {
                    if (directory.StartsWith("~"))
                    {
                        string homeDirectory = Environment.GetEnvironmentVariable("HOME");
                        directory = directory.Replace("~", homeDirectory);
                    }

                    var filePath = Path.Combine(directory, updateFile.FileName);

                    Directory.CreateDirectory(directory);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        updateFile.CopyTo(stream);
                    }

                    if (updateFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        ZipFile.ExtractToDirectory(filePath, directory, true);
                        TempData["Success"] = $"ZIP archive {updateFile.FileName} uploaded and extracted successfully.";
                    }
                    else if (updateFile.FileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var archive = ArchiveFactory.Open(filePath))
                        {
                            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                            {
                                entry.WriteToDirectory(directory, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                            }
                        }
                        TempData["Success"] = $"RAR archive {updateFile.FileName} uploaded and extracted successfully.";
                    }
                    else
                    {
                        TempData["Info"] = $"File {updateFile.FileName} uploaded successfully but not extracted because it is not a ZIP or RAR archive.";
                    }
                }
                else
                {
                    TempData["Error"] = "No file selected or file is empty.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to upload and extract update: {ex.Message}";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostStart(string directory, string dllPath)
        {
            try
            {
                directory = directory.Replace("~", directoryPath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "/usr/lib/dotnet/dotnet",
                    Arguments = $"{dllPath}",
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                var process = new Process { StartInfo = startInfo };
                process.Start();

            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to start {dllPath} in {directory}: {ex.Message}";
            }

            return RedirectToPage();
        }


        public List<(string Directory, string DllPath)> GetDotnetProjectsFromScript(string scriptPath)
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

        public string GetExecutableName(Process process)
        {
            if (process == null)
            {
                return "Unknown";
            }

            try
            {
                var arguments = process.StartInfo.Arguments;
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    var dllPath = arguments.Split(' ').FirstOrDefault();
                    if (dllPath != null)
                    {
                        return Path.GetFileNameWithoutExtension(dllPath);
                    }
                }
            }
            catch
            {
                return "Unknown";
            }
            return "Unknown";
        }
    }
}