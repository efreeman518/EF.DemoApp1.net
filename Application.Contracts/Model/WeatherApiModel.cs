using System.Text.Json.Serialization;

namespace Application.Contracts.Model;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public record Astro
{
    [JsonPropertyName("sunrise")]
    public string? Sunrise { get; init; }

    [JsonPropertyName("sunset")]
    public string? Sunset { get; init; }

    [JsonPropertyName("moonrise")]
    public string? Moonrise { get; init; }

    [JsonPropertyName("moonset")]
    public string? Moonset { get; init; }

    [JsonPropertyName("moon_phase")]
    public string? MoonPhase { get; init; }

    [JsonPropertyName("moon_illumination")]
    public int? MoonIllumination { get; init; }

    [JsonPropertyName("is_moon_up")]
    public int? IsMoonUp { get; init; }

    [JsonPropertyName("is_sun_up")]
    public int? IsSunUp { get; init; }
}

public class Condition
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("code")]
    public int? Code { get; init; }
}

public class Current
{
    [JsonPropertyName("last_updated_epoch")]
    public int? LastUpdatedEpoch { get; init; }

    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; init; }

    [JsonPropertyName("temp_c")]
    public double? TempC { get; init; }

    [JsonPropertyName("temp_f")]
    public double? TempF { get; init; }

    [JsonPropertyName("is_day")]
    public int? IsDay { get; init; }

    [JsonPropertyName("condition")]
    public Condition? Condition { get; init; }

    [JsonPropertyName("wind_mph")]
    public double? WindMph { get; init; }

    [JsonPropertyName("wind_kph")]
    public double? WindKph { get; init; }

    [JsonPropertyName("wind_degree")]
    public double? WindDegree { get; init; }

    [JsonPropertyName("wind_dir")]
    public string? WindDir { get; init; }

    [JsonPropertyName("pressure_mb")]
    public double? PressureMb { get; init; }

    [JsonPropertyName("pressure_in")]
    public double? PressureIn { get; init; }

    [JsonPropertyName("precip_mm")]
    public double? PrecipMm { get; init; }

    [JsonPropertyName("precip_in")]
    public double? PrecipIn { get; init; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; init; }

    [JsonPropertyName("cloud")]
    public int? Cloud { get; init; }

    [JsonPropertyName("feelslike_c")]
    public double? FeelslikeC { get; init; }

    [JsonPropertyName("feelslike_f")]
    public double? FeelslikeF { get; init; }

    [JsonPropertyName("vis_km")]
    public double? VisKm { get; init; }

    [JsonPropertyName("vis_miles")]
    public double? VisMiles { get; init; }

    [JsonPropertyName("uv")]
    public double? Uv { get; init; }

    [JsonPropertyName("gust_mph")]
    public double? GustMph { get; init; }

    [JsonPropertyName("gust_kph")]
    public double? GustKph { get; init; }
}

public class Day
{
    [JsonPropertyName("maxtemp_c")]
    public double? MaxtempC { get; init; }

    [JsonPropertyName("maxtemp_f")]
    public double? MaxtempF { get; init; }

    [JsonPropertyName("mintemp_c")]
    public double? MintempC { get; init; }

    [JsonPropertyName("mintemp_f")]
    public double? MintempF { get; init; }

    [JsonPropertyName("avgtemp_c")]
    public double? AvgtempC { get; init; }

    [JsonPropertyName("avgtemp_f")]
    public double? AvgtempF { get; init; }

    [JsonPropertyName("maxwind_mph")]
    public double? MaxwindMph { get; init; }

    [JsonPropertyName("maxwind_kph")]
    public double? MaxwindKph { get; init; }

    [JsonPropertyName("totalprecip_mm")]
    public double? TotalprecipMm { get; init; }

    [JsonPropertyName("totalprecip_in")]
    public double? TotalprecipIn { get; init; }

    [JsonPropertyName("totalsnow_cm")]
    public double? TotalsnowCm { get; init; }

    [JsonPropertyName("avgvis_km")]
    public double? AvgvisKm { get; init; }

    [JsonPropertyName("avgvis_miles")]
    public double? AvgvisMiles { get; init; }

    [JsonPropertyName("avghumidity")]
    public double? Avghumidity { get; init; }

    [JsonPropertyName("daily_will_it_rain")]
    public double? DailyWillItRain { get; init; }

    [JsonPropertyName("daily_chance_of_rain")]
    public double? DailyChanceOfRain { get; init; }

