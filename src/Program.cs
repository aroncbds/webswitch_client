using WebSwitchClient.DataLayer;
using WebSwitchClient.DataLayer.Exceptions;

RestClient webSwitch = new("http://wsm.homenet.local");
try
{
    //float framLedningTempC = await webSwitch.GetTemperature("FL");
    //float returLedningTempC = await webSwitch.GetTemperature("RL");
    var tempSensorCollection = await webSwitch.GetTemperatures(new int[] { 1, 2, 3 });

    foreach (var tempSensor in tempSensorCollection)
    {
        Console.WriteLine(tempSensor);
    }

    if (tempSensorCollection.FailedReadingSensorIndexes.Count > 0)
    {
        Console.WriteLine($"Unable to read the sensors with the following requested indexes: {tempSensorCollection.FailedReadingSensorIndexesAsCsv()}");
    }
    //Console.WriteLine($"{framLedningTempC} / {returLedningTempC} C");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to retrieve temperature. Internal error: {ex.Message}");
}