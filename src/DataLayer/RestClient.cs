using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using WebSwitchClient.DataLayer.Exceptions;
using WebSwitchClient.DataLayer.Models;

namespace WebSwitchClient.DataLayer
{
    /// <summary>
    /// References:
    /// https://www.webswitch.se/wp/?page_id=342
    /// </summary>
    public class RestClient
    {
        private const string TEMPERATURE_SENSOR_NOT_FOUND_VALUE = "X";

        public string BaseURI { get; private set; }

        public string Username { get; private set; }

        public string Password { get; private set; }

        private bool _auheneticationNeeded;

        public RestClient(string baseUri, string username, string password)
        {
            this.BaseURI = baseUri;
            if (!this.BaseURI.EndsWith("/"))
                this.BaseURI += "/";
            this.Username = username;
            this.Password = password;

            _auheneticationNeeded = true;
        }

        public RestClient(string baseUri)
        {
            this.BaseURI = baseUri;
            if (!this.BaseURI.EndsWith("/"))
                this.BaseURI += "/";

            this.Username = null;
            this.Password = null;

            _auheneticationNeeded = false;
        }

        #region 1-Wire
        private void PrepareHeaders(HttpClient client)
        {
            if (!_auheneticationNeeded)
                return;

            client.BaseAddress = new Uri(this.BaseURI);
            client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Add("Authorization", "Basic " + this.Password);
            // Set up basic authentication
            var byteArray = Encoding.ASCII.GetBytes($"{this.Username}:{this.Password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        }

        /// <summary>
        /// Return temperature by sensor index. http://[webswitch address]/temperature/get2/{sensorIndex}
        /// </summary>
        /// <param name="sensorIndex"></param>
        /// <returns></returns>
        /// <exception cref="TemperatureSensorNotFoundException"></exception>
        /// <exception cref="TemperatureParseException"></exception>
        /// <exception cref="TemperatureSensorStatusCodeException"></exception>
        public async Task<float> GetTemperature(int sensorIndex, CancellationToken cancellationToken = default)
        {
            using (var client = new HttpClient())
            {
                PrepareHeaders(client);

                // Make a GET request to the endpoint
                HttpResponseMessage response = await client.GetAsync(this.BaseURI + $"temperature/get2/{sensorIndex}");
                if (response.IsSuccessStatusCode)
                {
                    // Successfully connected, handle the response here
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Check if the operation was canceled
                    cancellationToken.ThrowIfCancellationRequested();

                    if (responseBody == "X")
                    {
                        throw new TemperatureSensorNotFoundException($"Failed to find any sensor with index {sensorIndex}");
                    }
                    if (!float.TryParse(responseBody, NumberStyles.Float, CultureInfo.InvariantCulture, out float temperature))
                    {
                        throw new TemperatureParseException($"Failed to parse the temperature \"{responseBody}\"");
                    }
                    return temperature;
                }
                else
                {
                    throw new TemperatureSensorStatusCodeException($"Got unexpected status code {response.StatusCode}");
                }
            }
        }

        /// <summary>
        /// Return temperature by sensor name. http://[webswitch address]/temperature/get2/fl
        /// </summary>
        /// <param name="sensorName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="TemperatureSensorNotFoundException"></exception>
        /// <exception cref="TemperatureParseException"></exception>
        /// <exception cref="TemperatureSensorStatusCodeException"></exception>
        public async Task<float> GetTemperature(string sensorName, CancellationToken cancellationToken = default)
        {
            using (var client = new HttpClient())
            {
                PrepareHeaders(client);

                // Make a GET request to the endpoint
                HttpResponseMessage response = await client.GetAsync(this.BaseURI + $"temperature/get2/{sensorName}");

                // Check if the operation was canceled
                cancellationToken.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    // Successfully connected, handle the response here
                    string responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody == TEMPERATURE_SENSOR_NOT_FOUND_VALUE)
                    {
                        throw new TemperatureSensorNotFoundException($"Failed to find any sensor with name {sensorName}");
                    }
                    if (!float.TryParse(responseBody, NumberStyles.Float, CultureInfo.InvariantCulture, out float temperature))
                    {
                        throw new TemperatureParseException($"Failed to parse the temperature \"{responseBody}\"");
                    }
                    return temperature;
                }
                else
                {
                    throw new TemperatureSensorStatusCodeException($"Got unexpected status code {response.StatusCode}");
                }
            }
        }

