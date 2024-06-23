export interface TrafficData {
    id: number;
    location: string;
    trafficFlow: number; // Could be a percentage or any other metric
    timestamp: Date;
}
  