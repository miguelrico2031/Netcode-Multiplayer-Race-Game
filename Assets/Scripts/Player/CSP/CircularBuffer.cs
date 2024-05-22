


public class CircularBuffer<T>
{
    private T[] _buffer;
    private int _size;

    public CircularBuffer(int size)
    {
        _size = size;
        _buffer = new T[_size];
    }

    public void Add(T element, int index) => _buffer[index % _size] = element;
    public T Get(int index) => _buffer[index % _size];
    public void Clear() => _buffer = new T[_size];
}