    [JsonPropertyName("daily_will_it_snow")]
    public double? DailyWillItSnow { get; init; }

    [JsonPropertyName("daily_chance_of_snow")]
    public double? DailyChanceOfSnow { get; init; }

    [JsonPropertyName("condition")]
    public Condition? Condition { get; init; }

    [JsonPropertyName("uv")]
    public double? Uv { get; init; }
}

public class Forecast
{
    [JsonPropertyName("forecastday")]
    public List<Forecastday>? Forecastday { get; init; }
}

public class Forecastday
{
    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("date_epoch")]
    public int? DateEpoch { get; init; }

    [JsonPropertyName("day")]
    public Day? Day { get; init; }

    [JsonPropertyName("astro")]
    public Astro? Astro { get; init; }

    [JsonPropertyName("hour")]
    public List<Hour>? Hour { get; init; }
}

public class Hour
{
    [JsonPropertyName("time_epoch")]
    public int? TimeEpoch { get; init; }

    [JsonPropertyName("time")]
    public string? Time { get; init; }

    [JsonPropertyName("temp_c")]
    public double? TempC { get; init; }

    [JsonPropertyName("temp_f")]
    public double? TempF { get; init; }

    [JsonPropertyName("is_day")]
    public int? IsDay { get; init; }

    [JsonPropertyName("condition")]
    public Condition? Condition { get; init; }

    [JsonPropertyName("wind_mph")]
    public double? WindMph { get; init; }

    [JsonPropertyName("wind_kph")]
    public double? WindKph { get; init; }

    [JsonPropertyName("wind_degree")]
    public double? WindDegree { get; init; }

    [JsonPropertyName("wind_dir")]
    public string? WindDir { get; init; }

    [JsonPropertyName("pressure_mb")]
    public double? PressureMb { get; init; }

    [JsonPropertyName("pressure_in")]
    public double? PressureIn { get; init; }

    [JsonPropertyName("precip_mm")]
    public double? PrecipMm { get; init; }

    [JsonPropertyName("precip_in")]
    public double? PrecipIn { get; init; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; init; }

    [JsonPropertyName("cloud")]
    public double? Cloud { get; init; }

    [JsonPropertyName("feelslike_c")]
    public double? FeelslikeC { get; init; }

    [JsonPropertyName("feelslike_f")]
    public double? FeelslikeF { get; init; }

    [JsonPropertyName("windchill_c")]
    public double? WindchillC { get; init; }

    [JsonPropertyName("windchill_f")]
    public double? WindchillF { get; init; }

    [JsonPropertyName("heatindex_c")]
    public double? HeatindexC { get; init; }

    [JsonPropertyName("heatindex_f")]
    public double? HeatindexF { get; init; }

    [JsonPropertyName("dewpoint_c")]
    public double? DewpointC { get; init; }

    [JsonPropertyName("dewpoint_f")]
    public double? DewpointF { get; init; }

    [JsonPropertyName("will_it_rain")]
    public int? WillItRain { get; init; }

    [JsonPropertyName("chance_of_rain")]
    public int? ChanceOfRain { get; init; }

    [JsonPropertyName("will_it_snow")]
    public int? WillItSnow { get; init; }

    [JsonPropertyName("chance_of_snow")]
    public int? ChanceOfSnow { get; init; }

    [JsonPropertyName("vis_km")]
    public double? VisKm { get; init; }

    [JsonPropertyName("vis_miles")]
    public double? VisMiles { get; init; }

    [JsonPropertyName("gust_mph")]
    public double? GustMph { get; init; }

    [JsonPropertyName("gust_kph")]
    public double? GustKph { get; init; }

    [JsonPropertyName("uv")]
    public double? Uv { get; init; }
}

public class Location
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("region")]
    public string? Region { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    [JsonPropertyName("lon")]
    public double? Lon { get; init; }

    [JsonPropertyName("tz_id")]
    public string? TzId { get; init; }

    [JsonPropertyName("localtime_epoch")]
    public int? LocaltimeEpoch { get; init; }

    [JsonPropertyName("localtime")]
    public string? Localtime { get; init; }
}

public class WeatherRoot
{
    [JsonPropertyName("location")]
    public Location? Location { get; init; }

    [JsonPropertyName("current")]
    public Current? Current { get; init; }

    [JsonPropertyName("forecast")]
    public Forecast? Forecast { get; init; }
}

