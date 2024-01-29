namespace WebSwitchClient.DataLayer.Models
{
    public class TemperatureSensorCollection : List<TemperatureSensor>
    {
        public List<int> FailedReadingSensorIndexes { get; set; }

        public List<TemperatureSensor> this[string name]
        {
            get
            {
                return this.FindAll(s => s.Name == name);
            }
        }

        public List<TemperatureSensor> this[int sensorIndex]
        {
            get
            {
                return this.FindAll(s => s.SensorIndex == sensorIndex);
            }
        }

        public string FailedReadingSensorIndexesAsCsv()
        {
            if (this.FailedReadingSensorIndexes == null)
                return null;
            return string.Join(',', this.FailedReadingSensorIndexes);
        }
    }
}