        /// <summary>
        /// Returns the value of the temperature sensors with the provided indices. http://wsm.homenet.local/temperature/get2/1$2
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="TemperatureSensorStatusCodeException"></exception>
        public async Task<TemperatureSensorCollection> GetTemperatures(int[] indices, CancellationToken cancellationToken = default)
        {
            string indexSegment = string.Join('$', indices.ToArray());

            //var url = GetSensorTypeUrl();

            TemperatureSensorCollection result = new();

            using (var client = new HttpClient())
            {
                PrepareHeaders(client);

                // Make a GET request to the endpoint
                HttpResponseMessage response = await client.GetAsync(this.BaseURI + $"temperature/get2/{indexSegment}");

                // Check if the operation was canceled
                cancellationToken.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    // Successfully connected, handle the response here
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var splitResult = responseBody.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    var failedItems = splitResult.Where(t => t.EndsWith($",{TEMPERATURE_SENSOR_NOT_FOUND_VALUE}")).ToArray();
                    if (failedItems.Count() > 0)
                    {
                        var failedItemIndices = failedItems
                            .Select(s => s.First())
                            .ToArray();

                        result.FailedReadingSensorIndexes = failedItemIndices.Select(c => int.Parse(c.ToString())).ToList();
                    }

                    var parsedItems = splitResult.Where(t => !t.EndsWith($",{TEMPERATURE_SENSOR_NOT_FOUND_VALUE}")).ToArray();
                    if (parsedItems.Count() > 0)
                    {
                        var objects = parsedItems
                            .Select(s =>
                            {
                                var parts = s.Split(',');
                                int sensorIndex = int.Parse(parts[0]);
                                float value = float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                                return new TemperatureSensor(sensorIndex, value);
                            })
                            .ToArray();

                        result.AddRange(objects);
                    }
                    return result;
                }
                else
                {
                    throw new TemperatureSensorStatusCodeException($"Got unexpected status code {response.StatusCode}");
                }
            }
        }
        #endregion

        #region Relays
        public async Task<bool> SetRelay(bool isOn)
        {
            // http://192.168.2.18/relaycontrol/on/2
            return false;
        }

        /// <summary>
        /// Pulse a relay high(on) for a selectable duration in seconds.
        /// Note: If the initial relay state is on, the relay will go low after the provided interval.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public async Task<bool> PulseRelayHigh(int index, int seconds)
        {
            // http://192.168.2.18/relaycontrol/pulse/high/1/2
            return false;
        }

        /// <summary>
        /// Pulse a relay low(off) for a selectable duration in seconds.
        /// Note: If the initial relay state is off, the relay will go high after the provided interval.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public async Task<bool> PulseRelayLow(int index, int seconds)
        {
            // http://192.168.2.18/relaycontrol/pulse/low/1/2
            return false;
        }

        public async Task<bool> GetRelayState(int relayIndex)
        {
            // http://192.168.2.18/relaystate/get/4
            return false;
        }
        #endregion

        #region Helpers
        private string GetSensorTypeUrl(SensorType sensorType = SensorType.Temperature)
        {
            switch (sensorType)
            {
                case SensorType.Relays:
                    return $"{this.BaseURI}relaycontrol/";
                case SensorType.Temperature:
                    return $"{this.BaseURI}temperature/";
                case SensorType.Digital:
                    return $"{this.BaseURI}input/";
                default:
                    throw new Exception("Unknown sensor type");
            }
        }
        #endregion
    }
}
