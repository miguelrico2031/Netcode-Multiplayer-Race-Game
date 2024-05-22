
    public class NetworkTimer
    {
        public readonly float MinTimeBetweenTicks;
        public int Tick { get; private set; }
        private float _timer;

        public NetworkTimer(float tickRate) => MinTimeBetweenTicks = 1f / tickRate;

        public void Update(float dt) => _timer += dt;

        public bool CanTick()
        {
            if (_timer < MinTimeBetweenTicks) return false;
            _timer -= MinTimeBetweenTicks;
            Tick++;
            return true;
        }

    }
