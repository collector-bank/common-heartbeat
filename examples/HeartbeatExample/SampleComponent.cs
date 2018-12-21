using System;
using System.Threading.Tasks;

namespace HeartbeatExample
{
    public class SampleComponent
    {
        private int _i = 0;

        public async Task PerformHealthCheckAsync()
        {
            await Task.Delay(500);
            _i++;
            if (_i % 3 == 0)
            {
                throw new Exception("Internal error");
            }
            await Task.Delay(250);
        }
    }

    public class GenericSampleComponent<T>
    {
        public async Task PerformHealthCheckAsync()
        {
            await Task.Delay(300);
        }
    }
}