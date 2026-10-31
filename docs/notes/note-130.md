# Note 130

WeatherForecast.TemperatureF returns 32 plus the integer result of dividing TemperatureC by 0.5556 (`public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);`). The property is `TemperatureF` on the `WeatherForecast` record in `src/Core/Model/WeatherForecast.cs`.
