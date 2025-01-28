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

    // Add your code
    [KernelFunction("search_flights")]
    [Description("Searches for available flights based on the destination and departure date in the format YYYY-MM-DD")]
    [return: Description("A list of available flights")]
    public List<FlightModel> SearchFlights(string destination, string departureDate)
    {
       // Filter flights based on destination
        return flights.Where(flight =>
            flight.Destination.Equals(destination, StringComparison.OrdinalIgnoreCase) &&
            flight.DepartureDate.Equals(departureDate)).ToList();
    }

    [KernelFunction("book_flight")]
    [Description("Books a flight based on the flight ID provided")]
    [return: Description("Booking confirmation message")]
    public string BookFlight(int flightId)
    {
        var flight = flights.FirstOrDefault(f => f.Id == flightId);
        if (flight == null)
        {
            return "Flight not found. Please provide a valid flight ID.";
        }

        if (flight.IsBooked)
        {
            return $"You've already booked this flight.";
        }

        flight.IsBooked = true;
        SaveFlightsToFile();
        
        return $"Flight booked successfully! Airline: {flight.Airline}, Destination: {flight.Destination}, Departure: {flight.DepartureDate}, Price: ${flight.Price}.";
    }

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
