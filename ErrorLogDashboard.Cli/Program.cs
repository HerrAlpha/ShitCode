using System;
using System.IO;

namespace ErrorLogDashboard.Cli;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: artisan <command> [options]");
            Console.WriteLine("Commands:");
            Console.WriteLine("  make:model <Name>       Create a new model");
            Console.WriteLine("  make:controller <Name>  Create a new controller");
            return;
        }

        string command = args[0];

        try 
        {
            switch (command)
            {
                case "make:model":
                    if (args.Length < 2) { Console.WriteLine("Name required."); return; }
                    MakeModel(args[1]);
                    break;
                case "make:controller":
                    if (args.Length < 2) { Console.WriteLine("Name required."); return; }
                    MakeController(args[1]);
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void MakeModel(string name)
    {
        // Assuming running from solution root
        string path = Path.Combine(Directory.GetCurrentDirectory(), "ErrorLogDashboard.Web", "Models", $"{name}.cs");
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        
        string content = $@"using System.ComponentModel.DataAnnotations;

namespace ErrorLogDashboard.Web.Models;

public class {name}
{{
    public int Id {{ get; set; }}
    
    // Add properties here
}}";
        File.WriteAllText(path, content);
        Console.WriteLine($"Model {name} created successfully at {path}");
    }

    static void MakeController(string name)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "ErrorLogDashboard.Web", "Controllers", $"{name}Controller.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string content = $@"using Microsoft.AspNetCore.Mvc;

namespace ErrorLogDashboard.Web.Controllers;

public class {name}Controller : Controller
{{
    public IActionResult Index()
    {{
        return View();
    }}
}}";
        File.WriteAllText(path, content);
        Console.WriteLine($"Controller {name} created successfully at {path}");
    }
}
