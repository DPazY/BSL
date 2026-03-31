namespace BSL.Implementation.Collection
{
    public class ObjectHistory
    {
        double[] _buffer;
        int _head;
        int _requestRate = 0;

        public ObjectHistory(int bufferSize)
        {
            _buffer = new double[bufferSize];
            BufferSize = bufferSize;
            _head = bufferSize - 1;
        }
        public int BufferSize { get; init; }

        private int NextPosition(int position)
        {
            return (position + 1) % BufferSize;
        }

        public void RecordHit() => Interlocked.Increment(ref _requestRate);
        

        public double[] GetTimeSeriesAndReset()
        {
            int currentRate = Interlocked.Exchange(ref _requestRate, 0);

            _head = NextPosition(_head);
            _buffer[_head] = currentRate;

            double[] result = new double[BufferSize];
            for (int i = 0; i < BufferSize; i++)
            {
                int index = (_head + 1 + i) % BufferSize;

                result[i] = _buffer[index];
            }

            return result;
        }

    }
}
