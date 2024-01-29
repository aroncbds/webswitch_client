namespace WebSwitchClient.DataLayer.Models
{
    public class TemperatureSensor
    {
        /// <summary>
        /// The numerical index of the sensor on the WebSwitch.
        /// </summary>
        public int SensorIndex { get; private set; }

        public string Name { get; set; }

        public float Value { get; private set; }

        public TemperatureSensor(int index, float value)
        {
            this.SensorIndex = index;
            this.Value = value;
        }

        public TemperatureSensor(string name, float value)
        {
            this.Name = name;
            this.Value = value;
        }

        public override string ToString()
        {
            return $"Name: {this.Name ?? "N/A"}, Sensor index: {this.SensorIndex}, Value: {this.Value}";
        }
    }
}
