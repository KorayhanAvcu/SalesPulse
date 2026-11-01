namespace SalesPulse.DataGenerator;

// Ağırlıklı rastgele seçim: ağırlığı büyük olan daha sık seçilir.
public class WeightedRandom<T>
{
    private readonly T[] _items;           // seçenekler (şehirler, ürünler...)
    private readonly double[] _cumulative; // cetveldeki sınırlar: 25, 34, 41...
    private readonly double _total;        // cetvelin toplam uzunluğu
    private readonly Random _random;

    public WeightedRandom(IEnumerable<(T Item, double Weight)> items, Random random)
    {
        var list = items.ToList();
        _items = new T[list.Count];
        _cumulative = new double[list.Count];

        double sum = 0;
        for (int i = 0; i < list.Count; i++)
        {
            _items[i] = list[i].Item;
            sum += list[i].Weight;
            _cumulative[i] = sum;   // her seçeneğin bitiş noktası
        }

        _total = sum;
        _random = random;
    }

    public T Next()
    {
        // 0 ile toplam arasında rastgele bir nokta = dart
        double dart = _random.NextDouble() * _total;

        // Dart hangi aralığa düştü? (ikili arama ile hızlıca bul)
        int index = Array.BinarySearch(_cumulative, dart);
        if (index < 0) index = ~index;

        return _items[index];
    }
}
