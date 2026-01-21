using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;

public class FlightBookingPlugin
{
    private const string FilePath = "flights.json";
    private List<FlightModel> flights;

    public FlightBookingPlugin()
    {
        // Load flights from the file
        flights = LoadFlightsFromFile();
    }

    //
    // Create a plugin function with kernel function attributes
    //

    //
    // Create a kernel function to book flights
    //
    
    private void SaveFlightsToFile()
    {
        var json = JsonSerializer.Serialize(flights, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    private List<FlightModel> LoadFlightsFromFile()
    {
        if (File.Exists(FilePath))
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<FlightModel>>(json)!;
        }

        throw new FileNotFoundException($"The file '{FilePath}' was not found. Please provide a valid flights.json file.");
    }
}

// Flight model
public class FlightModel
{
    public int Id { get; set; }
    public required string Airline { get; set; }
    public required string Destination { get; set; }
    public required string DepartureDate { get; set; }
    public decimal Price { get; set; }
    public bool IsBooked { get; set; } = false; // Added to track booking status
}
