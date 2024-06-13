export interface Condition {
  text: string;
  icon: string;
  code: number;
}

export interface CurrentWeather {
  condition: Condition;
  temp_c: number;
  humidity: number;
}

export interface WeatherResponse {
  current: CurrentWeather;
}
