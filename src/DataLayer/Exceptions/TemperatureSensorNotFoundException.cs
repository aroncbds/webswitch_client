namespace WebSwitchClient.DataLayer.Exceptions
{
    public class TemperatureSensorNotFoundException : Exception
    {
        public TemperatureSensorNotFoundException(string message) : base(message)
        {}
    }
}
